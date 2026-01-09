using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using PlantGateway.Application.Abstractions.Configuration.Providers;
using PlantGateway.Application.Abstractions.Configuration.Watchers;
using PlantGateway.Core.Config.Legacy;
using PlantGateway.Core.Config.Models.Contracts;
using PlantGateway.Infrastructure.Implementations.Configuration.Resolvers;
using PlantGateway.Infrastructure.Implementations.Logging;
using PlantGateway.Presentation.CLI.Composition;
using Serilog;
using SMSgroup.Aveva.Application.CLI.PGedge.Bridge;
using SMSgroup.Aveva.Application.CLI.PGedge.Command;
using SMSgroup.Aveva.Application.CLI.PGedge.Connector.Commands;
using SMSgroup.Aveva.Application.CLI.PGedge.Environment.Project;
using SMSgroup.Aveva.Application.CLI.PGedge.Launch.Launcher;
using SMSgroup.Aveva.Application.CLI.PGedge.Mapping;
using SMSgroup.Aveva.Application.CLI.PGedge.Protocol.Commands;
using SMSgroup.Aveva.Application.CLI.PGedge.Session;
using SMSgroup.Aveva.Application.CLI.PGedge.Utility;
using Spectre.Console;
using Spectre.Console.Cli;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using PlantGateway.Presentation.CLI.Notifications;
using PlantGateway.Application.Pipelines.Execution.Parser.Factories;
using PlantGateway.Application.Pipelines.Execution.Parser.Interfaces;
using PlantGateway.Application.Pipelines.Execution.Parser.Strategies;
using PlantGateway.Application.Pipelines.Execution.Planner.Factories;
using PlantGateway.Application.Pipelines.Execution.Planner.Interfaces;
using PlantGateway.Application.Pipelines.Execution.Planner.Strategies;
using PlantGateway.Application.Pipelines.Execution.Validator.Factories;
using PlantGateway.Application.Pipelines.Execution.Validator.Interfaces;
using PlantGateway.Application.Pipelines.Execution.Validator.Strategies;
using PlantGateway.Application.Pipelines.Execution.Walker.Factory;
using PlantGateway.Application.Pipelines.Execution.Walker.Interfaces;
using PlantGateway.Application.Pipelines.Execution.Processor.Factories;
using PlantGateway.Application.Pipelines.Execution.Processor.Interfaces;
using PlantGateway.Application.Pipelines.Execution.Validator.Rules.Elements;
using PlantGateway.Application.Pipelines.Execution.Validator.Rules.Document.Xml;
using PlantGateway.Presentation.CLI.Commands;
using PlantGateway.Core.Config.Abstractions.Maps;
using PlantGateway.Core.Config.Models.Maps;
using PlantGateway.Core.Config.Implementations.Maps;
using PlantGateway.Infrastructure.Implementations.Configuration.Watchers;
using PlantGateway.Application.Pipelines.Execution.Parser.Analyzers.Elements;
using PlantGateway.Application.Pipelines.Execution.Parser.Analyzers.Document.Xml;
using PlantGateway.Domain.Specifications.Maps;

namespace PlantGateway.Presentation.CLI
{
    class Program
    {
        #region Fields
        private static IConfigWatcher? _watcher;
        #endregion

        [STAThread]
        static void Main(string[] args)
        {
            // 🔹 Load configuration and setup logger
            var config = BuildConfiguration();

            var (userLogger, adminLogger) = BuildLoggers(config);

            // keep Log.Logger for backward compatibility (goes to user log)
            Log.Logger = userLogger;

            // 🔹 Setup DI
            var services = new ServiceCollection();
            RegisterServices(services, config, args);
            RegisterCommands(services);

            // ✅ Build the provider FIRST
            var provider = services.BuildServiceProvider();

            // ✅ Now pass the built provider to TypeRegistrar
            ITypeRegistrar registrar = new TypeRegistrar(provider);
            var app = new CommandApp(registrar);
            app.Configure(ConfigureCliCommands);

            try
            {
                // -----------------------------------------------------------------------
                // 1) Intercept Windows protocol handler call: pgedge://...
                // -----------------------------------------------------------------------
                if (args.Length == 1 && args[0].StartsWith("pgedge://", StringComparison.OrdinalIgnoreCase))
                {
                    // Forward the URL to the dispatcher command
                    app.Run(new[] { "protocol", "dispatch", args[0] });
                }
                else if (args.Length == 1)
                {
                    // Handle one argument.

                    string fullInput = args[0];

                    // Tokenize into proper Spectre-compatible tokens
                    string[] tokenized = TokenizeArgs(fullInput);

                    // run actual CLI commands
                    app.Run(tokenized);
                }
                else if (args.Length > 1)
                {
                    app.Run(args);
                }
                else
                {
                    // Handle no arguments → Interactive Mode. Open CLI App

                    // Interactive mode → allocate console
                    AllocConsole();

                    // ✅ Start Config Watcher (background listener) only in Interactive Mode
                    StartConfigWatcher(provider);

                    // ✅ Only start background listener if in interactive mode
                    StartBridgeServerReceiver(provider);

                    RenderCliHeader(provider);

                    app.Run(new[] { "--help" });

                    while (true)
                    {
                        AnsiConsole.Markup("[bold green]> [/]");
                        var input = Console.ReadLine()?.Trim();

                        if (string.IsNullOrWhiteSpace(input)) continue;
                        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                        try
                        {
                            string[] parsedArgs = TokenizeArgs(input);
                            int result = app.Run(parsedArgs);
                            HandleReturnCode(result, app);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Command execution failed");
                            AnsiConsole.MarkupLineInterpolated($"[red]❌ Error: {ex.Message}[/]");
                        }
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("❌ Missing file or DLL:");
                Console.WriteLine($"   {ex.FileName}");
                Console.WriteLine($"   Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "CLI execution failed.");
                Console.WriteLine($"[FATAL] {ex.Message}");
            }
            finally
            {
                _watcher?.Dispose();   // ensure watcher stops cleanly
                if (provider is IDisposable disp)
                    disp.Dispose();

                Log.CloseAndFlush();
                Console.WriteLine("Exiting...");
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            return ConfigHelper.Load(AppEnvironmentResolver.ResolveAppEnv(string.Empty));
        }
        private static (ILogger userLogger, ILogger adminLogger) BuildLoggers(IConfiguration config)
        {
            // 1. Bind JSON section to contract
            var contract = config.GetSection("Logging").Get<SerilogContract>();

            // 2. Map contract → strongly typed settings
            var settings = SerilogMapper.Map(contract);

            // 3. Use service to build Serilog loggers
            var service = new SerilogService();
            var (userLogger, adminLogger) = service.BuildSeparateLoggers(settings);

            // Keep global static Log.Logger pointing to user logger for legacy Serilog usages
            Log.Logger = userLogger;

            return (userLogger, adminLogger);
        }

        private static void RegisterServices(IServiceCollection services, IConfiguration config, string[] args)
        {
            // Build both loggers once
            var (userLogger, adminLogger) = BuildLoggers(config);

            // Register both explicitly
            services.AddSingleton(userLogger);
            services.AddSingleton(adminLogger);

            // Notification service gets both
            services.AddSingleton<INotificationService>(sp => new NotificationService(userLogger, adminLogger));

            services.AddSingleton(config);

            // Parser Pipeline Layer
            services.AddScoped<IParserStrategy, TxtParserStrategy>();
            services.AddScoped<IParserStrategy, XmlParserStrategy>();
            services.AddScoped<IParserFactory, ParserFactory>();

            #region Validator
            // Validator Pipeline Layer
            services.AddSingleton<IValidatorFactory, ValidatorFactory>();

            // XML document-level validators
            services.AddTransient<IXDocumentValidator, HeaderValidator>();
            services.AddTransient<IXDocumentValidator, BodyValidator>();
            // (FooterValidator later, if needed)

            // XML element-level validators
            services.AddTransient<IXElementValidator, AssemblyNodeValidator>();
            services.AddTransient<IXElementValidator, PartNodeValidator>();

            // XML attribute-level validators
            // (none yet – to be added later)
            // services.AddTransient<IXAttributeValidator, AvevaTagAttributeValidator>();
            // services.AddTransient<IXAttributeValidator, GeomAttributeValidator>();

            // Strategy
            services.AddTransient<IValidatorStrategy, XmlValidatorStrategy>();

            #region Validator Rules Registration

            #region Validator Rules - Common

            #endregion

            #region Validator Rules - Xml

            #endregion

            #region Validator Rules - Txt

            #endregion

            #endregion
            #endregion

            // Planner Pipeline Layer
            services.AddSingleton<IPlannerFactory, PlannerFactory>();

            services.AddTransient(typeof(IPlanner<>), typeof(CommonPlannerStrategy<>));

            // Build provider once
            var cfgProvider = new ConfigProvider(config);

            // Get raw env input (only --env=… here; anything else -> empty string)
            var envArg = args?.FirstOrDefault(a => a.StartsWith("--env=", StringComparison.OrdinalIgnoreCase));
            var rawEnv = string.IsNullOrWhiteSpace(envArg) ? string.Empty : envArg.Split('=')[1];

            // Single resolver of truth: "" => (Debugger? dev : prod); else dev|stage|prod
            var appEnv = AppEnvironmentResolver.ResolveAppEnv(rawEnv);

            // Materialize snapshot for this run (file paths, etc.)
            var evars = cfgProvider.ResolveAppSettings(appEnv);
            cfgProvider.InitializeEvars(evars); // one-time lock

            // Register in DI
            services.AddSingleton<IConfigProvider>(cfgProvider);
            services.AddSingleton(evars); // direct injection of ConfigEvars

            // === Pipeline core ===
            // The generic version covers all DTOs dynamically.
            services.AddScoped(typeof(PipelineContract<>));
            services.AddScoped(typeof(IPipelineCoordinator<>), typeof(PipelineCoordinator<>));

            // other services
            services.AddSingleton<SessionManager>();

            // Header Map
            services.AddSingleton<IHeaderMapService, HeaderMapService>();
            services.AddSingleton<IMapService, HeaderMapService>();             // marker interface
            services.AddSingleton<IMapService<HeaderMapDTO>, HeaderMapService>();

            // Catref Map
            services.AddSingleton<ICatrefMapService, CatrefMapService>();
            services.AddSingleton<IMapService, CatrefMapService>();             // marker interface
            services.AddSingleton<IMapService<CatrefMapDTO>, CatrefMapService>();

            // Discipline Map
            services.AddSingleton<IDisciplineMapService, DisciplineMapService>();
            services.AddSingleton<IMapService, DisciplineMapService>();             // marker interface
            services.AddSingleton<IMapService<DisciplineMapDTO>, DisciplineMapService>();

            // TOS Map
            services.AddSingleton<ITechnicalOrderStructureMapService, TechnicalOrderStructureMapService>();
            services.AddSingleton<IMapService, TechnicalOrderStructureMapService>();             // marker interface
            services.AddSingleton<IMapService<TechnicalOrderStructureMapDTO>, TechnicalOrderStructureMapService>();

            // Entity Map
            services.AddSingleton<IEntityMapService, EntityMapService>();
            services.AddSingleton<IMapService, EntityMapService>();             // marker interface
            services.AddSingleton<IMapService<EntityMapDTO>, EntityMapService>();

            // Hierarchy Map
            services.AddSingleton<IHierarchyTreeMapService, HierarchyTreeMapService>();
            services.AddSingleton<IMapService, HierarchyTreeMapService>();             // marker interface
            services.AddSingleton<IMapService<HierarchyTreeMapDTO>, HierarchyTreeMapService>();

            // TokenRegex Map
            services.AddSingleton<ITokenRegexMapService, TokenRegexMapService>();
            services.AddSingleton<IMapService, TokenRegexMapService>();             // marker interface
            services.AddSingleton<IMapService<TokenRegexMapDTO>, TokenRegexMapService>();

            // Role Map
            services.AddSingleton<IRoleMapService, RoleMapService>();
            services.AddSingleton<IMapService, RoleMapService>();             // marker interface
            services.AddSingleton<IMapService<RoleMapDTO>, RoleMapService>();

            // Tag Map
            services.AddSingleton<ISuffixMapService, SuffixMapService>();
            services.AddSingleton<IMapService, SuffixMapService>();             // marker interface
            services.AddSingleton<IMapService<SuffixMapDTO>, SuffixMapService>();

            // Tag Map
            services.AddSingleton<IAllowedTreeMapService, AllowedTreeMapService>();
            services.AddSingleton<IMapService, AllowedTreeMapService>();             // marker interface
            services.AddSingleton<IMapService<AllowedTreeMapDTO>, AllowedTreeMapService>();

            // Discipline Hierarchy Token Map
            services.AddSingleton<IDisciplineHierarchyTokenMapService, DisciplineHierarchyTokenMapService>();
            services.AddSingleton<IMapService, DisciplineHierarchyTokenMapService>();             // marker interface
            services.AddSingleton<IMapService<DisciplineHierarchyTokenMapDTO>, DisciplineHierarchyTokenMapService>();

            // Discipline Hierarchy Token Map
            services.AddSingleton<ICodificationMapService, CodificationMapService>();
            services.AddSingleton<IMapService, CodificationMapService>();             // marker interface
            services.AddSingleton<IMapService<CodificationMapDTO>, CodificationMapService>();

            // this one is for quick test, i dont know if it should be assigned with imapservice- it is not a map
            services.AddSingleton<TakeOverPointCacheService>();

            // Factories + Interfaces
            services.AddSingleton<IWalkerFactory, WalkerFactory>();
            services.AddSingleton<IProcessorFactory, ProcessorFactory>();
            services.AddSingleton<IWriterFactory, WriterFactory>();

            // Engines + Interfaces
            services.AddTransient<IEngine, CatrefEngine>();
            services.AddTransient<IEngine, DisciplineEngine>();
            services.AddTransient<IEngine, EntityEngine>();
            services.AddTransient<IEngine, HierarchyConsolidationHandler>();
            services.AddTransient<IEngine, HierarchyEngine>();
            services.AddTransient<IEngine, NamingEngine>();
            services.AddTransient<IEngine, OrientationEngine>();
            services.AddTransient<IEngine, PositionEngine>();
            services.AddTransient<IEngine, RoleEngine>();
            services.AddTransient<IEngine, SuffixEngine>();
            services.AddTransient<IEngine, TagEngine>();
            services.AddTransient<IEngine, TokenEngine>();
            services.AddTransient<IEngine, TransformationEngine>();
            services.AddTransient<IEngine, IdentityEngine>();
            services.AddTransient<IEngine, DispositionEngine>();

            // Parser Analysers for XML document-level analyzer
            services.AddTransient<IXDocumentAnalyzer, HeaderAnalyzer>();
            services.AddTransient<IXDocumentAnalyzer, BodyAnalyzer>();
            services.AddTransient<IXDocumentAnalyzer, FooterAnalyzer>();

            services.AddTransient<IXElementAnalyzer, AssemblyNodeAnalyzer>();
            services.AddTransient<IXElementAnalyzer, PartNodeAnalyzer>();

            // log regi
            services.AddSingleton<ISerilogService, SerilogService>();

            // Core diagnostic builder service
            services.AddSingleton<PipelineDiagnosticService>();

            // Diagnoser: transient (safe per pipeline run)
            services.AddTransient(typeof(IPipelineDiagnoser<>), typeof(PipelineDiagnoser<>));

            // Config paths watcher for CLI app configs reloads and user feedback (Interactive Mod only)
            services.AddSingleton<IConfigWatcher, ConfigWatcher>();
        }

        private static void RegisterCommands(IServiceCollection services)
        {
            // Bridge
            services.AddSingleton<CheckBridgeConnection>();
            services.AddSingleton<ConnectBridge>();
            services.AddSingleton<DisconnectBridge>();
            services.AddSingleton<ListBridgesAll>();
            services.AddSingleton<ListBridgesConnected>();
            services.AddSingleton<ListBridgesStatuses>();

            // Command
            services.AddSingleton<SendCommand>();
            services.AddSingleton<ReceiveCommand>();

            // Convert
            services.AddScoped<ProjectStructure>();
            services.AddScoped<TakeOverPoint>();
            //services.AddSingleton<CheckTxt>();
            //services.AddSingleton<ConvertXMLtoPMLMAC>();
            //services.AddSingleton<ConvertTXTtoPMLMAC>();

            // Environment
            services.AddSingleton<ListProjects>();
            services.AddSingleton<ValidateProjects>();
            services.AddSingleton<EnvControl>();

            // Connectors
            services.AddSingleton<PublishConnector>();

            //Protocols
            services.AddSingleton<PublishProtocol>();
            services.AddSingleton<InstallProtocol>();
            services.AddSingleton<UninstallProtocol>();
            services.AddSingleton<TestProtocol>();

            // Mappings
            services.AddSingleton<InspectCommand>();

            // Import
            // Basickly not ready, I don't want to allow import yet.

            // Launch
            services.AddSingleton<Batgen>();
            services.AddSingleton<Run>();
            services.AddSingleton<Validate>();
            // Standalone is not ready so no regi yet.

            // Session
            services.AddSingleton<ListRunningAvevaModules>();

            // Utility
            services.AddSingleton<ClearConsole>();
            services.AddSingleton<QuitApplication>();
            services.AddSingleton<FileInfoScanCommand>();
        }

        private static void ConfigureCliCommands(IConfigurator config)
        {
            config.SetApplicationName("Plant Gateway Aveva CLI");
            config.PropagateExceptions();
            config.ValidateExamples();

            config.AddBranch("pgedge", pgedge =>
            {
                pgedge.SetDescription("Plant Gateway Edge — interface to AVEVA E3D services.");

                // Sessions
                pgedge.AddBranch("sessions", sessions =>
                {
                    sessions.SetDescription("Manage or list active Aveva sessions.");
                    sessions.AddCommand<ListRunningAvevaModules>("list")
                        .WithDescription("List running Aveva E3D modules and suggest PipeBridge names.");
                });

                // Launch
                pgedge.AddBranch("launch", launch =>
                {
                    launch.SetDescription("Launch Aveva E3D with predefined configurations.");

                    launch.AddBranch("launcher", launcher =>
                    {
                        launcher.SetDescription("Launch using Aveva launcher profiles.");
                        launcher.AddCommand<Run>("execute")
                            .WithDescription("Start Aveva E3D using launcher.")
                            .WithAlias("exe")
                            .WithAlias("run");

                        launcher.AddCommand<Batgen>("generate")
                            .WithDescription("Generate .bat for launcher profile.")
                            .WithAlias("gen");

                        launcher.AddCommand<Validate>("validate")
                            .WithDescription("Validate launcher configuration.")
                            .WithAlias("val");
                    });

                    //launch.AddBranch("standalone", standalone =>
                    //{
                    //    standalone.SetDescription("Launch standalone session (no launcher).");
                    //    standalone.AddCommand<AvevaStandaloneLaunchCommand>("execute").WithDescription("Start Aveva standalone.");
                    //    standalone.AddCommand<AvevaStandaloneExeGenCommand>("generate").WithDescription("Generate .exe launcher.");
                    //    standalone.AddCommand<AvevaStandaloneValidateCommand>("validate").WithDescription("Validate standalone config.");
                    //});
                });

                // Import
                pgedge.AddBranch("import", import =>
                {
                    import.SetDescription("Import geometry and structure into Aveva E3D.");

                    import.AddBranch("top", top =>
                    {
                        top.SetDescription("Import Take Over Point geometry.");

                        top.AddCommand<FromFile>("from_file")
                            .WithDescription("Import from .txt file.")
                            .WithAlias("file");

                        top.AddCommand<Preview>("preview")
                            .WithDescription("Shows a dry-run preview of the ImportTOP ASM file.");


                        //top.AddBranch("database", db =>
                        //{
                        //    db.AddCommand<AvevaImportTOPDatabaseCommand>("execute").WithDescription("Import TOP from database.");
                        //    db.AddCommand<AvevaImportTOPDatabaseValidateCommand>("validate").WithDescription("Validate TOP database entry.");
                        //}).WithAlias("db");
                    });
                });

                // Export (reserved)
                //pgedge.AddBranch("export", export =>
                //{
                //    export.SetDescription("🔄 Export to Aveva or external systems. (Planned)");
                //});

                // Bridge
                pgedge.AddBranch("bridge", bridge =>
                {
                    bridge.SetDescription("Manage PipeBridge session connections.");

                    bridge.AddCommand<ConnectBridge>("connect").WithDescription("Manually connect to a PipeBridge.");
                    bridge.AddCommand<DisconnectBridge>("disconnect").WithDescription("Disconnect from a PipeBridge.");
                    bridge.AddCommand<CheckBridgeConnection>("check-connection").WithDescription("Check CMD/RSP pipe status.");
                    bridge.AddCommand<ListBridgesAll>("all").WithDescription("List all sessions (connected or not).");
                    bridge.AddCommand<ListBridgesConnected>("ready").WithDescription("List connected/ready sessions.");
                    bridge.AddCommand<ListBridgesStatuses>("status").WithDescription("Display detailed bridge info.");
                });

                // Environment
                pgedge.AddBranch("environment", env =>
                {
                    env.SetDescription("PGEdge environment variables and configuration checks.");

                    env.AddCommand<EnvControl>("validate")
                        .WithDescription("Validate shared configuration, schemas and DLLs for the selected environment.")
                        .WithAlias("val");

                    env.AddCommand<EnvControl>("get")
                        .WithDescription("Show PGEdge environment variables (PGEDGE_CLI_LAUNCHER, PGEDGE_ENV, PGEDGE_CONFIG_PATH) and resolved config paths.");

                    env.AddCommand<EnvControl>("set")
                        .WithDescription("Set PGEdge environment variables such as PGEDGE_CLI_LAUNCHER, PGEDGE_ENV or PGEDGE_CONFIG_PATH.");
                })
                .WithAlias("env");

                // Connector
                pgedge.AddBranch("connector", connector =>
                {
                    connector.SetDescription("Manage PGEdge connector settings.");

                    connector.AddCommand<PublishConnector>("publish")
                        .WithDescription("Publish the PGEdge connector.")
                        .WithAlias("push");
                });

                // Protocol
                pgedge.AddBranch("protocol", protocol =>
                {
                    protocol.SetDescription("Manage PGEdge protocols and protocol settings.");

                    protocol.AddCommand<PublishProtocol>("publish")
                        .WithDescription("Publish the PGEdge protocols.")
                        .WithAlias("push");

                    protocol.AddCommand<InstallProtocol>("install")
                        .WithDescription("Install pgedge protocol directly into Windows registry.")
                        .WithAlias("add");

                    protocol.AddCommand<UninstallProtocol>("uninstall")
                        .WithDescription("Remove the pgedge:// protocol registration.")
                        .WithAlias("remove");

                    protocol.AddCommand<TestProtocol>("validate")
                        .WithDescription("Check the pgedge:// protocol registration.")
                        .WithAlias("test");
                });

                // Convert (structure → macros)
                pgedge.AddBranch("convert", convert =>
                {
                    convert.SetDescription("Convert structure XML to .pmlmac macro.");

                    convert.AddCommand<ProjectStructure>("ProjectStructure")
                        .WithDescription("New function for all MSCAD actions.")
                        .WithAlias("projectstructure")
                        .WithAlias("structure")
                        .WithAlias("MSCAD")
                        .WithAlias("mscad")
                        .WithAlias("mcad");

                    convert.AddCommand<TakeOverPoint>("TakeOverPoint")
                        .WithDescription("New function for all TOP actions.")
                        .WithAlias("takeoverpoint")
                        .WithAlias("top")
                        .WithAlias("TOP");

                    //convert.AddCommand<ConvertXMLtoPMLMAC>("xml-to-pmlmac")
                    //    .WithDescription("Convert XML-structure to PMLMAC");

                    //convert.AddCommand<CheckTxt>("checktxt")
                    //    .WithDescription("Check TXT file against predefined schema");

                    //convert.AddCommand<ConvertTXTtoPMLMAC>("txt-to-pmlmac")
                    //    .WithDescription("Convert TXT-file to PMLMAC");

                });

                // Commands
                pgedge.AddBranch("commands", commands =>
                {
                    commands.SetDescription("Send commands to an E3D session.");
                    commands.AddCommand<SendCommand>("send")
                        .WithDescription("Send command string to PipeBridge.")
                        .WithAlias("exec")
                        .WithAlias("run");
                });

                pgedge.AddBranch("mapping", mapping =>
                {
                    mapping.SetDescription("Inspect mapping configurations.");
                    mapping.AddCommand<InspectCommand>("inspect")
                        .WithDescription("Inspect mapping definitions and rules.")
                        .WithAlias("check")
                        .WithAlias("view")
                        .WithAlias("show")
                        .WithAlias("get");
                })
                .WithAlias("mappings")
                .WithAlias("map")
                .WithAlias("maps");

                // Utilities
                pgedge.AddBranch("utility", utility =>
                {
                    utility.SetDescription("Utility tools for file inspection and diagnostics.");
                    utility.AddCommand<FileInfoScanCommand>("fileinfo")
                        .WithDescription("Scan file(s) or folder(s) and output metadata.")
                        .WithAlias("scan")
                        .WithAlias("inspect")
                        .WithAlias("check");
                });
            });

            // Clear screen
            config.AddCommand<ClearConsole>("clear")
                .WithDescription("Clear console output.")
                .WithAlias("cls")
                .WithAlias("clr");

            // Quit Application
            config.AddCommand<QuitApplication>("exit")
                .WithDescription("Quit the Application with Clean-Up Procedure.")
                .WithAlias("quit");
        }

        public static string[] TokenizeArgs(string input)
        {
            var args = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    // Toggle quoted mode but do NOT append the quote character
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    // Append as-is, including backslashes and slashes
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                args.Add(current.ToString());

            return args.ToArray();
        }
        private static void HandleReturnCode(int result, CommandApp app)
        {
            const int ShowHelpCode = 999;

            if (result == ShowHelpCode)
            {
                app.Run(new[] { "--help" });
            }
        }
        private static void RenderCliHeader(IServiceProvider provider)
        {
            try
            {
                // prevent crash if window is too small
                if (Console.LargestWindowWidth >= 120 && Console.LargestWindowHeight >= 40)
                    Console.SetWindowSize(width: 120, height: 40);
            }
            catch
            {
                // ignore if running in redirected mode (like CI/CD or pipe)
            }

            // Update status board values from services
            StatusBoard.Environment = AppEnvironmentResolver.ResolveAppEnv(string.Empty).ToString();
            StatusBoard.PipeBridge = "BridgeServer"; // or resolve from DI
            StatusBoard.ConfigWatcherOn = provider.GetService<IConfigWatcher>()?.IsRunning ?? false;

            // Print status + banner
            StatusBoard.RenderInitial();

            AnsiConsole.Write(
                new FigletText($"Plant Gateway\nAveva CLI")
                    .Centered()
                    .Color(Spectre.Console.Color.Green));
        }
        private static void StartConfigWatcher(IServiceProvider provider)
        {
            _watcher = provider.GetRequiredService<IConfigWatcher>();
            _watcher.Start();

            StatusBoard.ConfigWatcherOn = true;

            _watcher.ConfigChanged += (s, e) =>
            {
                if (e.IsCritical)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]⚠ Critical {e.Source} changed:[/] {e.Identifier}. Run 'pgedge reload' to apply.");
                }
                else
                {
                    AnsiConsole.MarkupLine(
                        $"[green]✔ Auto-reloaded non-critical config:[/] {e.Identifier}");
                    // You could also call auto-reload hooks here.
                }

                StatusBoard.Update();
            };
        }

        private static void StartBridgeServerReceiver(IServiceProvider provider)
        {
            const string pipeName = "BridgeServer";

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                        await server.WaitForConnectionAsync();

                        using var reader = new StreamReader(server);
                        var message = await reader.ReadLineAsync();

                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            Log.Information("📥 Received from E3D: {Message}", message);
                            AnsiConsole.MarkupLineInterpolated($"[bold green]📥 E3D → CLI:[/] {message}");

                            if (message.StartsWith("E3D_READY "))
                            {
                                var sessionName = message.Split(' ').Last();

                                try
                                {
                                    var connectCommand = provider.GetRequiredService<ConnectBridge>();
                                    Log.Debug("🔁 Using ConnectCommand with SessionManager hash: {Hash}", connectCommand.GetHashCode());

                                    var result = await connectCommand.ExecuteAsync(null, new ConnectBridge.Settings { Pipe = sessionName });

                                    if (result == 0)
                                        Log.Information("🔗 Session [{Session}] connected successfully.", sessionName);
                                    else
                                        Log.Warning("⚠️ Failed to connect session [{Session}]", sessionName);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "❌ Error while auto-connecting session: {Session}", sessionName);
                                }
                            }
                        }
                    }
                    catch (IOException ioEx)
                    {
                        Log.Warning(ioEx, "⚠️ I/O error on BridgeServer pipe.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "❌ Unexpected error in BridgeServer pipe loop.");
                    }

                    await Task.Delay(200); // small delay before listening again
                }
            });
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
    }
}
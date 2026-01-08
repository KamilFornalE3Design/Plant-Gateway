using DevExpress.Blazor;
using Microsoft.AspNetCore.Components.Server;
using PlantGateway.Presentation.WebApp.Application.Abstractions.Auth;
using PlantGateway.Presentation.WebApp.Application.Abstractions.Help;
using PlantGateway.Presentation.WebApp.Application.Abstractions.Layout;
using PlantGateway.Presentation.WebApp.Application.Abstractions.Navigation;
using PlantGateway.Presentation.WebApp.Application.Abstractions.Search;
using PlantGateway.Presentation.WebApp.Components;
using PlantGateway.Presentation.WebApp.Configuration.Extensions;
using PlantGateway.Presentation.WebApp.Features.Data.Services;
using PlantGateway.Presentation.WebApp.Features.Diagnostic.Services;
using PlantGateway.Presentation.WebApp.Features.Docs.Services;
using PlantGateway.Presentation.WebApp.Infrastructure.Auth;
using PlantGateway.Presentation.WebApp.Infrastructure.Help;
using PlantGateway.Presentation.WebApp.Infrastructure.Layout;
using PlantGateway.Presentation.WebApp.Infrastructure.Navigation;
using PlantGateway.Presentation.WebApp.Infrastructure.Search;
using PlantGateway.Presentation.WebApp.Services.UI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
})
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient();

// --- Application Options (your config) ---
builder.Services.AddApplicationOptions(builder.Configuration);

// --- Feature Services (Diagnostics, etc.) ---
builder.Services.AddSingleton<DiagnosticsUiService>(); 
builder.Services.AddScoped<ILayoutContextProvider, LayoutContextProvider>();
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();
builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddSingleton<IHelpMenuProvider, HelpMenuProvider>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserProfileNavigation, UserProfileNavigation>();
builder.Services.AddScoped<IAuthService, AuthService>(); 
builder.Services.AddScoped<IPlantGatewayDialogService, PlantGatewayDialogService>();
builder.Services.AddScoped<IPipelineRunnerService, PipelineRunnerService>();
builder.Services.AddSingleton<IDocumentationService, DocumentationService>();


builder.Services.AddScoped<IAvevaCliLauncher, AvevaCliLauncher>();

// --- Serilog ---
builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Verbose)
    .WriteTo.Console()
    .WriteTo.File("Logs/plantgateway-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(ctx.Configuration)
);


// --- DevExpress ---
builder.Services.AddDevExpressBlazor(config =>
{
    config.BootstrapVersion = BootstrapVersion.v5;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PlantGateway.Presentation.WebApp.Client._Imports).Assembly);

app.Run();
using System.Diagnostics;

namespace PlantGateway.Presentation.WebApp.Features.Data.Services
{
    public interface IAvevaCliLauncher
    {
        Task LaunchAsync(string arguments, CancellationToken cancellationToken = default);
    }

    public sealed class AvevaCliLauncher : IAvevaCliLauncher
    {
        // TODO: move to appsettings.json later
        private const string AvevaExePath =
            @"C:\Users\e3des\OneDrive - E3Design\Aplikacje\SMS Group\Production\SMSgroup.Aveva\SMSgroup.Aveva.exe";

        private readonly ILogger<AvevaCliLauncher> _log;

        public AvevaCliLauncher(ILogger<AvevaCliLauncher> log)
        {
            _log = log;
        }

        public Task LaunchAsync(string arguments, CancellationToken cancellationToken = default)
        {
            _log.LogInformation("AvevaCliLauncher: requested launch with args: {Args}", arguments);

            if (!File.Exists(AvevaExePath))
            {
                _log.LogError("AvevaCliLauncher: EXE not found at path {Path}", AvevaExePath);
                throw new FileNotFoundException($"EXE not found at {AvevaExePath}", AvevaExePath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = AvevaExePath,
                Arguments = arguments,
                UseShellExecute = true,              // simplest for a GUI/console app
                WorkingDirectory = Path.GetDirectoryName(AvevaExePath)
                                   ?? Environment.CurrentDirectory
            };

            _log.LogInformation("AvevaCliLauncher: starting process {FileName} in {WorkingDir}",
                startInfo.FileName, startInfo.WorkingDirectory);

            var process = Process.Start(startInfo);

            if (process is null)
            {
                _log.LogError("AvevaCliLauncher: Process.Start returned null");
                throw new InvalidOperationException("Process.Start returned null");
            }

            _log.LogInformation("AvevaCliLauncher: started process PID={Pid}", process.Id);

            return Task.CompletedTask;
        }
    }
}

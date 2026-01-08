using System.Diagnostics;
using PlantGateway.Presentation.WebApp.Features.Data.Models;

namespace PlantGateway.Presentation.WebApp.Features.Data.Services
{
    public class PipelineRunnerService : IPipelineRunnerService
    {
        public Task<string> RunPipelineAsync(PipelineRunRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.PlantGatewayCLIPath))
                throw new InvalidOperationException("PlantGatewayCLIPath is empty.");

            if (!File.Exists(req.PlantGatewayCLIPath))
                throw new FileNotFoundException("CLI executable not found.", req.PlantGatewayCLIPath);

            var psi = new ProcessStartInfo
            {
                FileName = req.PlantGatewayCLIPath,
                UseShellExecute = true, // open like double-click
                WorkingDirectory = Path.GetDirectoryName(req.PlantGatewayCLIPath) ?? Environment.CurrentDirectory
            };

            // This should open the EXE in its own window
            Process.Start(psi);

            return Task.FromResult("launched");
        }
    }
}

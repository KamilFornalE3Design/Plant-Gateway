using PlantGateway.Presentation.WebApp.Features.Data.Models;

namespace PlantGateway.Presentation.WebApp.Features.Data.Services
{
    public interface IPipelineRunnerService
    {
        Task<string> RunPipelineAsync(PipelineRunRequest request);
    }
}

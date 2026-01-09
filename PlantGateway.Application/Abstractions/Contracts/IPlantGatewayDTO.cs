using PlantGateway.Domain.Services.Engines.Abstractions;

namespace PlantGateway.Application.Abstractions.Contracts
{
    // Rename it to IPlantGatewayWorkItem
    public interface IPlantGatewayDTO
    {
        Guid Id { get; set; }
        string Name { get; set; }
        Guid TopLevelAssemblyId { get; set; }
        List<IEngineResult> EngineResults { get; set; }
    }
}

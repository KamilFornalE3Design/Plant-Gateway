using SMSgroup.Aveva.Config.Models.ValueObjects;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface IMapService
    {
        MapKeys Key { get; }
        string Description { get; }
        object GetMapUntyped();
        string GetFilePath();
    }

    public interface IMapService<TDto> : IMapService
    {
        new TDto GetMap();
    }
}

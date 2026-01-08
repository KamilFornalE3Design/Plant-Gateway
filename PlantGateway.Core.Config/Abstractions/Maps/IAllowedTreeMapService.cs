using SMSgroup.Aveva.Config.Models.DTO;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    public interface IAllowedTreeMapService
    {
        void Reload();
        AllowedTreeMapDTO GetMap();
        IReadOnlyList<string> GetAllowedChildren(string parentType);
        bool IsAllowedParent(string nodeType, string potentialParentType);
        bool IsAllowedChild(string nodeType, string potentialChildType);
        string GetDescription(string nodeType);
    }
}

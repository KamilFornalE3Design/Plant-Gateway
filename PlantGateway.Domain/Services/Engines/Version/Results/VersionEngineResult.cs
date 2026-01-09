using PlantGateway.Domain.Services.Engines.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Domain.Services.Engines.Version.Results
{
    public class VersionEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsSuccess => throw new NotImplementedException();

        public bool IsValid { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<string> Message { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<string> Warning { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<string> Error { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void AddError(string text)
        {
            throw new NotImplementedException();
        }

        public void AddMessage(string text)
        {
            throw new NotImplementedException();
        }

        public void AddWarning(string text)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;

namespace PlantGateway.Domain.Specifications.Maps
{
    public class CatrefMapDTO
    {
        public Dictionary<string, string> Nozz { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Elconn { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Datum { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Events.Configuration
{
    /// <summary>
    /// Event args for configuration changes (file, folder, or environment variable).
    /// </summary>
    public sealed class ConfigChangedEventArgs : EventArgs
    {
        public string Source { get; }
        public string Identifier { get; }
        public bool IsCritical { get; }
        public DateTime ChangedAt { get; }

        public ConfigChangedEventArgs(string source, string identifier, bool isCritical)
        {
            Source = source;
            Identifier = identifier;
            IsCritical = isCritical;
            ChangedAt = DateTime.UtcNow;
        }
    }
}

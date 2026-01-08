using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Presentation.CLI.Rendering.StatusBoard
{
    public sealed class StatusBoardState
    {
        public string Environment { get; set; } = "Unknown";
        public string PipeBridge { get; set; } = "BridgeServer";
        public bool ConfigWatcherOn { get; set; }
        public string Machine { get; set; } = System.Environment.MachineName;
        public string User { get; set; } = System.Environment.UserName;
    }
}

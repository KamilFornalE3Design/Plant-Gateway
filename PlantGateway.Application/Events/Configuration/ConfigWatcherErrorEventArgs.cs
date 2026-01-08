using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Events.Configuration
{
    public sealed class ConfigWatcherErrorEventArgs : EventArgs
    {
        public ConfigWatcherErrorEventArgs(string operation, string path, Exception exception)
        {
            Operation = operation;
            Path = path;
            Exception = exception;
        }

        public string Operation { get; }
        public string Path { get; }
        public Exception Exception { get; }
    }
}

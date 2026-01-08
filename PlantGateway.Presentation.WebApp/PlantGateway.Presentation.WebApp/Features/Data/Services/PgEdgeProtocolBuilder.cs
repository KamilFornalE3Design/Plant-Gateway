using System.Web;

namespace PlantGateway.Presentation.WebApp.Features.Data.Services
{
    public static class PgEdgeProtocolBuilder
    {
        /// <summary>
        /// Builds a pgedge:// URL with encoded arguments.
        /// </summary>
        /// <param name="command">The full CLI command to run.</param>
        /// <returns>A valid pgedge://run?cmd=... URL.</returns>
        public static string BuildCommandUrl(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be empty.", nameof(command));

            string encoded = Uri.EscapeDataString(command);
            return $"pgedge://run?cmd={encoded}";
        }

        /// <summary>
        /// Builds a pgedge:// URL from command + file + arguments.
        /// </summary>
        public static string BuildForMcad(string filePath, string mode = "dump", string format = "txt", string csys = "global")
        {
            string cmd = $"pgedge convert mcad -f \"{filePath}\" --mode {mode} --format {format} --CsysOption {csys}";
            return BuildCommandUrl(cmd);
        }

        /// <summary>
        /// Generic builder for key/value pairs.
        /// </summary>
        public static string BuildWithParams(params (string Key, string Value)[] pairs)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            foreach (var (Key, Value) in pairs)
                query[Key] = Value;

            string encoded = HttpUtility.UrlEncode(query.ToString());
            return $"pgedge://run?{encoded}";
        }
    }
}

using Serilog;

namespace PlantGateway.Infrastructure.Implementations.Logging
{
    /// <summary>
    /// Defines a service responsible for building and configuring Serilog loggers.
    /// </summary>
    public interface ISerilogService
    {
        /// <summary>
        /// Builds a Serilog logger based on the provided settings.
        /// Also assigns the global static <see cref="Log.Logger"/> for application-wide usage.
        /// </summary>
        /// <param name="settings">The logging settings mapped from configuration.</param>
        /// <returns>An <see cref="ILogger"/> instance configured with sinks.</returns>
        ILogger BuildLogger(SerilogSettings settings);
    }
}

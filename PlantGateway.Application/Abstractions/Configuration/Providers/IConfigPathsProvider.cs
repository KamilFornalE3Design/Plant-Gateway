using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Application.Abstractions.Configuration.Providers
{
    /// <summary>
    /// Provides access to resolved configuration file system locations used by the application.
    /// </summary>
    /// <remarks>
    /// This is an application-layer abstraction (port). Implementations are expected to live in Infrastructure
    /// and may source values from <c>appsettings.json</c>, environment variables, or other configuration systems.
    /// Consumers must not assume how paths are derived; they should only rely on the returned values.
    /// </remarks>
    public interface IConfigPathsProvider
    {
        /// <summary>
        /// Returns all configuration-related paths known to the provider, aggregated across supported categories.
        /// </summary>
        /// <remarks>
        /// This is intended for diagnostics and validation (e.g., "show me all config inputs used by this run").
        /// Implementations may include folders and files, and may expand to include additional categories in the future
        /// (e.g., shared DLL folders, AVEVA DLL folders, project-specific config folders).
        /// </remarks>
        IReadOnlyList<string> GetAllConfigPaths();

        /// <summary>
        /// Returns the SharedConfig root folder and all SharedConfigFiles entries as full paths.
        /// </summary>
        /// <remarks>
        /// The result typically includes the SharedConfig directory path plus each file referenced by configuration
        /// (e.g., JSON maps, dll.config.json, etc.). Non-existing entries may be omitted depending on implementation
        /// policy (e.g., only include paths that exist on disk).
        /// </remarks>
        IReadOnlyList<string> GetSharedConfigPaths();

        /// <summary>
        /// Returns the root folder path that contains shared configuration assets.
        /// </summary>
        /// <remarks>
        /// This is usually the value of a key such as <c>ConfigPaths:SharedConfig</c>.
        /// </remarks>
        string GetSharedConfigPath();

        /// <summary>
        /// Resolves a named SharedConfigFiles entry to a full file path.
        /// </summary>
        /// <param name="key">
        /// The configuration key under <c>SharedConfigFiles</c> (e.g., <c>"StructureMapDefault"</c>).
        /// </param>
        /// <returns>
        /// A full path combining the SharedConfig root folder and the configured file name for the specified key.
        /// </returns>
        /// <remarks>
        /// Implementations typically read <c>SharedConfigFiles:{key}</c> and combine it with the SharedConfig base path.
        /// </remarks>
        string GetSharedConfigFile(string key);
    }
}

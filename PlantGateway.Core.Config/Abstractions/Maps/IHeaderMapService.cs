

namespace PlantGateway.Core.Config.Abstractions.Maps
{
    /// <summary>
    /// Provides a uniform way to resolve external header mappings
    /// from configurable JSON definitions.
    ///
    /// A "header map" is a dictionary that translates raw file column
    /// names (e.g. TXT, CSV, Excel headers) into normalized, 
    /// AVEVA-friendly identifiers used inside Plant Gateway.
    ///
    /// The service supports multiple named maps (e.g. "TOP", "IMPORT"),
    /// so different input formats can be normalized with the same API.
    ///
    /// In addition to simple one-to-one mappings (<c>TryMap</c>),
    /// the service also supports "groups" of related headers 
    /// (<c>GetGroupMembers</c>). Groups are useful for cases where 
    /// multiple columns together form a single semantic unit,
    /// such as position coordinates (X, Y, Z) or orientation vectors (Ex, Ey, Ez).
    ///
    /// Consumers should never access JSON files directly — they depend only on
    /// this abstraction, which guarantees consistent mapping across the app.
    /// </summary>
    public interface IHeaderMapService
    {
        HeaderMapDTO GetMap();

        /// <summary>
        /// Maps a raw header name to its normalized form 
        /// according to the specified header map.
        /// </summary>
        /// <param name="mapKey">The name of the header map (e.g. "TOP").</param>
        /// <param name="rawHeader">The raw column name to translate.</param>
        /// <returns>
        /// The normalized header string if defined in the map; otherwise <c>null</c>.
        /// </returns>
        string TryMap(MapKeys mapKey, string rawHeader);

        /// <summary>
        /// Returns all raw header names belonging to a semantic group
        /// (e.g. POSITION, ORIENTATION) within a specified header map.
        /// </summary>
        /// <param name="mapKey">The name of the header map (e.g. "TOP").</param>
        /// <param name="groupName">The name of the group (e.g. "POSITION").</param>
        /// <returns>
        /// Array of raw header names defined in the group, or an empty array if none exist.
        /// </returns>
        string[] GetGroupMembers(MapKeys mapKey, string groupName);


        /// <summary>
        /// Maps a single TXT row into a DTO instance based on a header map definition.
        /// </summary>
        /// <typeparam name="TDto">The DTO type to construct.</typeparam>
        /// <param name="mapKey">Which header map to use (e.g. "TOP").</param>
        /// <param name="line">The raw TXT line.</param>
        /// <param name="rawHeaders">The raw TXT headers from the file.</param>
        /// <returns>The constructed DTO, or null if row is invalid/empty.</returns>
        TDto MapRow<TDto>(MapKeys mapKey, string line, string[] rawHeaders) where TDto : class;
    }
}

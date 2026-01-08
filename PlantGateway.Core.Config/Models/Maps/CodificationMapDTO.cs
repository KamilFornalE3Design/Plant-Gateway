using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PlantGateway.Core.Config.Models.Maps
{
    /// <summary>
    /// Represents the map of codifications loaded from configuration.
    /// </summary>
    public class CodificationMapDTO
    {
        private readonly Dictionary<string, CodificationDTO> _codifications = new Dictionary<string, CodificationDTO>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the collection of all codifications (case-insensitive).
        /// </summary>
        public IReadOnlyDictionary<string, CodificationDTO> Codifications => _codifications;

        /// <summary>
        /// Adds a codification entry to the map. Typically used only by the loader/service layer.
        /// </summary>
        public void Add(string key, CodificationDTO dto)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Codification key cannot be null or empty.", nameof(key));

            _codifications[key] = dto ?? throw new ArgumentNullException(nameof(dto));
        }
    }

    /// <summary>
    /// Represents a single codification definition within the structure.
    /// </summary>
    public class CodificationDTO
    {
        public string Code { get; private set; }
        [JsonIgnore] public CodificationType CodificationType { get; private set; }
        [JsonIgnore] public string ParentCode { get; private set; } = string.Empty;
        [JsonIgnore] public List<string> Children { get; } = new List<string>();

        [JsonConstructor]
        public CodificationDTO(string code)
        {
            Code = code ?? string.Empty;
            CodificationType = CodificationType.Undefined;
        }

        public void SetType(CodificationType type) => CodificationType = type;
        public void SetParent(string parentCode) => ParentCode = parentCode ?? string.Empty;
        public void AddChild(string childCode)
        {
            if (!string.IsNullOrWhiteSpace(childCode) && !Children.Contains(childCode))
                Children.Add(childCode);
        }
    }

    /// <summary>
    /// Defines the hierarchical level of a codification within the plant breakdown structure.
    /// </summary>
    public enum CodificationType
    {
        Undefined = 0,
        Plant = 1,
        PlantUnit = 2,
        PlantSection = 3,
        Equipment = 4
    }
}

using PlantGateway.Application.Abstractions.Contracts;
using PlantGateway.Domain.Services.Engines.Abstractions;
using PlantGateway.Core.Config.Attributes;

namespace PlantGateway.Application.Pipelines.WorkItems
{
    /// <summary>
    /// Represents a TakeOverPoint entry extracted from TXT or XML.
    /// Raw values are strings directly mapped from input headers.
    /// Processing stage enriches with orientation, catref, naming, etc.
    /// </summary>
    public class TakeOverPointDTO : IPlantGatewayDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid TopLevelAssemblyId { get; set; } // Top Level Assembly Id -> not in use yet
        public string Version { get; set; } = string.Empty;

        #region Raw Input Fields

        [HeaderKey("OwnerModelName")]
        public string OwnerModel { get; set; } = string.Empty;
        [HeaderKey("Description")]
        public string Description { get; set; } = string.Empty;

        [HeaderKey("PositionX")]
        public string PosX { get; set; } = string.Empty;
        [HeaderKey("PositionY")]
        public string PosY { get; set; } = string.Empty;
        [HeaderKey("PositionZ")]
        public string PosZ { get; set; } = string.Empty;

        [HeaderKey("VectorX")]
        public string VectorX { get; set; } = string.Empty;
        [HeaderKey("VectorY")]
        public string VectorY { get; set; } = string.Empty;
        [HeaderKey("VectorZ")]
        public string VectorZ { get; set; } = string.Empty;

        [HeaderKey("GeometryType")]
        public string GeometryType { get; set; } = string.Empty; // NOZZ | ELCONN | DATUM
        [HeaderKey("AvevaTag")]
        public string AvevaTag { get; set; } = string.Empty; // Raw input aveva tag, if any. Treated also as output, after normalization process pipeline (see Processor in Utilities!)

        [HeaderKey("RawCatref")]
        public string RawCatref { get; set; } = string.Empty; // Raw input catref, if any
        [HeaderKey("DN")]
        public string DN { get; set; } = string.Empty; // Diameter Nominal
        [HeaderKey("PN")]
        public string PN { get; set; } = string.Empty; // Pressure Nominal
        [HeaderKey("Norm")]
        public string Norm { get; set; } = string.Empty; // Standard/Norm
        [HeaderKey("ConnectionType")]
        public string ConnectionType { get; set; } = string.Empty; // e.g. Flanged, Welded, Screwed, Heated etc.


        [HeaderKey("RefNo")]
        public string RefNo { get; set; } = string.Empty; // filled by Aveva in format "=1234/5678"

        #endregion

        // Calculated/enriched fields (set later by processor)
        //[HeaderKey("Orientation")] -> this is moved to OrientationEngineResult
        //public string Orientation { get; set; } = string.Empty;// Output format of Orientation for Aveva
        //[HeaderKey("Position")] -> this is moved to PositionEngineResult
        //public string Position { get; set; } = string.Empty;// Output format of Position for Aveva
        //[HeaderKey("Catref")] -> this is moved to CatrefEngineResult
        //public string Catref { get; set; } = string.Empty; // Output format of Catref for Aveva
        //[HeaderKey("NormalizedName")] //-> this is moved to NamingEngineResult
        //public string NormalizedName { get; set; } = string.Empty; // Output format of Name for Aveva (possible double definition with AvevaTag!)

        public List<IEngineResult> EngineResults { get; set; } = new List<IEngineResult>();
    }
}

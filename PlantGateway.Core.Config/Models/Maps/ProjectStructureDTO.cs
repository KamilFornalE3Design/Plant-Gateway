using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Attributes;
using System;
using System.Collections.Generic;

namespace PlantGateway.Core.Config.Models.Maps
{
    public sealed class ProjectStructureDTO : IPlantGatewayDTO
    {
        public Guid Id { get; set; } // This element Id
        public Guid TopLevelAssemblyId { get; set; } // Top Level Assembly Id


        #region Raw Input Fields

        [RawInput, HeaderKey("Name")] // name
        public string Name { get; set; } = string.Empty;
        [RawInput, HeaderKey("AvevaTag")] // avevaTag / AVEVA_TAG
        public string AvevaTag { get; set; } = string.Empty;
        [RawInput, HeaderKey("DescriptionDE")] // DescrDE
        public string DescriptionDE { get; set; } = string.Empty;
        [RawInput, HeaderKey("DescriptionEN")] // DescrEN
        public string DescriptionEN { get; set; } = string.Empty;
        [RawInput, HeaderKey("ColorRGB")] // colorRGB = "191.0 191.0 191.0" 
        public string ColorRGB { get; set; } = string.Empty;
        [RawInput, HeaderKey("ColorTranslucency")] // colorTransp="0.0" (INT)?
        public string ColorTranslucency { get; set; } = string.Empty;
        [RawInput, HeaderKey("Geometry")] // geom="ARP01.TFM01.FES01.TFS01.CI01-PUP01.M01.stp" 
        public string Geometry { get; set; } = string.Empty;
        [RawInput, HeaderKey("windchillVersion")] // windchillVersion="-.2"
        public string Version { get; set; } = string.Empty;

        // all other undefined attributes - not in popular use but ready to be captured
        public List<string> OtherAttributes { get; set; } = new List<string>();

        #endregion

        #region Enriched Fields

        [EnrichedInput]
        public string OwnerAvevaTag { get; set; } = string.Empty;
        [EnrichedInput]
        public string FileFullPath { get; set; } = string.Empty;
        [EnrichedInput]
        public string FolderPath { get; set; } = string.Empty;

        #endregion

        #region Calculated Fields

        // Rotation Matrix 4x4
        [CalculatedField]
        public MatrixMetadata Matrix4x4 { get; set; } = new MatrixMetadata();
        // Rotation Global Matrix 4x4 (Top-level Assembly)
        [CalculatedField]
        public MatrixMetadata GlobalMatrix4x4 { get; set; } = new MatrixMetadata();

        // Rotation Absolute Matrix 4x4 (to WORL)
        [CalculatedField]
        public MatrixMetadata AbsoluteMatrix4x4 { get; set; } = new MatrixMetadata();

        // TransformedMatrix4x4 from (relative) Matrix4x4
        [CalculatedField]
        public double[,] TransformedMatrix4x4 { get; set; } = new double[4, 4];

        [CalculatedField]
        public bool IsComponent { get; set; } // true if leaf node (Part)

        // List of Engine Results that stores all Calculated Values.
        [CalculatedField]
        public List<IEngineResult> EngineResults { get; set; } = new List<IEngineResult>();

        #endregion

    }
    public sealed class MatrixMetadata
    {
        public double[,] Value { get; set; } = new double[4, 4];
        public bool HasInput { get; set; } = false;
        public MatrixOrigin Origin { get; set; } = MatrixOrigin.None;

        public bool IsValid =>
            Value != null &&
            Value.Length == 16 &&
            !IsZeroMatrix(Value);

        private static bool IsZeroMatrix(double[,] m)
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    if (Math.Abs(m[i, j]) > 1e-9) return false;
            return true;
        }
    }

    public enum MatrixOrigin
    {
        None,          // not present at all
        XmlInput,      // read from XML
        Derived,       // computed from parent
        Transformed,   // result of transformation
        Fallback       // default identity or synthetic
    }
}

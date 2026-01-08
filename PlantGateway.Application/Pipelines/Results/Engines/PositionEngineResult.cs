using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.PlannerBlocks.Position;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    public class PositionEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsValid { get; set; }
        public bool IsSuccess =>
            IsValid &&
            Error.Count == 0;

        /// <summary>
        /// Position string according to selected CsysOption.   
        /// It returns the appropriate position string based on the selected coordinate system option.
        /// </summary>
        public string Position
        {
            get
            {
                switch (CsysOption)
                {
                    case CsysOption.Absolute:
                        return PositionAbsolute;
                    case CsysOption.Global:
                        return PositionGlobal;
                    case CsysOption.Relative:
                        return PositionRelative;
                    case CsysOption.Transformed:
                        return PositionTransformed;
                    case CsysOption.WithOffset:
                        return PositionWithOffset;
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Position absolute to the Plant (WORL).
        /// It is always in strict reference to the WORLD coordinate system.
        /// Aveva OrientPositionation 'WRT WORL' is fixed.
        /// </summary>
        public string PositionAbsolute { get; set; } = string.Empty;
        /// <summary>
        /// Position global to the Top-level Assembly.
        /// It is in refernce to passed master element in the InputTarget.
        /// Aveva Position 'WRT ...' warries accordingly.
        /// </summary>
        public string PositionGlobal { get; set; } = string.Empty;
        /// <summary>
        /// Position relative Owner Element.
        /// It is always in strict reference to the OWNER coordinate system.
        /// Aveva Position 'WRT OWNER' is fixed.
        /// </summary>
        public string PositionRelative { get; set; } = string.Empty;
        /// <summary>
        /// Position relative to Any Element.
        /// It is the result of transforming from Owner-relative to Any-relative.
        /// Aveva Position 'WRT ...' warries accordingly.
        /// </summary>
        public string PositionTransformed { get; set; } = string.Empty;
        /// <summary>
        /// Position that is the result of any combination, additionaly rotated with Offset-Matrix.
        /// It is the result of applying an offset to the any result-Position.
        /// Aveva Position 'WRT ...' warries accordingly.
        /// </summary>
        public string PositionWithOffset { get; set; } = string.Empty;

        // object for position orign offsets
        public CsysReferenceOffset CsysReferenceOffset { get; set; }

        // "Relative" | "Absolute" | "Transformed"
        public CsysOption CsysOption { get; set; }
        public CsysWRT CsysWRT { get; set; }

        public List<string> Message { get; set; } = new List<string>();
        public List<string> Warning { get; set; } = new List<string>();
        public List<string> Error { get; set; } = new List<string>();

        public void AddMessage(string text) { if (!string.IsNullOrWhiteSpace(text)) Message.Add(text); }
        public void AddWarning(string text) { if (!string.IsNullOrWhiteSpace(text)) Warning.Add(text); }
        public void AddError(string text) { if (!string.IsNullOrWhiteSpace(text)) Error.Add(text); }
    }
}

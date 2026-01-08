using SMSgroup.Aveva.Config.Abstractions;
using PlantGateway.Core.Config.Models.PlannerBlocks;
using System;
using System.Collections.Generic;

namespace PlantGateway.Application.Pipelines.Results.Engines
{
    public class OrientationEngineResult : IEngineResult
    {
        public Guid SourceDtoId { get; set; }
        public bool IsValid { get; set; }
        public bool IsSuccess =>
            IsValid &&
            Error.Count == 0;

        /// <summary>
        /// Orientation according to selected CsysOption.
        /// Value assigned dynamically based on CsysOption, no need to get it with logic after values are calculated.
        /// </summary>
        public string Orientation
        {
            get
            {
                switch (CsysOption)
                {
                    case CsysOption.Absolute:
                        return OrientationAbsolute;
                    case CsysOption.Global:
                        return OrientationGlobal;
                    case CsysOption.Relative:
                        return OrientationRelative;
                    case CsysOption.Transformed:
                        return OrientationTransformed;
                    case CsysOption.WithOffset:
                        return OrientationWithOffset;
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Orientation absolute to the Plant (WORL).
        /// It is always in strict reference to the WORLD coordinate system.
        /// Aveva Orientation 'WRT WORL' is fixed.
        /// </summary>
        public string OrientationAbsolute { get; set; } = string.Empty;
        /// <summary>
        /// Orientation global to the Top-level Assembly.
        /// It is in refernce to passed master element in the InputTarget.
        /// Aveva Orientation 'WRT ...' warries accordingly.
        /// </summary>
        public string OrientationGlobal { get; set; } = string.Empty;
        /// <summary>
        /// Orientation relative Owner Element.
        /// It is always in strict reference to the OWNER coordinate system.
        /// Aveva Orientation 'WRT OWNER' is fixed.
        /// </summary>
        public string OrientationRelative { get; set; } = string.Empty;
        /// <summary>
        /// Orientation relative to Any Element.
        /// It is the result of transforming from Owner-relative to Any-relative.
        /// Aveva Orientation 'WRT ...' warries accordingly.
        /// </summary>
        public string OrientationTransformed { get; set; } = string.Empty;
        /// <summary>
        /// Orientation that is the result of any combination, additionaly rotated with Offset-Matrix.
        /// It is the result of applying an offset to the any result-orientation.
        /// Aveva Orientation 'WRT ...' warries accordingly.
        /// </summary>
        public string OrientationWithOffset { get; set; } = string.Empty;

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

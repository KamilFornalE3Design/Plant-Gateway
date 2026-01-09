using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.PlannerBlocks.Position;
using System.Globalization;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Engine responsible for computing formatted E/N/U or E/N/D coordinate strings.
    /// Handles both absolute (global) and relative (owner/zone/site) positioning.
    /// </summary>
    public sealed class PositionEngine : IEngine
    {
        private readonly CsysOption _csysOption;
        private readonly CsysWRT _csysWRT;
        private readonly CsysReferenceOffset _csysReferenceOffset;
        private readonly int _precision;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionEngine"/> class.
        /// </summary>
        /// <param name="csysOption">Absolute or Relative mode.</param>
        /// <param name="csysRelative">Defines the relative reference (Owner, Zone, Site, etc.).</param>
        /// <param name="csysReference">Origin point for relative calculations.</param>
        /// <param name="precision">Output precision in millimeters (default: 2).</param>
        public PositionEngine(CsysOption csysOption, CsysWRT csysWRT, CsysReferenceOffset csysReferenceOffset, int precision = 2)
        {
            _csysOption = csysOption;
            _csysWRT = csysWRT;
            _csysReferenceOffset = csysReferenceOffset ?? throw new ArgumentNullException(nameof(csysReferenceOffset));
            _precision = precision;
        }

        // ==========================================================
        // ===============  PUBLIC ENTRY POINTS  ====================
        // ==========================================================

        #region Public APIs

        public PositionEngineResult Process(IPlantGatewayDTO dtoUntyped, IPipelineContract contractUntyped)
        {
            if (dtoUntyped is ProjectStructureDTO typedProjectStructure && contractUntyped is PipelineContract<ProjectStructureDTO> typedProjectStructureContract)
                return Process(typedProjectStructure, typedProjectStructureContract);

            if (dtoUntyped is TakeOverPointDTO typedTakeOverPoint && contractUntyped is PipelineContract<TakeOverPointDTO> typedTakeOverPointContract)
                return Process(typedTakeOverPoint, typedTakeOverPointContract);

            throw new NotSupportedException("OrientationEngine only supports TakeOverPointDTO and ProjectStructureDTO at this time.");
        }

        /// <summary>
        /// Calculates the formatted position for a TakeOverPointDTO.
        /// </summary>
        public PositionEngineResult Process(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var result = new PositionEngineResult
            {
                CsysReferenceOffset = _csysReferenceOffset,
                CsysOption = _csysOption
            };

            result.PositionAbsolute = FormatAbsolute(dto, contract, result);
            result.PositionGlobal = FormatGlobal(dto, contract, result);
            result.PositionRelative = FormatRelative(dto, contract, result);
            result.PositionTransformed = FormatTransformed(dto, contract, result);
            result.PositionWithOffset = FormatWithOffset(dto, contract, result);

            return result;
        }

        /// <summary>
        /// Calculates the formatted position for a TakeOverPointDTO.
        /// </summary>
        public PositionEngineResult Process(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var result = new PositionEngineResult
            {
                SourceDtoId = dto.Id,
                CsysReferenceOffset = _csysReferenceOffset,
                CsysOption = _csysOption,
                CsysWRT = _csysWRT
            };

            result.PositionAbsolute = FormatAbsolute(dto, contract, result);
            result.PositionGlobal = FormatGlobal(dto, contract, result);
            result.PositionRelative = FormatRelative(dto, contract, result);
            result.PositionTransformed = FormatTransformed(dto, contract, result);
            result.PositionWithOffset = FormatWithOffset(dto, contract, result);

            return result;
        }

        #endregion

        // ==========================================================
        // ================  PRIVATE UTILITIES ======================
        // ==========================================================

        #region Private Utilities 

        #region ProjectStructureDTO Calculations 

        /// <summary>
        /// Formats an absolute position (global coordinates relative to WORL).
        /// </summary>
        private string FormatAbsolute(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, PositionEngineResult result)
        {
            try
            {
                // === 1️ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ ProjectStructureDTO is null in FormatAbsolute.");
                    return "ERROR: DTO missing.";
                }

                var matrixMeta = dto.AbsoluteMatrix4x4; // now MatrixMetadata

                // === 2️ Validate matrix availability and validity ===
                if (matrixMeta == null || !matrixMeta.HasInput || !matrixMeta.IsValid)
                {
                    result.Warning.Add("⚠️ AbsoluteMatrix not provided or invalid — skipping position computation.");
                    return "Skipped: No absolute matrix input.";
                }

                // === 3️ Extract position coordinates (E/N/U or X/Y/Z) ===
                double x = ParseCoordinate(matrixMeta.Value[3, 0]);
                double y = ParseCoordinate(matrixMeta.Value[3, 1]);
                double z = ParseCoordinate(matrixMeta.Value[3, 2]);

                // === 4️ Format the ENU-style position ===
                string pos = FormatENU(x, y, z);

                // === 5️ Determine the reference target (WRT) ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Absolute);
                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for ABSOLUTE position could not be resolved. Defaulted to WORL.");
                    wrt = "WORL";
                }

                // === 6️ Log success ===
                result.Message.Add($"✅ Computed ABSOLUTE position: {pos} (WRT {wrt}).");

                return $"{pos} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 7️ Catch unexpected errors ===
                result.Error.Add($"❌ Exception in FormatAbsolute (ProjectStructureDTO): {ex.Message}");
                return "ERROR: Absolute position computation failed.";
            }
        }

        /// <summary>
        /// Formats a global position (relative to the top-level assembly, e.g., SITE or SUB_SITE).
        /// </summary>
        private string FormatGlobal(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, PositionEngineResult result)
        {
            try
            {
                // === 1️⃣ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ ProjectStructureDTO is null in FormatGlobal.");
                    return "ERROR: DTO missing.";
                }

                var matrixMeta = dto.GlobalMatrix4x4; // now MatrixMetadata

                // === 2️⃣ Validate matrix availability and integrity ===
                if (matrixMeta == null || !matrixMeta.HasInput || !matrixMeta.IsValid)
                {
                    result.Warning.Add("⚠️ GlobalMatrix not provided or invalid — skipping position computation.");
                    return "Skipped: No global matrix input.";
                }

                // === 3️⃣ Extract position coordinates (E/N/U or X/Y/Z) ===
                double x = ParseCoordinate(matrixMeta.Value[3, 0]);
                double y = ParseCoordinate(matrixMeta.Value[3, 1]);
                double z = ParseCoordinate(matrixMeta.Value[3, 2]);

                // === 4️⃣ Format ENU-style position ===
                string pos = FormatENU(x, y, z);

                // === 5️⃣ Determine WRT target ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Global);
                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for GLOBAL position could not be resolved. Defaulted to WORL.");
                    wrt = "WORL";
                }

                // === 6️⃣ Log success ===
                result.Message.Add($"✅ Computed GLOBAL position: {pos} (WRT {wrt}).");

                // === 7️⃣ Return formatted result ===
                return $"{pos} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 8️⃣ Handle unexpected exceptions ===
                result.Error.Add($"❌ Exception in FormatGlobal (ProjectStructureDTO): {ex.Message}");
                return "ERROR: Global position computation failed.";
            }
        }

        /// <summary>
        /// Formats a relative position (local coordinates relative to the immediate owner).
        /// </summary>
        private string FormatRelative(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, PositionEngineResult result)
        {
            try
            {
                // === 1️⃣ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ ProjectStructureDTO is null in FormatRelative.");
                    return "ERROR: DTO missing.";
                }

                var matrixMeta = dto.Matrix4x4; // now MatrixMetadata

                // === 2️⃣ Validate matrix availability and integrity ===
                if (matrixMeta == null || !matrixMeta.HasInput || !matrixMeta.IsValid)
                {
                    result.Warning.Add("⚠️ Local Matrix not provided or invalid — skipping position computation.");
                    return "Skipped: No relative matrix input.";
                }

                // === 3️⃣ Extract coordinates from matrix ===
                double x = ParseCoordinate(matrixMeta.Value[3, 0]);
                double y = ParseCoordinate(matrixMeta.Value[3, 1]);
                double z = ParseCoordinate(matrixMeta.Value[3, 2]);

                // === 4️⃣ Format ENU-style string ===
                string pos = FormatENU(x, y, z);

                // === 5️⃣ Resolve WRT target ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Relative);
                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for RELATIVE position could not be resolved. Defaulted to OWNER.");
                    wrt = string.Empty; // avoid explicit OWNER to prevent compare issues
                }

                // === 6️⃣ Log success ===
                result.Message.Add($"✅ Computed RELATIVE position: {pos} (WRT {wrt}).");

                // === 7️⃣ Return formatted result ===
                return $"{pos} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 8️⃣ Catch unexpected exceptions ===
                result.Error.Add($"❌ Exception in FormatRelative (ProjectStructureDTO): {ex.Message}");
                return "ERROR: Relative position computation failed.";
            }
        }

        private string FormatTransformed(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, PositionEngineResult result)
        {
            if (dto.TransformedMatrix4x4 == null)
                return "Transformed Matrix not defined.";

            // Get X, Y , Z from the Matrix
            double x = ParseCoordinate(dto.TransformedMatrix4x4[3, 0]);
            double y = ParseCoordinate(dto.TransformedMatrix4x4[3, 1]);
            double z = ParseCoordinate(dto.TransformedMatrix4x4[3, 2]);

            string pos = FormatENU(x, y, z);
            return $"{pos} WRT WORL";
        }
        private string FormatWithOffset(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, PositionEngineResult result)
        {
            try
            {
                // === 1️⃣ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ ProjectStructureDTO is null in FormatWithOffset.");
                    return "ERROR: DTO missing.";
                }

                var matrixMeta = dto.GlobalMatrix4x4; // MatrixMetadata

                // === 2️⃣ Validate matrix availability and integrity ===
                if (matrixMeta == null || !matrixMeta.HasInput || !matrixMeta.IsValid)
                {
                    result.Warning.Add("⚠️ GlobalMatrix not provided or invalid — skipping position-with-offset computation.");
                    return "Skipped: No global matrix input.";
                }

                // === 3️⃣ Extract X, Y, Z and apply Csys offset ===
                double x = ParseCoordinate(matrixMeta.Value[3, 0]) - _csysReferenceOffset.OriginX;
                double y = ParseCoordinate(matrixMeta.Value[3, 1]) - _csysReferenceOffset.OriginY;
                double z = ParseCoordinate(matrixMeta.Value[3, 2]) - _csysReferenceOffset.OriginZ;

                // === 4️⃣ Format into ENU-style text ===
                string pos = FormatENU(x, y, z);

                // === 5️⃣ Log success ===
                result.Message.Add($"✅ Computed position with CSYS offset applied: {pos} (Origin offset X={_csysReferenceOffset.OriginX}, Y={_csysReferenceOffset.OriginY}, Z={_csysReferenceOffset.OriginZ}).");

                // === 6️⃣ Return formatted output ===
                return pos; // intentionally omit "WRT OWNER" to avoid AVEVA compare issues
            }
            catch (Exception ex)
            {
                // === 7️⃣ Catch unexpected exceptions ===
                result.Error.Add($"❌ Exception in FormatWithOffset (ProjectStructureDTO): {ex.Message}");
                return "ERROR: Position with offset computation failed.";
            }
        }


        #endregion

        #region TakeOverPointDTO Calculations 

        /// <summary>
        /// Formats an absolute position for a TakeOverPoint (always relative to WORL).
        /// </summary>
        private string FormatAbsolute(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, PositionEngineResult result)
        {
            try
            {
                // === 1️⃣ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ TakeOverPointDTO is null in FormatAbsolute.");
                    return "ERROR: DTO missing.";
                }

                // === 2️⃣ Validate coordinates ===
                // I must convert string input to double
                //if (double.IsNaN(dto.PosX) || double.IsNaN(dto.PosY) || double.IsNaN(dto.PosZ))
                //{
                //    result.Warning.Add("⚠️ Position values (X/Y/Z) not defined. Using WORL as fallback.");
                //    return "Position values not defined.";
                //}

                // === 3️⃣ Parse and format position ===
                double x = ParseCoordinate(dto.PosX);
                double y = ParseCoordinate(dto.PosY);
                double z = ParseCoordinate(dto.PosZ);
                string pos = FormatENU(x, y, z);

                // === 4️⃣ Resolve reference target ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Absolute);

                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for ABSOLUTE position could not be resolved. Defaulted to WORL.");
                    wrt = "WORL";
                }

                // === 5️⃣ Log success ===
                result.Message.Add($"✅ Computed ABSOLUTE position: {pos} (WRT {wrt}).");

                // === 6️⃣ Return formatted string ===
                return $"{pos} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 7️⃣ Catch unexpected errors ===
                result.Error.Add($"❌ Exception in FormatAbsolute (TakeOverPointDTO): {ex.Message}");
                return "ERROR: Absolute position computation failed.";
            }
        }


        /// <summary>
        /// Formats a global position for a TakeOverPoint (relative to the top-level assembly).
        /// </summary>
        private string FormatGlobal(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, PositionEngineResult result)
        {
            try
            {
                // === 1️⃣ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ TakeOverPointDTO is null in FormatGlobal.");
                    return "ERROR: DTO missing.";
                }

                // === 2️⃣ Validate coordinates ===
                // I must convert string input to double
                //if (double.IsNaN(dto.PosX) || double.IsNaN(dto.PosY) || double.IsNaN(dto.PosZ))
                //{
                //    result.Warning.Add("⚠️ Position values (X/Y/Z) not defined. Using WORL as fallback.");
                //    return "Position values not defined.";
                //}

                // === 3️⃣ Parse and format position ===
                double x = ParseCoordinate(dto.PosX);
                double y = ParseCoordinate(dto.PosY);
                double z = ParseCoordinate(dto.PosZ);
                string pos = FormatENU(x, y, z);

                // === 4️⃣ Determine reference target (WRT) ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Global);

                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for GLOBAL position could not be resolved. Defaulted to WORL.");
                    wrt = "WORL";
                }

                // === 5️⃣ Log successful computation ===
                result.Message.Add($"✅ Computed GLOBAL position: {pos} (WRT {wrt}).");

                // === 6️⃣ Return formatted position string ===
                return $"{pos} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 7️⃣ Handle unexpected runtime issues ===
                result.Error.Add($"❌ Exception in FormatGlobal (TakeOverPointDTO): {ex.Message}");
                return "ERROR: Global position computation failed.";
            }
        }


        /// <summary>
        /// Formats a relative position for a TakeOverPoint (local coordinates relative to OWNER).
        /// </summary>
        private string FormatRelative(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, PositionEngineResult result)
        {
            try
            {
                // === 1️⃣ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ TakeOverPointDTO is null in FormatRelative.");
                    return "ERROR: DTO missing.";
                }

                // === 2️⃣ Validate coordinates ===
                // I must convert string input to double
                //if (double.IsNaN(dto.PosX) || double.IsNaN(dto.PosY) || double.IsNaN(dto.PosZ))
                //{
                //    result.Warning.Add("⚠️ Position values (X/Y/Z) not defined. Using OWNER as fallback.");
                //    return "Position values not defined.";
                //}

                // === 3️⃣ Parse coordinates safely ===
                double x = ParseCoordinate(dto.PosX);
                double y = ParseCoordinate(dto.PosY);
                double z = ParseCoordinate(dto.PosZ);

                // === 4️⃣ Format in ENU (East/North/Up) system ===
                string pos = FormatENU(x, y, z);

                // === 5️⃣ Resolve reference target (WRT) ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Relative);

                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for RELATIVE position could not be resolved. Defaulted to OWNER.");
                    wrt = "OWNER";
                }

                // === 6️⃣ Log success ===
                result.Message.Add($"✅ Computed RELATIVE position: {pos} (WRT {wrt}).");

                // === 7️⃣ Return formatted output ===
                return $"{pos} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 8️⃣ Log unexpected exceptions ===
                result.Error.Add($"❌ Exception in FormatRelative (TakeOverPointDTO): {ex.Message}");
                return "ERROR: Relative position computation failed.";
            }
        }

        private string FormatTransformed(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, PositionEngineResult result)
        {
            return string.Empty;
        }
        private string FormatWithOffset(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, PositionEngineResult result)
        {
            return string.Empty;
        }

        #endregion

        /// <summary>
        /// Parses string coordinates to double using invariant culture.
        /// Returns 0 if parsing fails.
        /// </summary>
        private static double ParseCoordinate(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;

            // Optional: log or warn here if needed
            return 0.0;
        }

        /// <summary>
        /// Pass-through overload for already numeric double coordinates.
        /// </summary>
        private static double ParseCoordinate(double value)
        {
            // Defensive check for NaN or Infinity — normalize to 0
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0.0;

            return value;
        }

        private string FormatENU(double x, double y, double z)
        {
            return $"E {Fmt(x)}mm N {Fmt(y)}mm U {Fmt(z)}mm";
        }

        private string Fmt(double val)
        {
            return Math.Round(val, _precision).ToString($"F{_precision}", CultureInfo.InvariantCulture);
        }


        #region Reference Target Resolution

        /// <summary>
        /// Resolves the "WRT" (with respect to) reference label
        /// based on the coordinate system option and DTO context.
        /// Supports any DTO implementing IPlantGatewayDTO with RoleEngineResult data.
        /// </summary>
        private static string ResolveReferenceTarget<TDto>(TDto dto, PipelineContract<TDto> contract, CsysOption option) where TDto : IPlantGatewayDTO
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(contract);

            // === 1️⃣ Extract suffix (AvevaTag reference) ===
            var tagResult = dto.EngineResults.OfType<TagEngineResult>().FirstOrDefault();
            string avevaTag = tagResult?.FullTag?.Trim();

            string suffix = string.IsNullOrEmpty(avevaTag)
                ? string.Empty
                : $" of /{avevaTag}";

            // === 2️⃣ Determine base reference by option ===
            string baseRef = option switch
            {
                CsysOption.Absolute => "WORL",
                CsysOption.Relative => "OWNER",
                CsysOption.Global => ResolveGlobalReference(dto, contract),
                CsysOption.Transformed or CsysOption.WithOffset => "OWNER",
                _ => "WORL"
            };

            // === 3️⃣ Compose and return final reference ===
            return string.Concat(baseRef, suffix);
        }

        /// <summary>
        /// Resolves the "WRT" label specifically for Global coordinate system mode.
        /// </summary>
        private static string ResolveGlobalReference<TDto>(TDto dto, PipelineContract<TDto> contract) where TDto : IPlantGatewayDTO
        {
            // 1️ Self-relation → handled as OWNER
            if (dto.TopLevelAssemblyId == dto.Id)
                return "OWNER";

            // 2️ Retrieve top-level DTO
            var topLevelDto = contract.Items.FirstOrDefault(x => x.Id == dto.TopLevelAssemblyId);
            if (topLevelDto is null)
                return "WORL";

            // 3️ Extract role information from RoleEngineResult (preferred)
            var roleResult = topLevelDto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault();
            var avevaRole = roleResult?.AvevaType?.Trim();

            // 4️ Return role or fallback
            return !string.IsNullOrEmpty(avevaRole)
                ? avevaRole
                : string.Empty;
        }

        #endregion

        #endregion
    }
}

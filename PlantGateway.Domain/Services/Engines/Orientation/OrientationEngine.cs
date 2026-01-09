using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Abstractions;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.PlannerBlocks.Position;
using System.Globalization;
using System.Xml.Linq;

namespace PlantGateway.Domain.Services.Engines.NewFolder
{
    /// <summary>
    /// Engine responsible for calculating AVEVA orientation strings
    /// from rotation vectors or full rotation matrices.
    /// </summary>
    public class OrientationEngine : IEngine
    {
        private readonly CsysOption _csysOption;
        private readonly CsysWRT _csysWRT;
        private readonly CsysReferenceOffset _csysReferenceOffset;
        private readonly int _precision;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrientationEngine"/> class.
        /// </summary>
        /// <param name="csysOption">Absolute or Relative mode.</param>
        /// <param name="csysRelative">Defines the relative reference (Owner, Zone, Site, etc.).</param>
        /// <param name="csysReference">Origin point for relative calculations.</param>
        /// <param name="precision">Output precision in millimeters (default: 2).</param>
        public OrientationEngine(CsysOption csysOption, CsysWRT csysWRT, CsysReferenceOffset csysReferenceOffset, int precision = 2)
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

        public OrientationEngineResult Process(IPlantGatewayDTO dtoUntyped, IPipelineContract contractUntyped)
        {
            if (dtoUntyped is ProjectStructureDTO typedProjectStructure && contractUntyped is PipelineContract<ProjectStructureDTO> typedProjectStructureContract)
                return Process(typedProjectStructure, typedProjectStructureContract);

            if (dtoUntyped is TakeOverPointDTO typedTakeOverPoint && contractUntyped is PipelineContract<TakeOverPointDTO> typedTakeOverPointContract)
                return Process(typedTakeOverPoint, typedTakeOverPointContract);

            throw new NotSupportedException("OrientationEngine only supports TakeOverPointDTO and ProjectStructureDTO at this time.");
        }

        public OrientationEngineResult Process(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var result = new OrientationEngineResult
            {
                SourceDtoId = dto.Id,
                CsysReferenceOffset = _csysReferenceOffset,
                CsysOption = _csysOption,
                CsysWRT = _csysWRT
            };

            result.OrientationAbsolute = FormatAbsolute(dto, contract, result);
            result.OrientationGlobal = FormatGlobal(dto, contract, result);
            result.OrientationRelative = FormatRelative(dto, contract, result);
            result.OrientationTransformed = FormatTransformed(dto, contract, result);
            result.OrientationWithOffset = FormatWithOffset(dto, contract, result);

            return result;
        }

        public OrientationEngineResult Process(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var result = new OrientationEngineResult
            {
                SourceDtoId = dto.Id,
                CsysReferenceOffset = _csysReferenceOffset,
                CsysOption = _csysOption,
                CsysWRT = _csysWRT
            };

            result.OrientationAbsolute = FormatAbsolute(dto, contract, result);
            result.OrientationGlobal = FormatGlobal(dto, contract, result);
            result.OrientationRelative = FormatRelative(dto, contract, result);
            result.OrientationTransformed = FormatTransformed(dto, contract, result);
            result.OrientationWithOffset = FormatWithOffset(dto, contract, result);

            return result;
        }

        #endregion

        // ==========================================================
        // ================  PRIVATE UTILITIES ======================
        // ==========================================================

        #region Private Utilities 

        #region ProjectStructureDTO Calculations

        private string FormatAbsolute(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, OrientationEngineResult result)
        {
            try
            {
                // Define MatrixMetadata
                var matrixMeta = dto.AbsoluteMatrix4x4;

                // 1️⃣  Check for input availability and validity
                if (matrixMeta == null || !matrixMeta.HasInput || !matrixMeta.IsValid)
                {
                    result.Warning.Add("⚠️ AbsoluteMatrix not provided or invalid — skipping orientation computation.");
                    return "Skipped: No absolute matrix input.";
                }

                // 2️⃣  Compute orientation
                string orientation = Calculate(
                    matrixMeta.Value[0, 0], matrixMeta.Value[0, 1], matrixMeta.Value[0, 2],
                    matrixMeta.Value[1, 0], matrixMeta.Value[1, 1], matrixMeta.Value[1, 2],
                    matrixMeta.Value[2, 0], matrixMeta.Value[2, 1], matrixMeta.Value[2, 2]);

                // 3️⃣  Resolve WRT reference target
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Absolute);

                // 4️⃣  Diagnostics and return
                result.Message.Add($"✅ Computed ABSOLUTE orientation relative to {wrt}.");
                return $"{orientation} WRT {wrt}";
            }
            catch (Exception ex)
            {
                result.Error.Add($"❌ Error in FormatAbsolute: {ex.Message}");
                return "ERROR: Absolute orientation failed.";
            }
        }
        private string FormatGlobal(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, OrientationEngineResult result)
        {
            try
            {
                // === 1️⃣ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ DTO is null in FormatGlobal.");
                    return "ERROR: DTO missing.";
                }

                var matrixMeta = dto.GlobalMatrix4x4; // now MatrixMetadata

                // === 2️⃣ Validate matrix availability and integrity ===
                if (matrixMeta == null || !matrixMeta.HasInput || !matrixMeta.IsValid)
                {
                    result.Warning.Add("⚠️ GlobalMatrix not provided or invalid — skipping orientation computation.");
                    return "Skipped: No global matrix input.";
                }

                // === 3️⃣ Compute orientation from matrix ===
                string orientation = Calculate(
                    matrixMeta.Value[0, 0], matrixMeta.Value[0, 1], matrixMeta.Value[0, 2],
                    matrixMeta.Value[1, 0], matrixMeta.Value[1, 1], matrixMeta.Value[1, 2],
                    matrixMeta.Value[2, 0], matrixMeta.Value[2, 1], matrixMeta.Value[2, 2]);

                // === 4️⃣ Determine WRT target ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Global);
                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for GLOBAL orientation could not be resolved. Defaulted to WORL.");
                    wrt = "WORL";
                }

                // === 5️⃣ Log success ===
                result.Message.Add($"✅ Computed GLOBAL orientation relative to {wrt}.");
                return $"{orientation} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 6️⃣ Catch unexpected exceptions ===
                result.Error.Add($"❌ Exception in FormatGlobal: {ex.Message}");
                return "ERROR: Global orientation computation failed.";
            }
        }
        private string FormatRelative(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, OrientationEngineResult result)
        {
            try
            {
                // === 1️⃣ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ DTO is null in FormatRelative.");
                    return "ERROR: DTO missing.";
                }

                var matrixMeta = dto.Matrix4x4; // now MatrixMetadata

                // === 2️⃣ Validate matrix presence ===
                if (matrixMeta == null || !matrixMeta.HasInput || !matrixMeta.IsValid)
                {
                    result.Warning.Add("⚠️ Local Matrix not provided or invalid — skipping orientation computation.");
                    return "Skipped: No relative matrix input.";
                }

                // === 3️⃣ Compute orientation ===
                string orientation = Calculate(
                    matrixMeta.Value[0, 0], matrixMeta.Value[0, 1], matrixMeta.Value[0, 2],
                    matrixMeta.Value[1, 0], matrixMeta.Value[1, 1], matrixMeta.Value[1, 2],
                    matrixMeta.Value[2, 0], matrixMeta.Value[2, 1], matrixMeta.Value[2, 2]);

                // === 4️⃣ Determine WRT reference ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Relative);
                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for RELATIVE orientation could not be resolved. Defaulted to OWNER.");
                    wrt = "OWNER";
                }

                // === 5️⃣ Log success ===
                result.Message.Add($"✅ Computed RELATIVE orientation relative to {wrt}.");
                return $"{orientation} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 6️⃣ Catch unexpected exceptions ===
                result.Error.Add($"❌ Exception in FormatRelative: {ex.Message}");
                return "ERROR: Relative orientation computation failed.";
            }
        }

        private string FormatTransformed(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, OrientationEngineResult result)
        {
            string orientation = Calculate(
                dto.TransformedMatrix4x4[0, 0], dto.TransformedMatrix4x4[0, 1], dto.TransformedMatrix4x4[0, 2],
                dto.TransformedMatrix4x4[1, 0], dto.TransformedMatrix4x4[1, 1], dto.TransformedMatrix4x4[1, 2],
                dto.TransformedMatrix4x4[2, 0], dto.TransformedMatrix4x4[2, 1], dto.TransformedMatrix4x4[2, 2]);

            string transformedAgainst = "OWNER"; // default OWNER, to clarify the context

            // Return value with WORL suffix
            return $"{orientation} WRT {transformedAgainst}";
        }
        private string FormatWithOffset(ProjectStructureDTO dto, PipelineContract<ProjectStructureDTO> contract, OrientationEngineResult result)
        {
            string orientation = Calculate(
                dto.TransformedMatrix4x4[0, 0], dto.TransformedMatrix4x4[0, 1], dto.TransformedMatrix4x4[0, 2],
                dto.TransformedMatrix4x4[1, 0], dto.TransformedMatrix4x4[1, 1], dto.TransformedMatrix4x4[1, 2],
                dto.TransformedMatrix4x4[2, 0], dto.TransformedMatrix4x4[2, 1], dto.TransformedMatrix4x4[2, 2]);

            string transformedAgainst = "OWNER"; // default OWNER, to clarify the context

            // Return value with WORL suffix
            return $"{orientation} WRT {transformedAgainst}";
        }

        #endregion

        #region TakeOverPointDTO Calculations
        private string FormatAbsolute(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, OrientationEngineResult result)
        {
            try
            {
                // === 1️ Validate input ===
                if (dto == null)
                {
                    result.Error.Add("❌ TakeOverPointDTO is null in FormatAbsolute.");
                    return "ERROR: DTO missing.";
                }

                // === 2️ Validate required vectors ===
                if (dto.VectorX == null || dto.VectorY == null || dto.VectorZ == null)
                {
                    result.Warning.Add("⚠️ Orientation vectors (X/Y/Z) not fully defined. Using WORL as fallback.");
                    return "Orientation vectors not defined.";
                }

                // === 3️ Calculate the orientation ===
                string orientation = Calculate(dto.VectorX, dto.VectorY, dto.VectorZ);

                // === 4️ Determine reference target (WRT) ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Absolute);

                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for ABSOLUTE orientation could not be resolved. Defaulted to WORL.");
                    wrt = "WORL";
                }

                // === 5️ Log success ===
                result.Message.Add($"✅ Computed ABSOLUTE orientation relative to {wrt}.");
                return $"{orientation} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 6️ Catch unexpected exceptions ===
                result.Error.Add($"❌ Exception in FormatAbsolute (TakeOverPoint): {ex.Message}");
                return "ERROR: Absolute orientation computation failed.";
            }
        }
        private string FormatGlobal(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, OrientationEngineResult result)
        {
            try
            {
                // === 1️ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ TakeOverPointDTO is null in FormatGlobal.");
                    return "ERROR: DTO missing.";
                }

                // === 2️ Validate orientation vectors ===
                if (dto.VectorX == null || dto.VectorY == null || dto.VectorZ == null)
                {
                    result.Warning.Add("⚠️ Orientation vectors (X/Y/Z) not fully defined. Using WORL as fallback.");
                    return "Orientation vectors not defined.";
                }

                // === 3️ Compute orientation ===
                string orientation = Calculate(dto.VectorX, dto.VectorY, dto.VectorZ);

                // === 4️ Resolve "WRT" reference target ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Global);

                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for GLOBAL orientation could not be resolved. Defaulted to WORL.");
                    wrt = "WORL";
                }

                // === 5️ Log successful computation ===
                result.Message.Add($"✅ Computed GLOBAL orientation relative to {wrt}.");
                return $"{orientation} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 6️ Catch unexpected exceptions ===
                result.Error.Add($"❌ Exception in FormatGlobal (TakeOverPoint): {ex.Message}");
                return "ERROR: Global orientation computation failed.";
            }
        }
        private string FormatRelative(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, OrientationEngineResult result)
        {
            try
            {
                // === 1️ Validate DTO ===
                if (dto == null)
                {
                    result.Error.Add("❌ TakeOverPointDTO is null in FormatRelative.");
                    return "ERROR: DTO missing.";
                }

                // === 2️ Validate vectors ===
                if (dto.VectorX == null || dto.VectorY == null || dto.VectorZ == null)
                {
                    result.Warning.Add("⚠️ Orientation vectors (X/Y/Z) not fully defined. Using OWNER as fallback.");
                    return "Orientation vectors not defined.";
                }

                // === 3️ Compute orientation ===
                string orientation = Calculate(dto.VectorX, dto.VectorY, dto.VectorZ);

                // === 4️ Resolve reference target (WRT) ===
                string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Relative);

                if (string.IsNullOrEmpty(wrt))
                {
                    result.Warning.Add("⚠️ Reference target for RELATIVE orientation could not be resolved. Defaulted to OWNER.");
                    wrt = "OWNER";
                }

                // === 5️ Log success ===
                result.Message.Add($"✅ Computed RELATIVE orientation relative to {wrt}.");
                return $"{orientation} WRT {wrt}";
            }
            catch (Exception ex)
            {
                // === 6️ Catch unexpected errors ===
                result.Error.Add($"❌ Exception in FormatRelative (TakeOverPoint): {ex.Message}");
                return "ERROR: Relative orientation computation failed.";
            }
        }
        private string FormatTransformed(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, OrientationEngineResult result)
        {
            string orientation = Calculate(dto.VectorX, dto.VectorY, dto.VectorZ);

            // 2️ Determine reference target (WRT)
            string wrt = ResolveReferenceTarget(dto, contract, CsysOption.Transformed);

            // 3️ Return formatted string
            return $"{orientation} WRT {wrt}";
        }
        private string FormatWithOffset(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> contract, OrientationEngineResult result)
        {
            string orientation = Calculate(dto.VectorX, dto.VectorY, dto.VectorZ);

            // 2️ Determine reference target (WRT)
            string wrt = ResolveReferenceTarget(dto, contract, CsysOption.WithOffset);

            // 3️ Return formatted string
            return $"{orientation} WRT {wrt}";
        }

        #endregion

        #region Orientation Calculations

        /// <summary>
        /// Calculate orientation using three direction vectors (E, N, U).
        /// Input format: "[x, y, z]".
        /// </summary>
        /// <param name="vectorX">East (X) vector in string format.</param>
        /// <param name="vectorY">North (Y) vector in string format.</param>
        /// <param name="vectorZ">Up (Z) vector in string format.</param>
        /// <returns>Orientation string in AVEVA format.</returns>
        public string Calculate(string vectorX, string vectorY, string vectorZ)
        {
            var matrix = BuildRotationMatrix(vectorX, vectorY, vectorZ);

            double ex = matrix[0, 0], ey = matrix[0, 1], ez = matrix[0, 2];
            double nx = matrix[1, 0], ny = matrix[1, 1], nz = matrix[1, 2];
            double ux = matrix[2, 0], uy = matrix[2, 1], uz = matrix[2, 2];

            string eDir = ToAvevaDirectionString(ex, ey, ez, out _);
            string nDir = ToAvevaDirectionString(nx, ny, nz, out _);
            string uDir = ToAvevaDirectionString(ux, uy, uz, out var elevU);

            return BuildOrientationString("DTO", ex, ey, ez, nx, ny, nz, ux, uy, uz, eDir, nDir, uDir, elevU);
        }

        /// <summary>
        /// Calculate orientation using a full 3x3 rotation matrix.
        /// </summary>
        /// <returns>Orientation string in AVEVA format.</returns>
        private string Calculate(
            double ex, double ey, double ez,
            double nx, double ny, double nz,
            double ux, double uy, double uz)
        {
            string eDir = ToAvevaDirectionString(ex, ey, ez, out _);
            string nDir = ToAvevaDirectionString(nx, ny, nz, out _);
            string uDir = ToAvevaDirectionString(ux, uy, uz, out var elevU);

            string avevaOrientation = BuildOrientationString("DTO", ex, ey, ez, nx, ny, nz, ux, uy, uz, eDir, nDir, uDir, elevU);

            return avevaOrientation;
        }

        /// <summary>
        /// Builds the final AVEVA orientation string, handling flat-Z fallback and orthogonality checks.
        /// </summary>
        private string BuildOrientationString(
            string name,
            double ex, double ey, double ez,
            double nx, double ny, double nz,
            double ux, double uy, double uz,
            string eDir, string nDir, string uDir,
            double elevU)
        {
            bool useXZ = Math.Abs(elevU) >= 0.5;

            if (!useXZ)
            {
                //Console.WriteLine($"Z vector too flat (elevation={elevU:F2}°) for {name} — using Y as secondary axis");
            }

            if (!IsOrthogonal(ex, ey, ez, nx, ny, nz) ||
                !IsOrthogonal(nx, ny, nz, ux, uy, uz) ||
                !IsOrthogonal(ux, uy, uz, ex, ey, ez))
            {
                Console.WriteLine($"❗ Non-orthogonal matrix for {name} — invalid rotation.");
            }

            return useXZ
                ? $"X is {eDir} and Z is {uDir}"
                : $"X is {eDir} and Y is {nDir}";
        }

        private double[,] BuildRotationMatrix(string xVector, string yVector, string zVector)
        {
            double[] eVec = ParseVector(xVector, "X (East) vector");
            double[] nVec = ParseVector(yVector, "Y (North) vector");
            double[] uVec = ParseVector(zVector, "Z (Up) vector");

            return new double[,]
            {
                { eVec[0], eVec[1], eVec[2] },
                { nVec[0], nVec[1], nVec[2] },
                { uVec[0], uVec[1], uVec[2] }
            };
        }

        private double[] ParseVector(string raw, string label)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException($"Missing input vector for {label}");

            var cleaned = raw.Trim('[', ']', ' ');
            var parts = cleaned.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
                throw new FormatException($"Invalid vector format for {label}: '{raw}'");

            try
            {
                return new[]
                {
                    double.Parse(parts[0], CultureInfo.InvariantCulture),
                    double.Parse(parts[1], CultureInfo.InvariantCulture),
                    double.Parse(parts[2], CultureInfo.InvariantCulture)
                };
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to parse {label} '{raw}'", ex);
            }
        }

        private double GetDouble(XElement matrix, string key)
        {
            var attr = matrix.Attribute(key);
            return double.TryParse(attr?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var val) ? val : 0.0;
        }

        /// <summary>
        /// Converts a direction vector into an AVEVA orientation string.
        /// </summary>
        private string ToAvevaDirectionString(double x, double y, double z, out double elevation)
        {
            double magnitude = Math.Sqrt(x * x + y * y + z * z);
            if (magnitude < 1e-6)
            {
                elevation = 0;
                return "N";
            }

            double nx = x / magnitude;
            double ny = y / magnitude;
            double nz = z / magnitude;

            elevation = Math.Asin(nz) * (180 / Math.PI);
            double absElevation = Math.Abs(elevation);

            // Azimuth angle in XY plane
            double azimuth = Math.Atan2(nx, ny) * (180 / Math.PI);
            if (azimuth < 0) azimuth += 360;

            string horiz;
            double horizAngle;

            if (azimuth < 90)
            {
                horiz = "N";
                horizAngle = azimuth;
            }
            else if (azimuth < 180)
            {
                horiz = "E";
                horizAngle = azimuth - 90;
            }
            else if (azimuth < 270)
            {
                horiz = "S";
                horizAngle = azimuth - 180;
            }
            else
            {
                horiz = "W";
                horizAngle = azimuth - 270;
            }

            horizAngle = Math.Round(horizAngle, 2);

            string horizontal;
            if (horizAngle < 1e-3)
                horizontal = horiz;
            else
            {
                string next = horiz switch
                {
                    "N" => "E",
                    "E" => "S",
                    "S" => "W",
                    "W" => "N",
                    _ => "N"
                };
                horizontal = $"{horiz} {horizAngle.ToString("0.##", CultureInfo.InvariantCulture)} {next}";
            }

            string vertical = elevation > 0.5 ? "U" :
                              elevation < -0.5 ? "D" : string.Empty;

            if (string.IsNullOrEmpty(vertical))
                return horizontal;

            if (absElevation > 89.9)
                return vertical;

            return $"{horizontal} {absElevation.ToString("0.##", CultureInfo.InvariantCulture)} {vertical}";
        }

        private bool IsOrthogonal(double ax, double ay, double az, double bx, double by, double bz)
        {
            double dot = ax * bx + ay * by + az * bz;
            return Math.Abs(dot) < 1e-6;
        }

        #endregion

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

            string suffix = string.IsNullOrEmpty(avevaTag) ? string.Empty : $" of /{avevaTag}";

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

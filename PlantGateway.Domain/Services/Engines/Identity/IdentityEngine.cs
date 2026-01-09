using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Data.IdentityCache;
using SMSgroup.Aveva.Config.Models.Contracts;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using System.Text.RegularExpressions;

namespace SMSgroup.Aveva.Utilities.Engines
{
    /// <summary>
    /// Handles identity resolution for TakeOverPointDTOs using JsonIdentityCache.
    /// Ensures that same model + tag + geomrep combination reuses the same Guid Id.
    /// </summary>
    public sealed class IdentityEngine : IEngine
    {
        private readonly TakeOverPointCacheService _cacheService;

        public IdentityEngine(TakeOverPointCacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }
        public IdentityEngineResult Process(TakeOverPointDTO dto, PipelineContract<TakeOverPointDTO> pipelineContract)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // === 1️⃣ Retrieve TagEngineResult ===
            var tagResult = dto.EngineResults.OfType<TagEngineResult>().FirstOrDefault();
            if (tagResult == null)
                throw new InvalidOperationException("❌ TagEngineResult missing before IdentityEngine.");

            var fullTag = tagResult.FullTag?.Trim();
            var cache = _cacheService.GetCache();

            IdentityRecord record;
            bool isRestored = false;

            // === 2️⃣ Resolve or create identity ===
            if (!string.IsNullOrWhiteSpace(fullTag) && cache.TryGet(dto, pipelineContract, out record, out bool isExact))
            {
                // Existing record found
                isRestored = true;
                record = UpdateIdentityRecord(record, dto);
            }
            else
            {
                // Create new record
                var id = Guid.NewGuid();
                record = CreateIdentityRecord(pipelineContract, dto, id, fullTag ?? "<unnamed>");
            }

            // === 3️⃣ Always ensure valid Id and save to cache ===
            if (record.Id == Guid.Empty)
                record.Id = Guid.NewGuid();

            cache.AddOrUpdate(record.Id.ToString(), record);
            _cacheService.Save();

            // === 4️⃣ Propagate identity + message to all engine results ===
            AssignIdentityToResults(dto, record.Id, isRestored);

            // === 5️⃣ Build engine result ===
            var result = new IdentityEngineResult
            {
                SourceDtoId = dto.Id,
                Id = record.Id,
                Message = new List<string>()
            };

            result.AddMessage(isRestored
                ? $"♻️ Identity restored from cache → {record.Id}"
                : $"🆕 New identity assigned → {record.Id}");

            return result;
        }

        private IdentityRecord CreateIdentityRecord(PipelineContract<TakeOverPointDTO> pipelineContract, TakeOverPointDTO dto, Guid id, string finalName)
        {
            var sourceFile = Regex.Replace(Path.GetFileName(pipelineContract.Input.FilePath ?? string.Empty),
                               @"\.asm-\d+\.txt$", ".asm.txt",
                               RegexOptions.IgnoreCase);

            var record = new IdentityRecord
            {
                Id = id,
                AvevaTag = dto.AvevaTag,
                AvevaGeomRep = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault()?.AvevaType,
                GeneratedName = finalName,
                CatRef = dto.EngineResults.OfType<CatrefEngineResult>().FirstOrDefault()?.Catref,
                RefNo = dto.RefNo ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SourceFile = sourceFile, // File name, stripped of version
                SourceVersion = dto.Version, // Version from DTO and InputTarget (for now)
                OwnerModelName = dto.OwnerModel, // Name of owner Model in source software
                SuffixLetter = dto.EngineResults.OfType<SuffixEngineResult>().FirstOrDefault()?.Suffix.Substring(0, 1),
                SuffixIncrement = dto.EngineResults.OfType<SuffixEngineResult>().FirstOrDefault()?.Suffix.Substring(1),
                CsysDescription = dto.Description,
                IsValid = true
            };

            EvaluateIdentityRecordState(record, dto);

            return record;
        }

        public IdentityRecord UpdateIdentityRecord(IdentityRecord existing, TakeOverPointDTO dto)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // --- Update relevant fields ---
            existing.UpdatedAt = DateTime.UtcNow;
            existing.SourceVersion = dto.Version ?? existing.SourceVersion;
            existing.CsysDescription = dto.Description ?? existing.CsysDescription;
            existing.CatRef = dto.EngineResults.OfType<CatrefEngineResult>().FirstOrDefault()?.Catref;
            existing.RefNo = dto.RefNo ?? existing.RefNo;

            existing.Message.Add($"{DateTime.UtcNow} Updated existing record by {System.Environment.UserName}");

            EvaluateIdentityRecordState(existing, dto);

            return existing;
        }

        /// <summary>
        /// Evaluates an IdentityRecord against its TakeOverPointDTO source and populates
        /// Messages, Warnings, and Errors collections accordingly.
        /// </summary>
        private void EvaluateIdentityRecordState(IdentityRecord record, TakeOverPointDTO dto)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            record.Message ??= new List<string>();
            record.Warning ??= new List<string>();
            record.Error ??= new List<string>();

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // --- Core integrity checks ---
            if (string.IsNullOrWhiteSpace(record.AvevaTag))
                record.Error.Add($"❌ [{timestamp}] Missing AvevaTag (cannot identify record).");

            if (string.IsNullOrWhiteSpace(record.AvevaGeomRep))
                record.Warning.Add($"⚠️ [{timestamp}] Geometry type undefined for {record.AvevaTag}.");

            if (string.IsNullOrWhiteSpace(record.CatRef))
                record.Warning.Add($"⚠️ [{timestamp}] CATREF not resolved for {record.AvevaTag}.");

            if (string.IsNullOrWhiteSpace(record.SourceFile))
                record.Warning.Add($"⚠️ [{timestamp}] Source file not set (record origin unclear).");

            if (record.CreatedAt == default)
                record.Warning.Add($"⚠️ [{timestamp}] CreatedAt timestamp missing or default.");

            if (record.SuffixLetter == null || record.SuffixIncrement == null)
                record.Warning.Add($"⚠️ [{timestamp}] Suffix incomplete for {record.AvevaTag}.");

            if (record.Id == Guid.Empty)
                record.Error.Add($"❌ [{timestamp}] Identifier empty for {record.AvevaTag}.");

            // --- Cross-check with DTO ---
            if (!string.Equals(record.AvevaTag, dto.AvevaTag, StringComparison.OrdinalIgnoreCase))
                record.Error.Add($"❌ [{timestamp}] AvevaTag mismatch: record='{record.AvevaTag}' vs dto='{dto.AvevaTag}'.");

            var dtoCatRef = dto.EngineResults.OfType<CatrefEngineResult>().FirstOrDefault()?.Catref;
            if (!string.Equals(record.CatRef, dtoCatRef, StringComparison.OrdinalIgnoreCase))
                record.Message.Add($"ℹ️ [{timestamp}] CatRef updated: '{record.CatRef}' → '{dtoCatRef}'.");

            // --- Versioning & Updates ---
            if (!string.Equals(record.SourceVersion, dto.Version, StringComparison.OrdinalIgnoreCase))
                record.Message.Add($"ℹ️ [{timestamp}] SourceVersion changed from '{record.SourceVersion}' to '{dto.Version}'.");

            // --- Geometry logic example ---
            if (record.AvevaGeomRep?.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase) == true)
                record.Error.Add($"❌ [{timestamp}] Unsupported geometry type for {record.AvevaTag}.");

            // --- OwnerModel sanity ---
            if (string.IsNullOrWhiteSpace(record.OwnerModelName))
                record.Warning.Add($"⚠️ [{timestamp}] Owner model name not defined.");

            // --- Final state decision ---
            record.IsValid = record.Error.Count == 0;
            if (!record.IsValid)
                record.Message.Add($"🚫 [{timestamp}] Record marked invalid due to {record.Error.Count} error(s).");
            else if (record.Warning.Count > 0)
                record.Message.Add($"⚠️ [{timestamp}] Record valid with {record.Warning.Count} warning(s).");
            else
                record.Message.Add($"✅ [{timestamp}] Record validated successfully.");
        }

        /// <summary>
        /// Assigns the resolved identity to all dependent engine results (Role, Suffix, etc.)
        /// </summary>
        private void AssignIdentityToResults(TakeOverPointDTO dto, Guid identityId, bool isRestored)
        {
            string statusText = isRestored
                ? $"♻️ Restored existing Identity Id: {identityId}"
                : $"🆕 Assigned new Identity Id: {identityId}";

            dto.Id = identityId;

            foreach (var engineResult in dto.EngineResults)
            {
                switch (engineResult)
                {
                    case TokenEngineResult tokenResult:
                        tokenResult.SourceDtoId = identityId;
                        tokenResult.AddMessage(statusText);
                        break;

                    case RoleEngineResult roleResult:
                        roleResult.SourceDtoId = identityId;
                        roleResult.AddMessage(statusText);
                        break;

                    case NamingEngineResult namingResult:
                        namingResult.SourceDtoId = identityId;
                        namingResult.AddMessage(statusText);
                        break;

                    case SuffixEngineResult suffixResult:
                        suffixResult.SourceDtoId = identityId;
                        suffixResult.AddMessage(statusText);
                        break;

                    // extend as needed
                    case DisciplineEngineResult disciplineResult:
                        disciplineResult.SourceDtoId = identityId;
                        disciplineResult.AddMessage(statusText);
                        break;

                    case TagEngineResult tagResult:
                        tagResult.SourceDtoId = identityId;
                        tagResult.AddMessage(statusText);
                        break;

                    case PositionEngineResult positionResult:
                        positionResult.SourceDtoId = identityId;
                        positionResult.AddMessage(statusText);
                        break;

                    case OrientationEngineResult orientationResult:
                        orientationResult.SourceDtoId = identityId;
                        orientationResult.AddMessage(statusText);
                        break;
                }
            }
        }
    }
}

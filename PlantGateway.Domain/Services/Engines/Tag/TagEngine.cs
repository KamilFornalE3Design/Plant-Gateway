using PlantGateway.Domain.Services.Engines.Abstractions;
using SMSgroup.Aveva.Config.Models.DTO;
using SMSgroup.Aveva.Config.Models.EngineResults;
using SMSgroup.Aveva.Config.Models.Extensions;

namespace PlantGateway.Domain.Services.Engines.NewFolder
{
    /// <summary>
    /// Builds the final AVEVA Tag by combining BaseName (from NamingEngine)
    /// with the calculated TagSuffix (from SuffixEngine).
    /// </summary>
    public sealed class TagEngine : IEngine
    {
        public TagEngine()
        {
            // No map dependencies — this engine only merges validated results.
        }

        public TagEngineResult Process(TakeOverPointDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var namingResult = dto.EngineResults.OfType<NamingEngineResult>().FirstOrDefault();
            var suffixResult = dto.EngineResults.OfType<SuffixEngineResult>().FirstOrDefault();
            var roleResult = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault();

            if (namingResult == null)
                throw new InvalidOperationException("❌ NamingEngineResult missing before TagEngine.");
            if (suffixResult == null)
                throw new InvalidOperationException("❌ SuffixEngineResult missing before TagEngine.");

            var baseName = namingResult.BaseName ?? string.Empty;
            var normalizedBase = namingResult.NormalizedBaseName ?? string.Empty;
            var suffix = suffixResult.Suffix ?? string.Empty;
            var role = roleResult?.AvevaType ?? "UNKNOWN";

            // Build final tag (used as identity key)
            var fullTag = string.IsNullOrWhiteSpace(suffix)
                ? baseName
                : $"{baseName}-{suffix}";

            var isValid = namingResult.IsValid && suffixResult.IsValid && !string.IsNullOrWhiteSpace(fullTag);

            var result = new TagEngineResult
            {
                SourceDtoId = dto.Id,
                Role = role,
                BaseName = baseName,
                TagSuffix = suffix,
                FullTag = fullTag,
                IsValid = isValid
            }.Also(r => r.AddMessage(isValid ? $"✅ Final AVEVA Tag built → {fullTag}" : $"⚠️ Tag build incomplete for {dto.AvevaTag}"));

            return result;
        }
        public TagEngineResult Process(ProjectStructureDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // === 1️ Retrieve dependencies ===
            var namingResult = dto.EngineResults.OfType<NamingEngineResult>().FirstOrDefault();
            var suffixResult = dto.EngineResults.OfType<SuffixEngineResult>().FirstOrDefault();
            var roleResult = dto.EngineResults.OfType<RoleEngineResult>().FirstOrDefault();

            if (namingResult == null)
                throw new InvalidOperationException("❌ NamingEngineResult missing before TagEngine.");

            if (suffixResult == null)
                throw new InvalidOperationException("❌ SuffixEngineResult missing before TagEngine.");

            var baseName = namingResult.BaseName?.Trim() ?? string.Empty;
            var normalizedBase = namingResult.NormalizedBaseName?.Trim() ?? string.Empty;
            var suffix = suffixResult.Suffix?.Trim() ?? string.Empty;
            var role = roleResult?.AvevaType ?? "UNKNOWN";

            // === 3️ Compose final tag ===
            // The base name and suffix are already formatted by their respective engines.
            // TagEngine only merges them safely.
            string fullTag = string.Concat(baseName, suffix);

            // === 4️ Validation ===
            bool isValid =
                namingResult.IsValid &&
                suffixResult.IsValid &&
                !string.IsNullOrEmpty(baseName);

            // === 5️ Build result ===
            return new TagEngineResult
            {
                SourceDtoId = dto.Id,
                Role = role,
                BaseName = baseName,
                TagSuffix = suffix,
                FullTag = fullTag,
                IsValid = isValid,
                Message = new List<string>()
            }
            .Also(r => r.AddMessage(isValid
                ? $"✅ Final tag built for role '{role}' → {fullTag}"
                : $"⚠️ Tag built with issues for role '{role}' → {fullTag}"))
            .When(r => !isValid,
                r => r.AddWarning($"⚠️ Check input data for missing components when building tag for '{role}'."));
        }
    }
}

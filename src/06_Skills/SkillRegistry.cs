using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DNAPlatform.Skills.BuiltIn;
using DNAPlatform.Skills.Bioinformatics;

namespace DNAPlatform.Skills
{
    /// <summary>
    /// Registry for managing skill lifecycle - registration, discovery, and execution
    /// </summary>
    public interface ISkillRegistry
    {
        Task RegisterSkill(string skillId, ISkill skill);
        Task<bool> UnregisterSkill(string skillId);
        Task<ISkill?> GetSkill(string skillId);
        Task<IEnumerable<SkillMetadata>> ListSkills();
        Task<IEnumerable<SkillMetadata>> ListSkillsByCategory(string category);
        Task<SkillOutput> ExecuteSkill(string skillId, Dictionary<string, object>? inputs = null);
        bool HasSkill(string skillId);
    }

    /// <summary>
    /// Thread-safe skill registry with built-in skill auto-registration
    /// </summary>
    public class SkillRegistry : ISkillRegistry
    {
        private readonly ConcurrentDictionary<string, ISkill> _skills = new();
        private readonly ILogger<SkillRegistry>? _logger;

        public SkillRegistry(ILogger<SkillRegistry>? logger = null)
        {
            _logger = logger;
            RegisterBuiltInSkills();
        }

        /// <summary>
        /// Auto-register all built-in skills
        /// </summary>
        private void RegisterBuiltInSkills()
        {
            // UI Components
            var msgBoxSkill = new MsgBoxAlertSkill();
            _skills.TryAdd(msgBoxSkill.SkillId, msgBoxSkill);

            // Data Processing Skills
            var dataTransformSkill = new DataTransformSkill();
            _skills.TryAdd(dataTransformSkill.SkillId, dataTransformSkill);

            var validationSkill = new ValidationSkill();
            _skills.TryAdd(validationSkill.SkillId, validationSkill);

            var mergeResultsSkill = new MergeResultsSkill();
            _skills.TryAdd(mergeResultsSkill.SkillId, mergeResultsSkill);

            // Bioinformatics Skills
            var fastaLoaderSkill = new FastaLoaderSkill();
            _skills.TryAdd(fastaLoaderSkill.SkillId, fastaLoaderSkill);

            var sequenceAlignerSkill = new SequenceAlignerSkill();
            _skills.TryAdd(sequenceAlignerSkill.SkillId, sequenceAlignerSkill);

            _logger?.LogInformation("Registered {Count} built-in skills", _skills.Count);

            // Dump the catalogue at startup so the CLI console immediately shows
            // which skills are available (and makes it obvious when one is missing).
            foreach (var skill in _skills.Values.OrderBy(s => s.SkillId))
            {
                _logger?.LogInformation(
                    "  • {SkillId,-22} {Name} [{Category}]",
                    skill.SkillId,
                    skill.Name,
                    skill.Category);
            }
        }

        /// <summary>
        /// Attaches the structured <see cref="SkillInputField"/> list to the metadata so the
        /// GUI can render a typed parameter editor. Done centrally here so individual skills
        /// only need to override <see cref="ISkill.GetInputFields"/>.
        /// </summary>
        private static SkillMetadata BuildMetadata(ISkill skill)
        {
            var metadata = skill.GetMetadata();
            metadata.InputFields = skill.GetInputFields() ?? new List<SkillInputField>();
            return metadata;
        }

        public Task RegisterSkill(string skillId, ISkill skill)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                throw new ArgumentException("Skill ID cannot be empty", nameof(skillId));

            if (skill == null)
                throw new ArgumentNullException(nameof(skill));

            if (_skills.TryAdd(skillId, skill))
            {
                _logger?.LogInformation("Registered skill: {SkillId} ({Name})", skillId, skill.Name);
            }
            else
            {
                _logger?.LogWarning("Skill already registered: {SkillId}", skillId);
            }

            return Task.CompletedTask;
        }

        public Task<bool> UnregisterSkill(string skillId)
        {
            if (_skills.TryRemove(skillId, out _))
            {
                _logger?.LogInformation("Unregistered skill: {SkillId}", skillId);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<ISkill?> GetSkill(string skillId)
        {
            _skills.TryGetValue(skillId, out var skill);
            return Task.FromResult(skill);
        }

        public Task<IEnumerable<SkillMetadata>> ListSkills()
        {
            var skills = _skills.Values.Select(BuildMetadata).ToList();
            return Task.FromResult<IEnumerable<SkillMetadata>>(skills);
        }

        public Task<IEnumerable<SkillMetadata>> ListSkillsByCategory(string category)
        {
            var skills = _skills.Values
                .Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .Select(BuildMetadata)
                .ToList();

            return Task.FromResult<IEnumerable<SkillMetadata>>(skills);
        }

        public async Task<SkillOutput> ExecuteSkill(string skillId, Dictionary<string, object>? inputs = null)
        {
            if (!_skills.TryGetValue(skillId, out var skill))
            {
                _logger?.LogWarning("Skill not found: {SkillId}", skillId);
                return SkillOutput.CreateError($"Skill not found: '{skillId}'");
            }

            try
            {
                var providedKeys = inputs == null ? "-" : string.Join(", ", inputs.Keys);
                _logger?.LogInformation(
                    "Executing skill {SkillId} with params [{Params}]",
                    skillId,
                    providedKeys);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await skill.Execute(inputs);
                stopwatch.Stop();

                // The skill may not measure itself - stamp the wall-clock duration so the
                // CLI log, the API response and the web log viewer all agree.
                if (result.DurationMs <= 0)
                    result.DurationMs = stopwatch.ElapsedMilliseconds;

                if (result.Success)
                {
                    _logger?.LogInformation(
                        "Skill {SkillId} succeeded in {DurationMs}ms",
                        skillId,
                        result.DurationMs);
                }
                else
                {
                    _logger?.LogWarning(
                        "Skill {SkillId} failed: {Error}",
                        skillId,
                        result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing skill: {SkillId}", skillId);
                return SkillOutput.CreateError($"Skill execution failed: {ex.Message}");
            }
        }

        public bool HasSkill(string skillId)
        {
            return _skills.ContainsKey(skillId);
        }
    }
}
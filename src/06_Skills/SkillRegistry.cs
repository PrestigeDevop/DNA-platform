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
            var skills = _skills.Values.Select(s => s.GetMetadata()).ToList();
            return Task.FromResult<IEnumerable<SkillMetadata>>(skills);
        }

        public Task<IEnumerable<SkillMetadata>> ListSkillsByCategory(string category)
        {
            var skills = _skills.Values
                .Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.GetMetadata())
                .ToList();

            return Task.FromResult<IEnumerable<SkillMetadata>>(skills);
        }

        public async Task<SkillOutput> ExecuteSkill(string skillId, Dictionary<string, object>? inputs = null)
        {
            if (!_skills.TryGetValue(skillId, out var skill))
            {
                return SkillOutput.CreateError($"Skill not found: '{skillId}'");
            }

            try
            {
                _logger?.LogInformation("Executing skill: {SkillId}", skillId);
                var result = await skill.Execute(inputs);
                _logger?.LogInformation("Skill {SkillId} executed: Success={Success}", skillId, result.Success);
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
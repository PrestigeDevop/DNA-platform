using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DNAPlatform.Skills
{
    /// <summary>
    /// Output result from skill execution
    /// </summary>
    public class SkillOutput
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public long DurationMs { get; set; }

        public static SkillOutput CreateSuccess(Dictionary<string, object>? data = null)
        {
            return new SkillOutput
            {
                Success = true,
                Data = data ?? new Dictionary<string, object>()
            };
        }

        public static SkillOutput CreateError(string errorMessage)
        {
            return new SkillOutput
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Skill metadata for UI display
    /// </summary>
    public class SkillMetadata
    {
        public string SkillId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public Dictionary<string, string> InputSchema { get; set; } = new();
        public Dictionary<string, string> OutputSchema { get; set; } = new();
        public string Icon { get; set; } = "🧩";
        public string Version { get; set; } = "1.0.0";
    }

    /// <summary>
    /// Core interface for all skills in the DNA Platform
    /// </summary>
    public interface ISkill
    {
        /// <summary>
        /// Unique identifier for this skill
        /// </summary>
        string SkillId { get; }

        /// <summary>
        /// Display name for the skill
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of what the skill does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Category for grouping in the UI
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Icon emoji for the skill
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// Execute the skill with optional inputs
        /// </summary>
        Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null);

        /// <summary>
        /// Get the input schema for this skill
        /// </summary>
        Dictionary<string, string> GetInputSchema();

        /// <summary>
        /// Get the output schema for this skill
        /// </summary>
        Dictionary<string, string> GetOutputSchema();

        /// <summary>
        /// Get full metadata for this skill
        /// </summary>
        SkillMetadata GetMetadata();
    }
}
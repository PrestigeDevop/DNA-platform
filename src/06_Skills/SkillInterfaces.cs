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
    /// Machine-readable description of a single skill input parameter.
    /// The workflow designer / skill "Details" panel uses this to render the
    /// correct editor widget (text box, number box, dropdown, checkbox, ...).
    /// </summary>
    public class SkillInputField
    {
        /// <summary>Parameter name as expected in the JSON request body.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Widget type: string | text | int | float | bool | enum | array | json</summary>
        public string Type { get; set; } = "string";

        /// <summary>Human friendly label shown in the UI.</summary>
        public string? Label { get; set; }

        /// <summary>Help text shown under the field.</summary>
        public string? Description { get; set; }

        /// <summary>Whether the parameter must be supplied.</summary>
        public bool Required { get; set; }

        /// <summary>Value pre-filled in the editor.</summary>
        public object? DefaultValue { get; set; }

        /// <summary>Allowed values when <see cref="Type"/> is "enum".</summary>
        public List<string>? Options { get; set; }

        /// <summary>Placeholder text for free-text inputs.</summary>
        public string? Placeholder { get; set; }
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

        /// <summary>
        /// Structured (typed) version of <see cref="InputSchema"/>. When populated the UI
        /// builds a real parameter editor; when empty the UI degrades gracefully to
        /// rendering the free-text entries of <see cref="InputSchema"/>.
        /// </summary>
        public List<SkillInputField> InputFields { get; set; } = new();
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
        /// Structured input field definitions used by the GUI parameter editor.
        /// Default implementation returns an empty list so existing skills keep
        /// working - the UI then falls back to the free-text <see cref="GetInputSchema"/>.
        /// </summary>
        List<SkillInputField> GetInputFields() => new();

        /// <summary>
        /// Get full metadata for this skill
        /// </summary>
        SkillMetadata GetMetadata();
    }
}
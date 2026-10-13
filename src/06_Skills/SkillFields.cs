using System.Collections.Generic;

namespace DNAPlatform.Skills
{
    /// <summary>
    /// Small factory helpers for building <see cref="SkillInputField"/> lists.
    /// Keeps skill definitions short and consistent so the GUI parameter editor
    /// renders the same widget for the same logical type across all skills.
    /// </summary>
    public static class SkillFields
    {
        /// <summary>Single-line text input.</summary>
        public static SkillInputField Text(
            string name,
            string? label = null,
            string? description = null,
            bool required = false,
            string? defaultValue = null,
            string? placeholder = null)
            => new()
            {
                Name = name,
                Type = "string",
                Label = label ?? name,
                Description = description,
                Required = required,
                DefaultValue = defaultValue,
                Placeholder = placeholder
            };

        /// <summary>Multi-line textarea input.</summary>
        public static SkillInputField Multiline(
            string name,
            string? label = null,
            string? description = null,
            bool required = false,
            string? defaultValue = null,
            string? placeholder = null)
            => new()
            {
                Name = name,
                Type = "text",
                Label = label ?? name,
                Description = description,
                Required = required,
                DefaultValue = defaultValue,
                Placeholder = placeholder
            };

        /// <summary>Numeric input (integer).</summary>
        public static SkillInputField Int(
            string name,
            string? label = null,
            string? description = null,
            bool required = false,
            int? defaultValue = null)
            => new()
            {
                Name = name,
                Type = "int",
                Label = label ?? name,
                Description = description,
                Required = required,
                DefaultValue = defaultValue
            };

        /// <summary>Numeric input (floating point).</summary>
        public static SkillInputField Float(
            string name,
            string? label = null,
            string? description = null,
            bool required = false,
            double? defaultValue = null)
            => new()
            {
                Name = name,
                Type = "float",
                Label = label ?? name,
                Description = description,
                Required = required,
                DefaultValue = defaultValue
            };

        /// <summary>Checkbox input.</summary>
        public static SkillInputField Bool(
            string name,
            string? label = null,
            string? description = null,
            bool defaultValue = false)
            => new()
            {
                Name = name,
                Type = "bool",
                Label = label ?? name,
                Description = description,
                Required = false,
                DefaultValue = defaultValue
            };

        /// <summary>Dropdown / select input.</summary>
        public static SkillInputField Enum(
            string name,
            IEnumerable<string> options,
            string? label = null,
            string? description = null,
            bool required = false,
            string? defaultValue = null)
            => new()
            {
                Name = name,
                Type = "enum",
                Label = label ?? name,
                Description = description,
                Required = required,
                DefaultValue = defaultValue,
                Options = new List<string>(options)
            };

        /// <summary>Comma / newline separated list input mapped to a JSON array.</summary>
        public static SkillInputField Array(
            string name,
            string? label = null,
            string? description = null,
            bool required = false,
            string? placeholder = null)
            => new()
            {
                Name = name,
                Type = "array",
                Label = label ?? name,
                Description = description,
                Required = required,
                Placeholder = placeholder
            };

        /// <summary>Raw JSON object input.</summary>
        public static SkillInputField Json(
            string name,
            string? label = null,
            string? description = null,
            bool required = false,
            string? placeholder = null)
            => new()
            {
                Name = name,
                Type = "json",
                Label = label ?? name,
                Description = description,
                Required = required,
                Placeholder = placeholder
            };
    }
}
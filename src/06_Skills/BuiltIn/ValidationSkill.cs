using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DNAPlatform.Skills;

namespace DNAPlatform.Skills.BuiltIn
{
    /// <summary>
    /// Validation Skill - Validates data against rules and schemas
    /// </summary>
    public class ValidationSkill : ISkill
    {
        public string SkillId => "validation";
        public string Name => "Validation";
        public string Description => "Validate data against rules: required, type, range, regex, FASTA format";
        public string Category => "Data Processing";
        public string Icon => "✅";

        public static readonly string[] ValidationTypes = new[]
        {
            "required", "type", "range", "regex", "fasta", "fastq", "email", "url"
        };

        public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        {
            try
            {
                if (inputs == null)
                    return SkillOutput.CreateError("No inputs provided");

                if (!inputs.TryGetValue("data", out var dataObj) || dataObj == null)
                    return SkillOutput.CreateError("Missing required parameter: 'data'");

                string validationType = "required";
                if (inputs.TryGetValue("validationType", out var typeObj) && typeObj != null)
                    validationType = typeObj.ToString()?.ToLower() ?? "required";

                if (!ValidationTypes.Contains(validationType))
                    return SkillOutput.CreateError($"Invalid validation type: '{validationType}'");

                string data = dataObj.ToString() ?? string.Empty;
                var result = await ValidateData(data, validationType, inputs);

                return SkillOutput.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                return SkillOutput.CreateError($"Validation error: {ex.Message}");
            }
        }

        private Task<Dictionary<string, object>> ValidateData(string data, string validationType, Dictionary<string, object> inputs)
        {
            var result = new Dictionary<string, object>
            {
                { "validation_type", validationType },
                { "input_length", data.Length },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            switch (validationType)
            {
                case "required":
                    result["is_valid"] = !string.IsNullOrWhiteSpace(data);
                    result["message"] = (bool)result["is_valid"] ? "Data is present" : "Data is required but missing";
                    break;

                case "type":
                    string expectedType = inputs.TryGetValue("expectedType", out var et) ? et?.ToString() ?? "string" : "string";
                    result["is_valid"] = CheckType(data, expectedType);
                    result["expected_type"] = expectedType;
                    break;

                case "range":
                    if (double.TryParse(data, out double numValue))
                    {
                        double min = inputs.TryGetValue("min", out var minObj) ? Convert.ToDouble(minObj) : double.MinValue;
                        double max = inputs.TryGetValue("max", out var maxObj) ? Convert.ToDouble(maxObj) : double.MaxValue;
                        result["is_valid"] = numValue >= min && numValue <= max;
                        result["min"] = min;
                        result["max"] = max;
                        result["value"] = numValue;
                    }
                    else
                    {
                        result["is_valid"] = false;
                        result["message"] = "Cannot parse as number for range validation";
                    }
                    break;

                case "regex":
                    string pattern = inputs.TryGetValue("pattern", out var patObj) ? patObj?.ToString() ?? string.Empty : string.Empty;
                    result["is_valid"] = !string.IsNullOrEmpty(pattern) && Regex.IsMatch(data, pattern);
                    result["pattern"] = pattern;
                    break;

                case "fasta":
                    result["is_valid"] = ValidateFasta(data);
                    result["message"] = (bool)result["is_valid"] ? "Valid FASTA format" : "Invalid FASTA format";
                    break;

                case "fastq":
                    result["is_valid"] = ValidateFastq(data);
                    result["message"] = (bool)result["is_valid"] ? "Valid FASTQ format" : "Invalid FASTQ format";
                    break;

                case "email":
                    result["is_valid"] = Regex.IsMatch(data, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                    break;

                case "url":
                    result["is_valid"] = Uri.TryCreate(data, UriKind.Absolute, out _);
                    break;

                default:
                    result["is_valid"] = false;
                    result["message"] = $"Unknown validation type: {validationType}";
                    break;
            }

            return Task.FromResult(result);
        }

        private bool CheckType(string data, string expectedType)
        {
            return expectedType.ToLower() switch
            {
                "int" or "integer" => int.TryParse(data, out _),
                "float" or "double" => double.TryParse(data, out _),
                "bool" or "boolean" => bool.TryParse(data, out _),
                "date" or "datetime" => DateTime.TryParse(data, out _),
                "url" => Uri.TryCreate(data, UriKind.Absolute, out _),
                _ => true
            };
        }

        private bool ValidateFasta(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return false;
            var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return false;
            if (!lines[0].StartsWith(">")) return false;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith(">")) continue;
                if (!Regex.IsMatch(line, "^[ACGTURYKMSWBDHVNXacgturykmswbdhvnx\\-]*$"))
                    return false;
            }
            return true;
        }

        private bool ValidateFastq(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return false;
            var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length % 4 != 0) return false;
            if (!lines[0].StartsWith("@")) return false;
            if (lines[2] != "+") return false;
            return true;
        }

        public Dictionary<string, string> GetInputSchema()
        {
            return new Dictionary<string, string>
            {
                { "data", "string - Data to validate (required)" },
                { "validationType", $"string - Validation type: {string.Join(", ", ValidationTypes)} (default: required)" },
                { "expectedType", "string - For type validation: int, float, bool, date, url" },
                { "min", "number - For range validation: minimum value" },
                { "max", "number - For range validation: maximum value" },
                { "pattern", "string - For regex validation: regex pattern" }
            };
        }

        public Dictionary<string, string> GetOutputSchema()
        {
            return new Dictionary<string, string>
            {
                { "is_valid", "bool - Whether validation passed" },
                { "validation_type", "string - Type of validation performed" },
                { "input_length", "int - Length of input data" },
                { "message", "string - Validation message" },
                { "timestamp", "string - Execution timestamp" }
            };
        }

        public SkillMetadata GetMetadata()
        {
            return new SkillMetadata
            {
                SkillId = SkillId,
                Name = Name,
                Description = Description,
                Category = Category,
                Icon = Icon,
                InputSchema = GetInputSchema(),
                OutputSchema = GetOutputSchema(),
                Version = "1.0.0"
            };
        }
    }
}
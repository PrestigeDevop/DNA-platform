using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DNAPlatform.Skills;

namespace DNAPlatform.Skills.BuiltIn
{
    /// <summary>
    /// Data Transform Skill - Normalizes, standardizes, and transforms data
    /// </summary>
    public class DataTransformSkill : ISkill
    {
        public string SkillId => "data-transform";
        public string Name => "Data Transform";
        public string Description => "Transform data with operations like normalize, standardize, filter, and map";
        public string Category => "Data Processing";
        public string Icon => "🔄";

        public static readonly string[] TransformOperations = new[]
        {
            "normalize",
            "standardize",
            "filter",
            "map",
            "sort",
            "unique",
            "reverse"
        };

        public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        {
            try
            {
                if (inputs == null)
                    return SkillOutput.CreateError("No inputs provided");

                if (!inputs.TryGetValue("data", out var dataObj) || dataObj == null)
                    return SkillOutput.CreateError("Missing required parameter: 'data'");

                string operation = "normalize";
                if (inputs.TryGetValue("operation", out var opObj) && opObj != null)
                    operation = opObj.ToString()?.ToLower() ?? "normalize";

                if (!TransformOperations.Contains(operation))
                    return SkillOutput.CreateError($"Invalid operation: '{operation}'. Valid: {string.Join(", ", TransformOperations)}");

                var result = await TransformData(dataObj, operation);

                return SkillOutput.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                return SkillOutput.CreateError($"Transform error: {ex.Message}");
            }
        }

        private Task<Dictionary<string, object>> TransformData(object data, string operation)
        {
            var result = new Dictionary<string, object>
            {
                { "operation", operation },
                { "input_type", data.GetType().Name },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            // Handle different data types
            if (data is IEnumerable<string> stringList)
            {
                var list = stringList.ToList();
                result["output"] = operation switch
                {
                    "normalize" => list.Select(s => s.Trim().ToLower()).ToList(),
                    "standardize" => list.Select(s => s.ToUpper().Trim()).ToList(),
                    "unique" => list.Distinct().ToList(),
                    "sort" => list.OrderBy(s => s).ToList(),
                    "reverse" => list.AsEnumerable().Reverse().ToList(),
                    _ => list
                };
                result["count"] = ((List<string>)result["output"]).Count;
            }
            else if (data is IEnumerable<double> doubleList)
            {
                var list = doubleList.ToList();
                result["output"] = operation switch
                {
                    "normalize" => NormalizeList(list),
                    "standardize" => StandardizeList(list),
                    "sort" => list.OrderBy(d => d).ToList(),
                    _ => list
                };
                result["count"] = ((List<double>)result["output"]).Count;
            }
            else
            {
                result["output"] = data;
                result["message"] = "Data passed through unchanged (unsupported type for operation)";
            }

            return Task.FromResult(result);
        }

        private List<double> NormalizeList(List<double> values)
        {
            if (!values.Any()) return values;
            var min = values.Min();
            var max = values.Max();
            var range = max - min;
            if (range == 0) return values.Select(_ => 0.5).ToList();
            return values.Select(v => (v - min) / range).ToList();
        }

        private List<double> StandardizeList(List<double> values)
        {
            if (!values.Any()) return values;
            var mean = values.Average();
            var stdDev = Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));
            if (stdDev == 0) return values.Select(_ => 0.0).ToList();
            return values.Select(v => (v - mean) / stdDev).ToList();
        }

        public Dictionary<string, string> GetInputSchema()
        {
            return new Dictionary<string, string>
            {
                { "data", "array - Input data to transform (required)" },
                { "operation", $"string - Transform operation: {string.Join(", ", TransformOperations)} (default: normalize)" }
            };
        }

        public Dictionary<string, string> GetOutputSchema()
        {
            return new Dictionary<string, string>
            {
                { "operation", "string - Operation that was applied" },
                { "output", "array - Transformed data" },
                { "count", "int - Number of items in output" },
                { "input_type", "string - Type of input data" },
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
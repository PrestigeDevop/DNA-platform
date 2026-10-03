using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DNAPlatform.Skills;

namespace DNAPlatform.Skills.BuiltIn
{
    /// <summary>
    /// Merge Results Skill - Combines multiple analysis results into unified output
    /// </summary>
    public class MergeResultsSkill : ISkill
    {
        public string SkillId => "merge-results";
        public string Name => "Merge Results";
        public string Description => "Merge multiple analysis results with strategies: concat, union, intersection, average";
        public string Category => "Data Processing";
        public string Icon => "🔀";

        public static readonly string[] MergeStrategies = new[]
        {
            "concat", "union", "intersection", "average", "vote"
        };

        public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        {
            try
            {
                if (inputs == null)
                    return SkillOutput.CreateError("No inputs provided");

                if (!inputs.TryGetValue("results", out var resultsObj) || resultsObj == null)
                    return SkillOutput.CreateError("Missing required parameter: 'results'");

                string strategy = "concat";
                if (inputs.TryGetValue("strategy", out var stratObj) && stratObj != null)
                    strategy = stratObj.ToString()?.ToLower() ?? "concat";

                if (!MergeStrategies.Contains(strategy))
                    return SkillOutput.CreateError($"Invalid strategy: '{strategy}'");

                var results = resultsObj as IEnumerable<object>;
                if (results == null)
                    return SkillOutput.CreateError("Results must be an array of objects");

                var result = await MergeData(results.ToList(), strategy);
                return SkillOutput.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                return SkillOutput.CreateError($"Merge error: {ex.Message}");
            }
        }

        private Task<Dictionary<string, object>> MergeData(List<object> results, string strategy)
        {
            var output = new Dictionary<string, object>
            {
                { "strategy", strategy },
                { "input_count", results.Count },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            switch (strategy)
            {
                case "concat":
                    output["output"] = results;
                    output["total_items"] = results.Count;
                    break;

                case "union":
                    var uniqueList = new HashSet<string>();
                    foreach (var item in results)
                    {
                        if (item is IEnumerable<object> subList)
                        {
                            foreach (var sub in subList)
                                uniqueList.Add(sub?.ToString() ?? string.Empty);
                        }
                        else
                            uniqueList.Add(item?.ToString() ?? string.Empty);
                    }
                    output["output"] = uniqueList.ToList();
                    output["unique_count"] = uniqueList.Count;
                    break;

                case "intersection":
                    var firstItem = results.FirstOrDefault() as IEnumerable<object>;
                    if (firstItem == null)
                    {
                        output["output"] = new List<object>();
                        output["common_count"] = 0;
                        break;
                    }
                    var commonItems = new HashSet<object>(firstItem);
                    foreach (var item in results.Skip(1))
                    {
                        var currentSet = new HashSet<object>(item as IEnumerable<object> ?? new List<object>());
                        commonItems.IntersectWith(currentSet);
                    }
                    output["output"] = commonItems.ToList();
                    output["common_count"] = commonItems.Count;
                    break;

                case "average":
                    var numbers = results.Select(r => Convert.ToDouble(r)).ToList();
                    output["output"] = numbers.Average();
                    output["min"] = numbers.Min();
                    output["max"] = numbers.Max();
                    output["count"] = numbers.Count;
                    break;

                case "vote":
                    var votes = results.Select(r => r?.ToString() ?? string.Empty).ToList();
                    var grouped = votes.GroupBy(v => v).OrderByDescending(g => g.Count()).ToList();
                    if (grouped.Any())
                    {
                        output["output"] = grouped.First().Key;
                        output["vote_counts"] = grouped.ToDictionary(g => g.Key, g => g.Count());
                        output["winner_votes"] = grouped.First().Count();
                    }
                    break;
            }

            return Task.FromResult(output);
        }

        public Dictionary<string, string> GetInputSchema()
        {
            return new Dictionary<string, string>
            {
                { "results", "array - Array of results to merge (required)" },
                { "strategy", $"string - Merge strategy: {string.Join(", ", MergeStrategies)} (default: concat)" }
            };
        }

        public Dictionary<string, string> GetOutputSchema()
        {
            return new Dictionary<string, string>
            {
                { "strategy", "string - Strategy used" },
                { "output", "any - Merged result" },
                { "input_count", "int - Number of input results" },
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

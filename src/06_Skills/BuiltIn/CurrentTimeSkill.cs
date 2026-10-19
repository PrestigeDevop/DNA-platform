using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace DNAPlatform.Skills.BuiltIn
{
    /// <summary>
    /// C# Skill that prints and returns high-precision current system time,
    /// UTC timestamp, and timezone information. Can be assigned to Agents
    /// and executed within workflow processing nodes or via MCP.
    /// </summary>
    public class CurrentTimeSkill : ISkill
    {
        public string SkillId => "current-time";
        public string Name => "Current System Time (C#)";
        public string Description => "Prints high-precision current system time, UTC timestamp, and timezone info in C#/.NET.";
        public string Category => "System & Diagnostics";
        public string Icon => "⏱️";

        public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        {
            await Task.CompletedTask;

            try
            {
                inputs ??= new Dictionary<string, object>();

                string format = inputs.TryGetValue("format", out var fVal) && fVal != null
                    ? fVal.ToString()!
                    : "yyyy-MM-dd HH:mm:ss.fff";

                string prefix = inputs.TryGetValue("prefix", out var pVal) && pVal != null
                    ? pVal.ToString()!
                    : "System Time: ";

                string tzReq = inputs.TryGetValue("timezone", out var tzVal) && tzVal != null
                    ? tzVal.ToString()!.Trim().ToUpperInvariant()
                    : "UTC";

                DateTime nowUtc = DateTime.UtcNow;
                DateTime targetTime = tzReq == "LOCAL" ? DateTime.Now : nowUtc;
                string tzName = tzReq == "LOCAL" ? TimeZoneInfo.Local.DisplayName : "UTC";

                string formattedTime;
                if (format.Equals("iso8601", StringComparison.OrdinalIgnoreCase))
                {
                    formattedTime = nowUtc.ToString("o", CultureInfo.InvariantCulture);
                }
                else if (format.Equals("time_only", StringComparison.OrdinalIgnoreCase))
                {
                    formattedTime = targetTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                }
                else
                {
                    formattedTime = targetTime.ToString(format, CultureInfo.InvariantCulture);
                }

                long unixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string fullPrint = $"{prefix}{formattedTime} ({tzName})";

                var data = new Dictionary<string, object>
                {
                    ["currentTime"] = formattedTime,
                    ["formattedPrint"] = fullPrint,
                    ["utcIso"] = nowUtc.ToString("o", CultureInfo.InvariantCulture),
                    ["unixTimestampMs"] = unixMs,
                    ["timezone"] = tzName,
                    ["timeOfDay"] = targetTime.Hour < 12 ? "Morning" : targetTime.Hour < 17 ? "Afternoon" : "Evening",
                    ["language"] = "C# / .NET 8"
                };

                return SkillOutput.CreateSuccess(data);
            }
            catch (Exception ex)
            {
                return SkillOutput.CreateError($"CurrentTimeSkill error: {ex.Message}");
            }
        }

        public Dictionary<string, string> GetInputSchema()
        {
            return new Dictionary<string, string>
            {
                { "format", "string - Date/time format string (e.g. 'yyyy-MM-dd HH:mm:ss.fff', 'iso8601', 'time_only')" },
                { "prefix", "string - Custom text prefix prepended to console output (default: 'System Time: ')" },
                { "timezone", "string - 'UTC' or 'Local' (default: 'UTC')" }
            };
        }

        public Dictionary<string, string> GetOutputSchema()
        {
            return new Dictionary<string, string>
            {
                { "currentTime", "string - Formatted current timestamp" },
                { "formattedPrint", "string - Ready-to-print string with prefix and timezone" },
                { "utcIso", "string - ISO-8601 UTC timestamp" },
                { "unixTimestampMs", "number - Epoch timestamp in milliseconds" },
                { "timezone", "string - Resolved timezone name" },
                { "timeOfDay", "string - Morning, Afternoon, or Evening" },
                { "language", "string - Execution runtime identifier" }
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
                Version = "1.0.0",
                InputFields = new List<InputFieldMetadata>
                {
                    new InputFieldMetadata
                    {
                        Name = "format",
                        Type = "string",
                        Label = "Timestamp Format",
                        Description = "Date/time format string (e.g. 'yyyy-MM-dd HH:mm:ss', 'iso8601', 'time_only')",
                        DefaultValue = "yyyy-MM-dd HH:mm:ss.fff",
                        Required = false
                    },
                    new InputFieldMetadata
                    {
                        Name = "prefix",
                        Type = "string",
                        Label = "Output Prefix",
                        Description = "Custom text prefix prepended to console output",
                        DefaultValue = "System Time: ",
                        Required = false
                    },
                    new InputFieldMetadata
                    {
                        Name = "timezone",
                        Type = "string",
                        Label = "Target Timezone",
                        Description = "'UTC' or 'Local'",
                        DefaultValue = "UTC",
                        Required = false
                    }
                },
                OutputFields = new List<OutputFieldMetadata>
                {
                    new OutputFieldMetadata { Name = "currentTime", Type = "string", Description = "Formatted current time" },
                    new OutputFieldMetadata { Name = "formattedPrint", Type = "string", Description = "Ready-to-print string with prefix" },
                    new OutputFieldMetadata { Name = "utcIso", Type = "string", Description = "ISO-8601 UTC timestamp" },
                    new OutputFieldMetadata { Name = "unixTimestampMs", Type = "number", Description = "Epoch timestamp in milliseconds" }
                }
            };
        }
    }
}

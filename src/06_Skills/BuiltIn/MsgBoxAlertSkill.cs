using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNAPlatform.Skills;

namespace DNAPlatform.Skills.BuiltIn
{
    public class MsgBoxAlertSkill : ISkill
    {
        public string SkillId => "msgbox-alert";
        public string Name => "Message Box Alert";
        public string Description => "Displays a configurable alert message";
        public string Category => "UI Components";
        public string Icon => "💬";

        public static readonly string[] MsgBoxTypes = new[] { "info", "warning", "error", "question", "success" };

        public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        {
            try
            {
                if (inputs == null)
                    return SkillOutput.CreateError("No inputs provided");

                if (!inputs.TryGetValue("message", out var messageObj) || messageObj == null)
                    return SkillOutput.CreateError("Missing required parameter: 'message'");

                string message = messageObj.ToString() ?? "No message";

                string msgType = "info";
                if (inputs.TryGetValue("msgType", out var typeObj) && typeObj != null)
                    msgType = typeObj.ToString()?.ToLower() ?? "info";

                string title = "Alert";
                if (inputs.TryGetValue("title", out var titleObj) && titleObj != null)
                    title = titleObj.ToString() ?? "Alert";

                int durationMs = 0;
                if (inputs.TryGetValue("duration", out var durationObj) && durationObj != null)
                    int.TryParse(durationObj.ToString(), out durationMs);

                var alertData = new Dictionary<string, object>
                {
                    { "alert_id", Guid.NewGuid().ToString() },
                    { "message", message },
                    { "type", msgType },
                    { "title", title },
                    { "duration_ms", durationMs },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                return SkillOutput.CreateSuccess(alertData);
            }
            catch (Exception ex)
            {
                return SkillOutput.CreateError($"Error: {ex.Message}");
            }
        }

        public Dictionary<string, string> GetInputSchema()
        {
            return new Dictionary<string, string>
            {
                { "message", "string - The message content (required)" },
                { "msgType", "string - Alert type: info, warning, error, question, success" },
                { "title", "string - Alert title" },
                { "duration", "int - Auto-dismiss duration in ms" }
            };
        }

        public Dictionary<string, string> GetOutputSchema()
        {
            return new Dictionary<string, string>
            {
                { "alert_id", "string - Unique identifier" },
                { "message", "string - The message" },
                { "type", "string - Alert type" },
                { "title", "string - Alert title" },
                { "timestamp", "string - ISO 8601 timestamp" }
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

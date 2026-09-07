using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNAPlatform.Skills;

namespace DNAPlatform.Skills.BuiltIn
{
    /// <summary>
    /// Message Box Alert Skill - Demonstrates GUI-triggered backend execution
    /// 
    /// When called from the frontend workflow designer, this skill:
    /// 1. Receives message content and type from GUI inputs
    /// 2. Processes the alert logic on the backend
    /// 3. Returns structured result for UI display
    /// 
    /// This is the simplest skill for Phase 3 demonstration.
    /// </summary>
    public class MsgBoxAlertSkill : ISkill
    {
        public string SkillId => "msgbox-alert";
        public string Name => "Message Box Alert";
        public string Description => "Displays a configurable alert message with selectable type (Info, Warning, Error, Question)";
        public string Category => "UI Components";
        public string Icon => "💬";

        // MsgBox type options for dropdown
        public static readonly string[] MsgBoxTypes = new[]
        {
            "info",
            "warning",
            "error",
            "question",
            "success"
        };

        public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (inputs == null)
                {
                    return SkillOutput.CreateError("No inputs provided - message is required");
                }

                // Extract message from inputs
                if (!inputs.TryGetValue("message", out var messageObj) || messageObj == null)
                {
                    return SkillOutput.CreateError("Missing required parameter: 'message'");
                }

                string message = messageObj.ToString() ?? "No message";

                // Extract message type from inputs (default to "info")
                string msgType = "info";
                if (inputs.TryGetValue("msgType", out var typeObj) && typeObj != null)
                {
                    msgType = typeObj.ToString()?.ToLower() ?? "info";
                }

                // Validate message type
                if (!IsValidMsgType(msgType))
                {
                    return SkillOutput.CreateError($"Invalid message type: '{msgType}'. Valid types: {string.Join(", ", MsgBoxTypes)}");
                }

                // Extract optional title
                string title = "Alert";
                if (inputs.TryGetValue("title", out var titleObj) && titleObj != null)
                {
                    title = titleObj.ToString() ?? "Alert";
                }

                // Extract optional duration (for auto-dismiss)
                int durationMs = 0;
                if (inputs.TryGetValue("duration", out var durationObj) && durationObj != null)
                {
                    int.TryParse(durationObj.ToString(), out durationMs);
                }

                // Backend processing logic
                var result = await ProcessAlert(message, msgType, title, durationMs);

                var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                result.Data!["duration_ms"] = duration;
                result.Data["executed_at"] = DateTime.UtcNow.ToString("o");

                return result;
            }
            catch (Exception ex)
            {
                return SkillOutput.CreateError($"Error executing MsgBoxAlertSkill: {ex.Message}");
            }
        
        private Task<SkillOutput> ProcessAlert(string message, string msgType, string title, int durationMs)
        {
            var alertData = new Dictionary<string, object>
            {
                { "alert_id", Guid.NewGuid().ToString() },
                { "message", message },
                { "type", msgType },
                { "title", title },
                { "duration_ms", durationMs },
                { "icon", GetIconForType(msgType) },
                { "color", GetColorForType(msgType) },
                { "sound", GetSoundForType(msgType) },
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "acknowledged", false }
            };

            return Task.FromResult(SkillOutput.CreateSuccess(alertData));
        }

        private bool IsValidMsgType(string type)
        {
            return Array.Exists(MsgBoxTypes, t => t.Equals(type, StringComparison.OrdinalIgnoreCase));
        }
        }

        private string GetIconForType(string type)
        {
            return type.ToLower() switch
            {
                "info" => "ℹ️",
                "warning" => "⚠️",
                "error" => "❌",
                "question" => "❓",
                "success" => "✅",
                _ => "💬"
            };
        }

        private string GetColorForType(string type)
        {
            return type.ToLower() switch
            {
                "info" => "#3b82f6",
                "warning" => "#f59e0b",
                "error" => "#ef4444",
                "question" => "#8b5cf6",
                "success" => "#10b981",
                _ => "#6b7280"
            };
        }

        private string GetSoundForType(string type)
        {
            return type.ToLower() switch
            {
                "info" => "notification.mp3",
                "warning" => "warning.mp3",
                "error" => "error.mp3",
                "question" => "question.mp3",
                "success" => "success.mp3",
                _ => "default.mp3"
            };
        }

        public Dictionary<string, string> GetInputSchema()
        {
            return new Dictionary<string, string>
            {
                { "message", "string - The message content to display (required)" },
                { "msgType", $"string - Alert type dropdown: {string.Join(", ", MsgBoxTypes)} (default: info)" },
                { "title", "string - Alert title (default: 'Alert')" },
                { "duration", "int - Auto-dismiss duration in ms, 0 = no auto-dismiss (default: 0)" }
            };
        }

        public Dictionary<string, string> GetOutputSchema()
        {
            return new Dictionary<string, string>
            {
                { "alert_id", "string - Unique identifier for this alert" },
                { "message", "string - The message that was processed" },
                { "type", "string - The alert type used" },
                { "title", "string - The alert title" },
                { "duration_ms", "int - Auto-dismiss duration" },
                { "icon", "string - Icon emoji for the alert type" },
                { "color", "string - Hex color code for the alert type" },
                { "sound", "string - Sound file associated with alert type" },
                { "timestamp", "string - ISO 8601 timestamp of execution" },
                { "acknowledged", "bool - Whether alert was acknowledged" },
                { "executed_at", "string - Execution timestamp" }
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
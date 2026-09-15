using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DNAPlatform.Skills;

namespace DNAPlatform.Skills.BuiltIn
{
    /// <summary>
    /// Simplest possible skill - builds a "message box" payload that the SvelteKit
    /// frontend renders as a modal dialog. Demonstrates the full GUI tool-call loop:
    /// GUI inputs -> POST /api/skills/msgbox-alert/execute -> backend logic -> GUI result.
    /// </summary>
    public class MsgBoxAlertSkill : ISkill
    {
        public string SkillId => "msgbox-alert";
        public string Name => "Message Box Alert";
        public string Description => "Displays a configurable alert message (msgbox type, title and message)";
        public string Category => "UI Components";
        public string Icon => "💬";

        /// <summary>Supported message box types - drives the GUI dropdown.</summary>
        public static readonly string[] MsgBoxTypes = new[] { "info", "warning", "error", "question", "success" };

        private static readonly Dictionary<string, string> TypeIcons = new(StringComparer.OrdinalIgnoreCase)
        {
            { "info", "ℹ️" },
            { "warning", "⚠️" },
            { "error", "❌" },
            { "question", "❓" },
            { "success", "✅" }
        };

        private static readonly Dictionary<string, string> TypeColors = new(StringComparer.OrdinalIgnoreCase)
        {
            { "info", "#3b82f6" },
            { "warning", "#f59e0b" },
            { "error", "#ef4444" },
            { "question", "#8b5cf6" },
            { "success", "#10b981" }
        };

        public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        {
            await Task.CompletedTask;

            try
            {
                inputs ??= new Dictionary<string, object>();

                // -------- message (required) --------
                string? message = GetString(inputs, "message");
                if (string.IsNullOrWhiteSpace(message))
                    return SkillOutput.CreateError("Missing required parameter: 'message' (the alert text)");

                // -------- msgType (validated against the dropdown list) --------
                string msgType = (GetString(inputs, "msgType") ?? "info").Trim().ToLowerInvariant();
                if (!MsgBoxTypes.Contains(msgType))
                    return SkillOutput.CreateError(
                        $"Invalid 'msgType' value '{msgType}'. Allowed values: {string.Join(", ", MsgBoxTypes)}");

                // -------- title (defaults to the msgbox type) --------
                string title = GetString(inputs, "title")
                               ?? char.ToUpperInvariant(msgType[0]) + msgType.Substring(1);

                // -------- duration --------
                int durationMs = GetInt(inputs, "duration");
                if (durationMs < 0) durationMs = 0;

                // NOTE: keys are camelCase because ASP.NET Core serialises dictionary keys
                // using the camelCase naming policy - keeping both sides identical avoids
                // "alert_id" vs "alertId" mismatches in the frontend.
                var alertData = new Dictionary<string, object>
                {
                    { "alertId", Guid.NewGuid().ToString() },
                    { "message", message },
                    { "type", msgType },
                    { "typeLabel", char.ToUpperInvariant(msgType[0]) + msgType.Substring(1) },
                    { "title", title },
                    { "icon", TypeIcons[msgType] },
                    { "color", TypeColors[msgType] },
                    { "durationMs", durationMs },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                return SkillOutput.CreateSuccess(alertData);
            }
            catch (Exception ex)
            {
                return SkillOutput.CreateError($"Error: {ex.Message}");
            }
        }

        private static string? GetString(Dictionary<string, object> inputs, string key)
        {
            if (!inputs.TryGetValue(key, out var value) || value == null) return null;
            var text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static int GetInt(Dictionary<string, object> inputs, string key)
        {
            if (!inputs.TryGetValue(key, out var value) || value == null) return 0;

            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;

            return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
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

        /// <summary>
        /// Typed parameter list driving the GUI "Details" editor + the Quick Demo form.
        /// </summary>
        public List<SkillInputField> GetInputFields()
        {
            return new List<SkillInputField>
            {
                SkillFields.Multiline(
                    "message",
                    label: "Message",
                    description: "Text shown inside the message box.",
                    required: true,
                    defaultValue: "Hello from DNA Platform!",
                    placeholder: "Type the alert text here…"),

                SkillFields.Enum(
                    "msgType",
                    MsgBoxTypes,
                    label: "Message Box Type",
                    description: "Controls the icon / colour of the dialog.",
                    required: false,
                    defaultValue: "info"),

                SkillFields.Text(
                    "title",
                    label: "Title",
                    description: "Dialog caption. Defaults to the capitalised type.",
                    placeholder: "Alert"),

                SkillFields.Int(
                    "duration",
                    label: "Auto-dismiss (ms)",
                    description: "0 keeps the dialog open until dismissed manually.",
                    defaultValue: 0)
            };
        }

        public Dictionary<string, string> GetOutputSchema()
        {
            return new Dictionary<string, string>
            {
                { "alertId", "string - Unique identifier" },
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

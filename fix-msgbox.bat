@echo off
echo Fixing MsgBoxAlertSkill.cs...

(
echo using System;
echo using System.Collections.Generic;
echo using System.Threading.Tasks;
echo using DNAPlatform.Skills;
echo.
echo namespace DNAPlatform.Skills.BuiltIn
echo {
echo     public class MsgBoxAlertSkill : ISkill
echo     {
echo         public string SkillId =^> "msgbox-alert";
echo         public string Name =^> "Message Box Alert";
echo         public string Description =^> "Displays a configurable alert message with selectable type";
echo         public string Category =^> "UI Components";
echo         public string Icon =^> "💬";
echo.
echo         public static readonly string[] MsgBoxTypes = new[] { "info", "warning", "error", "question", "success" };
echo.
echo         public async Task^<SkillOutput^> Execute(Dictionary^<string, object^>? inputs = null)
echo         {
echo             var startTime = DateTime.UtcNow;
echo.
echo             try
echo             {
echo                 if (inputs == null)
echo                     return SkillOutput.CreateError("No inputs provided");
echo.
echo                 if (!inputs.TryGetValue("message", out var messageObj) || messageObj == null)
echo                     return SkillOutput.CreateError("Missing required parameter: 'message'");
echo.
echo                 string message = messageObj.ToString() ?? "No message";
echo.
echo                 string msgType = "info";
echo                 if (inputs.TryGetValue("msgType", out var typeObj) && typeObj != null)
echo                     msgType = typeObj.ToString()?.ToLower() ?? "info";
echo.
echo                 if (!IsValidMsgType(msgType))
echo                     return SkillOutput.CreateError($"Invalid message type: '{msgType}'");
echo.
echo                 string title = "Alert";
echo                 if (inputs.TryGetValue("title", out var titleObj) && titleObj != null)
echo                     title = titleObj.ToString() ?? "Alert";
echo.
echo                 int durationMs = 0;
echo                 if (inputs.TryGetValue("duration", out var durationObj) && durationObj != null)
echo                     int.TryParse(durationObj.ToString(), out durationMs);
echo.
echo                 var result = await ProcessAlert(message, msgType, title, durationMs);
echo.
echo                 var duration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
echo                 result.Data!["duration_ms"] = duration;
echo                 result.Data["executed_at"] = DateTime.UtcNow.ToString("o");
echo.
echo                 return result;
echo             }
echo             catch (Exception ex)
echo             {
echo                 return SkillOutput.CreateError($"Error: {ex.Message}");
echo             }
echo         }
) > src\06_Skills\BuiltIn\MsgBoxAlertSkill.cs

echo Part 1 written.
pause
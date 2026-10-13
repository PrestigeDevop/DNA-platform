using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DNAPlatform.AgentFramework;

/// <summary>
/// Node executor that routes <see cref="NodeType.ProcessingSkill"/> nodes to a
/// registered skill and treats every other node type as a pass-through that
/// forwards what it received and adds its own identity.
///
/// <para>
/// This is what makes a run do real work rather than echo: an upstream node's
/// output is exactly what a downstream node receives as inputs, and a skill node
/// whose skill is missing fails loudly instead of reporting success.
/// </para>
/// </summary>
public class SkillBackedNodeExecutor : INodeExecutor
{
    private readonly ISkillRegistry _skills;
    private readonly ILogger<SkillBackedNodeExecutor> _logger;

    public SkillBackedNodeExecutor(
        ISkillRegistry skills,
        ILogger<SkillBackedNodeExecutor> logger)
    {
        _skills = skills;
        _logger = logger;
    }

    public IEnumerable<NodeType> GetSupportedNodeTypes() => Enum.GetValues<NodeType>();

    public async Task<NodeExecution> ExecuteNode(WorkflowNode node, Dictionary<string, object>? inputs = null)
    {
        var execution = new NodeExecution
        {
            NodeId = node.Id,
            Status = NodeStatus.Running,
            StartedAt = DateTime.UtcNow,
            Inputs = inputs ?? new Dictionary<string, object>()
        };

        try
        {
            switch (node.NodeType)
            {
                case NodeType.Value:
                    await ExecuteValueNode(node, execution);
                    break;

                case NodeType.Log:
                    await ExecuteLogNode(node, execution);
                    break;

                case NodeType.ProcessingSkill:
                    await ExecuteProcessingSkillNode(node, execution);
                    break;

                default:
                    // Pass-through node: forwards what it received and adds its own
                    // identity, so an Input -> Output chain produces something real.
                    ExecutePassThroughNode(node, execution);
                    break;
            }

            execution.Status = NodeStatus.Completed;
            execution.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            execution.Status = NodeStatus.Failed;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Error = new ExecutionError
            {
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                ErrorCode = "NODE_EXECUTION_ERROR"
            };
            _logger.LogError(ex, "Node {NodeId} execution failed", node.Id);
        }

        return execution;
    }

    private Task ExecuteValueNode(WorkflowNode node, NodeExecution execution)
    {
        // Value node: returns the static value from config
        var value = string.Empty;
        if (node.Config != null && node.Config.TryGetValue("value", out var configValue))
        {
            value = configValue?.ToString() ?? string.Empty;
        }

        execution.Outputs = new Dictionary<string, object>
        {
            ["value"] = value,
            ["nodeId"] = node.Id,
            ["nodeName"] = node.Name,
            ["nodeType"] = node.NodeType.ToString(),
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        _logger.LogInformation("Value node {NodeId} produced: {Value}", node.Id, value);
        return Task.CompletedTask;
    }

    private Task ExecuteLogNode(WorkflowNode node, NodeExecution execution)
    {
        // Log node: writes input to the platform log buffer via ILogger
        // The LogStoreLoggerProvider mirrors all ILogger calls to the LogStore
        var message = "Log node executed";
        var level = "Information";
        var category = "Workflow.Log";

        if (execution.Inputs != null && execution.Inputs.Count > 0)
        {
            // Use the first input as the message, or serialize all inputs
            if (execution.Inputs.TryGetValue("message", out var msg))
            {
                message = msg?.ToString() ?? message;
            }
            else if (execution.Inputs.TryGetValue("output", out var outVal))
            {
                message = outVal?.ToString() ?? message;
            }
            else
            {
                message = JsonSerializer.Serialize(execution.Inputs);
            }

            if (execution.Inputs.TryGetValue("level", out var lvl))
            {
                level = lvl?.ToString() ?? level;
            }

            if (execution.Inputs.TryGetValue("category", out var cat))
            {
                category = cat?.ToString() ?? category;
            }
        }
        else if (node.Config != null)
        {
            if (node.Config.TryGetValue("message", out var msg))
            {
                message = msg?.ToString() ?? message;
            }
            if (node.Config.TryGetValue("level", out var lvl))
            {
                level = lvl?.ToString() ?? level;
            }
            if (node.Config.TryGetValue("category", out var cat))
            {
                category = cat?.ToString() ?? category;
            }
        }

        // Write via ILogger - LogStoreLoggerProvider will mirror to LogStore
        var logLevel = level switch
        {
            "Debug" => LogLevel.Debug,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, "[Workflow:{NodeName}] {Message}", node.Name, message);

        execution.Outputs = new Dictionary<string, object>
        {
            ["logged"] = true,
            ["message"] = message,
            ["level"] = level,
            ["category"] = category,
            ["nodeId"] = node.Id,
            ["nodeName"] = node.Name,
            ["nodeType"] = node.NodeType.ToString(),
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        _logger.LogInformation("Log node {NodeId} wrote: {Message}", node.Id, message);
        return Task.CompletedTask;
    }

    private async Task ExecuteProcessingSkillNode(WorkflowNode node, NodeExecution execution)
    {
        var skillId = ResolveSkillId(node);
        if (skillId is not null)
        {
            var skill = await _skills.GetSkill(skillId);
            if (skill is null)
            {
                throw new InvalidOperationException(
                    $"Node '{node.Name}' references skill '{skillId}', which is not registered.");
            }

            // Node config is the node's parameter set; inherited edge values are
            // its data inputs. Skills expect one flat dictionary, so both are
            // merged with the node's own config winning on a key clash.
            var skillInputs = new Dictionary<string, object>(execution.Inputs);
            if (node.Config is not null)
            {
                foreach (var pair in node.Config)
                {
                    skillInputs[pair.Key] = pair.Value;
                }
            }

            // Handle template substitution if config contains a template string
            if (node.Config?.TryGetValue("template", out var templateObj) == true
                && templateObj is string template
                && !string.IsNullOrWhiteSpace(template))
            {
                var resolvedTemplate = ResolveTemplate(template, skillInputs);
                skillInputs["template"] = resolvedTemplate;
                _logger.LogInformation("ProcessingSkill node {NodeId} resolved template: {Template}", node.Id, resolvedTemplate);
            }

            var result = await skill.Execute(skillInputs);
            execution.Outputs = result.Data is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(result.Data);

            if (!result.Success)
            {
                execution.Status = NodeStatus.Failed;
                execution.CompletedAt = DateTime.UtcNow;
                execution.Error = new ExecutionError
                {
                    Message = result.ErrorMessage ?? $"Skill '{skillId}' reported failure.",
                    ErrorCode = "SKILL_FAILED"
                };
                _logger.LogError("Node {NodeId} failed in skill {SkillId}: {Error}",
                    node.Id, skillId, execution.Error.Message);
                return;
            }

            execution.Outputs["skillId"] = skillId;
            _logger.LogInformation("Node {NodeId} ran skill {SkillId}", node.Id, skillId);
        }
        else
        {
            // Pass-through for ProcessingSkill without a skill reference
            ExecutePassThroughNode(node, execution);
        }
    }

    private void ExecutePassThroughNode(WorkflowNode node, NodeExecution execution)
    {
        execution.Outputs = new Dictionary<string, object>(execution.Inputs ?? new Dictionary<string, object>())
        {
            ["nodeId"] = node.Id,
            ["nodeName"] = node.Name,
            ["nodeType"] = node.NodeType.ToString(),
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        // "output" is what an unlabelled connection looks for.
        execution.Outputs["output"] = $"Processed by node {node.Name}";
        _logger.LogInformation("Node {NodeId} ({NodeType}) executed", node.Id, node.NodeType);
    }

    public Task<bool> ValidateNode(WorkflowNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Id))
        {
            return Task.FromResult(false);
        }

        // A ProcessingSkill node must name a skill, otherwise it silently degrades
        // to a pass-through and the workflow reports success without doing the work.
        if (node.NodeType == NodeType.ProcessingSkill && ResolveSkillId(node) is null)
        {
            return Task.FromResult(false);
        }

        // Value node should have a value in config
        if (node.NodeType == NodeType.Value && (node.Config == null || !node.Config.ContainsKey("value")))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Reads the skill id a node asks for. Accepts <c>skillId</c> or <c>skill</c>
    /// in the node config, which are the keys the designer's config JSON editor
    /// produces.
    /// </summary>
    private static string? ResolveSkillId(WorkflowNode node)
    {
        if (node.Config is null)
        {
            return null;
        }

        foreach (var key in new[] { "skillId", "skill" })
        {
            if (node.Config.TryGetValue(key, out var value) && value is not null)
            {
                var text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves a template string by replacing placeholders like {user.firstname}
    /// with values from the inputs dictionary. Supports nested property access
    /// via dot notation.
    /// </summary>
    private static string ResolveTemplate(string template, Dictionary<string, object> inputs)
    {
        var result = template;
        var pattern = @"\{([^}]+)\}";
        var matches = Regex.Matches(template, pattern);

        foreach (Match match in matches)
        {
            var key = match.Groups[1].Value;
            var value = GetValueByPath(inputs, key);
            var replacement = value?.ToString() ?? match.Value; // Keep placeholder if not found
            result = result.Replace(match.Value, replacement);
        }

        return result;
    }

    /// <summary>
    /// Gets a value from a dictionary using dot-notation path (e.g., "user.firstname").
    /// </summary>
    private static object? GetValueByPath(Dictionary<string, object> dict, string path)
    {
        var parts = path.Split('.');
        object? current = null;

        if (!dict.TryGetValue(parts[0], out current))
        {
            return null;
        }

        for (int i = 1; i < parts.Length; i++)
        {
            if (current is Dictionary<string, object> nestedDict)
            {
                if (!nestedDict.TryGetValue(parts[i], out current))
                {
                    return null;
                }
            }
            else if (current is JsonObject jsonObj)
            {
                if (!jsonObj.TryGetPropertyValue(parts[i], out var node))
                {
                    return null;
                }
                current = node?.Deserialize<object>();
            }
            else if (current is JsonElement jsonEl)
            {
                if (jsonEl.ValueKind == JsonValueKind.Object)
                {
                    if (!jsonEl.TryGetProperty(parts[i], out var prop))
                    {
                        return null;
                    }
                    current = prop.ValueKind switch
                    {
                        JsonValueKind.Object => prop.Deserialize<Dictionary<string, object>>(),
                        JsonValueKind.Array => prop.Deserialize<List<object>>(),
                        JsonValueKind.String => prop.GetString(),
                        JsonValueKind.Number => prop.GetRawText(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => prop.GetRawText()
                    };
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current;
    }
}

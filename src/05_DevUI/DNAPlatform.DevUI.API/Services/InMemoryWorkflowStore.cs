using DNAPlatform.AgentFramework;
using System.Collections.Concurrent;

namespace DNAPlatform.DevUI.API.Services;

/// <summary>
/// In-memory implementation of workflow store for development
/// </summary>
public class InMemoryWorkflowStore : IWorkflowStore
{
    private readonly ConcurrentDictionary<string, Workflow> _workflows = new();

    public Task<IEnumerable<Workflow>> GetAllAsync()
    {
        return Task.FromResult(_workflows.Values.AsEnumerable());
    }

    public Task<Workflow?> GetByIdAsync(string id)
    {
        _workflows.TryGetValue(id, out var workflow);
        return Task.FromResult(workflow);
    }

    public Task<Workflow> CreateAsync(Workflow workflow)
    {
        if (string.IsNullOrEmpty(workflow.Id))
        {
            workflow.Id = Guid.NewGuid().ToString();
        }

        workflow.CreatedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        if (_workflows.TryAdd(workflow.Id, workflow))
        {
            return Task.FromResult(workflow);
        }

        throw new InvalidOperationException($"Workflow with ID '{workflow.Id}' already exists");
    }

    public Task<Workflow?> UpdateAsync(string id, Workflow workflow)
    {
        if (_workflows.TryGetValue(id, out var existing))
        {
            workflow.Id = id;
            workflow.CreatedAt = existing.CreatedAt;
            workflow.UpdatedAt = DateTime.UtcNow;
            _workflows[id] = workflow;
            return Task.FromResult<Workflow?>(workflow);
        }

        return Task.FromResult<Workflow?>(null);
    }

    public Task<bool> DeleteAsync(string id)
    {
        return Task.FromResult(_workflows.TryRemove(id, out _));
    }

    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_workflows.ContainsKey(id));
    }
}

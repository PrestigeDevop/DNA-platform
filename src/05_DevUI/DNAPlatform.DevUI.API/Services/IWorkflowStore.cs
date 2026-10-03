using DNAPlatform.AgentFramework;

namespace DNAPlatform.DevUI.API.Services;

/// <summary>
/// Interface for workflow persistence
/// </summary>
public interface IWorkflowStore
{
    /// <summary>
    /// Get all workflows
    /// </summary>
    Task<IEnumerable<Workflow>> GetAllAsync();

    /// <summary>
    /// Get workflow by ID
    /// </summary>
    Task<Workflow?> GetByIdAsync(string id);

    /// <summary>
    /// Create a new workflow
    /// </summary>
    Task<Workflow> CreateAsync(Workflow workflow);

    /// <summary>
    /// Update an existing workflow
    /// </summary>
    Task<Workflow?> UpdateAsync(string id, Workflow workflow);

    /// <summary>
    /// Delete a workflow
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Check if workflow exists
    /// </summary>
    Task<bool> ExistsAsync(string id);
}

namespace NetworkOptimizer.Web.Services;

/// <summary>
/// Interface for managing metric collection agents.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Gets a summary of agent status including active/inactive counts, total metrics, and average latency.
    /// </summary>
    /// <returns>An AgentSummary containing agent statistics.</returns>
    Task<AgentSummary> GetAgentSummaryAsync();

    /// <summary>
    /// Tests SSH connection to a host with the specified credentials.
    /// </summary>
    /// <param name="host">The hostname or IP address to connect to.</param>
    /// <param name="username">The SSH username.</param>
    /// <param name="authMethod">The authentication method (e.g., "password" or "key").</param>
    /// <param name="password">The password for authentication (if using password auth).</param>
    /// <param name="keyPath">The path to the SSH key file (if using key auth).</param>
    /// <returns>True if the connection test succeeds, false otherwise.</returns>
    Task<bool> TestConnectionAsync(string host, string username, string authMethod, string password, string keyPath);

    /// <summary>
    /// Deploys an agent to a remote host using the specified configuration.
    /// </summary>
    /// <param name="config">The deployment configuration including host, credentials, and agent type.</param>
    /// <returns>True if deployment succeeds, false otherwise.</returns>
    Task<bool> DeployAgentAsync(AgentDeploymentConfig config);

    /// <summary>
    /// Generates agent deployment scripts for manual installation.
    /// </summary>
    /// <param name="config">The deployment configuration to generate scripts for.</param>
    /// <returns>The download path for the generated script bundle.</returns>
    Task<string> GenerateAgentScriptsAsync(AgentDeploymentConfig config);

    /// <summary>
    /// Gets all registered agents with their current status and details.
    /// </summary>
    /// <returns>A list of all registered agents.</returns>
    Task<List<AgentDetails>> GetAllAgentsAsync();

    /// <summary>
    /// Removes an agent from the registry and optionally stops it on the remote host.
    /// </summary>
    /// <param name="agentId">The ID of the agent to remove.</param>
    /// <returns>True if removal succeeds, false otherwise.</returns>
    Task<bool> RemoveAgentAsync(int agentId);

    /// <summary>
    /// Restarts an agent on its remote host.
    /// </summary>
    /// <param name="agentId">The ID of the agent to restart.</param>
    /// <returns>True if restart succeeds, false otherwise.</returns>
    Task<bool> RestartAgentAsync(int agentId);
}

namespace NetworkOptimizer.Audit.Models;

/// <summary>
/// Firewall rule action types
/// </summary>
public enum FirewallAction
{
    Unknown,
    Allow,
    Accept,
    Drop,
    Deny,
    Reject,
    Block
}

/// <summary>
/// Extension methods for FirewallAction enum
/// </summary>
public static class FirewallActionExtensions
{
    /// <summary>
    /// Parse a string action to FirewallAction enum
    /// </summary>
    public static FirewallAction Parse(string? action) =>
        action?.ToLowerInvariant() switch
        {
            "allow" => FirewallAction.Allow,
            "accept" => FirewallAction.Accept,
            "drop" => FirewallAction.Drop,
            "deny" => FirewallAction.Deny,
            "reject" => FirewallAction.Reject,
            "block" => FirewallAction.Block,
            _ => FirewallAction.Unknown
        };

    /// <summary>
    /// Check if the action permits traffic (allow or accept)
    /// </summary>
    public static bool IsAllowAction(this FirewallAction action) =>
        action is FirewallAction.Allow or FirewallAction.Accept;

    /// <summary>
    /// Check if the action blocks traffic (drop, deny, reject, block)
    /// </summary>
    public static bool IsBlockAction(this FirewallAction action) =>
        action is FirewallAction.Drop or FirewallAction.Deny or FirewallAction.Reject or FirewallAction.Block;
}

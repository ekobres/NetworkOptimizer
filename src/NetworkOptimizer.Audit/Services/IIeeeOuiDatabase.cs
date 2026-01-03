namespace NetworkOptimizer.Audit.Services;

/// <summary>
/// Downloads and indexes the IEEE OUI database for MAC vendor lookup.
/// The database is downloaded on startup and cached in memory.
/// </summary>
public interface IIeeeOuiDatabase
{
    /// <summary>
    /// Gets the number of OUI entries loaded.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets whether the database has been loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Initialize the database - download from IEEE or load from cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Look up vendor name by MAC address or OUI prefix.
    /// </summary>
    /// <param name="macOrOui">Full MAC address or OUI prefix (e.g., "F8:01:B4" or "F8-01-B4-7B-4E-1B").</param>
    /// <returns>Vendor name or null if not found.</returns>
    string? GetVendor(string macOrOui);

    /// <summary>
    /// Check if a vendor exists in the database.
    /// </summary>
    /// <param name="macOrOui">Full MAC address or OUI prefix.</param>
    /// <returns>True if the vendor is found in the database.</returns>
    bool HasVendor(string macOrOui);
}

namespace NetworkOptimizer.Web.Services;

/// <summary>
/// Interface for reading and writing system-wide settings.
/// </summary>
public interface ISystemSettingsService
{
    /// <summary>
    /// Gets a setting value by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <returns>The setting value, or null if not found.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Gets a setting value as an integer with a default fallback.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value to return if the setting is not found or invalid.</param>
    /// <returns>The setting value as an integer, or the default value.</returns>
    Task<int> GetIntAsync(string key, int defaultValue);

    /// <summary>
    /// Sets a setting value.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to store, or null to clear.</param>
    Task SetAsync(string key, string? value);

    /// <summary>
    /// Sets a setting value as an integer.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The integer value to store.</param>
    Task SetIntAsync(string key, int value);

    /// <summary>
    /// Gets the iperf3 test duration setting.
    /// </summary>
    /// <returns>The test duration in seconds.</returns>
    Task<int> GetIperf3DurationAsync();

    /// <summary>
    /// Sets the iperf3 test duration setting.
    /// </summary>
    /// <param name="value">The test duration in seconds.</param>
    Task SetIperf3DurationAsync(int value);

    /// <summary>
    /// Gets the iperf3 parallel streams setting for gateway devices.
    /// </summary>
    /// <returns>The number of parallel streams.</returns>
    Task<int> GetIperf3GatewayParallelStreamsAsync();

    /// <summary>
    /// Sets the iperf3 parallel streams setting for gateway devices.
    /// </summary>
    /// <param name="value">The number of parallel streams.</param>
    Task SetIperf3GatewayParallelStreamsAsync(int value);

    /// <summary>
    /// Gets the iperf3 parallel streams setting for UniFi devices.
    /// </summary>
    /// <returns>The number of parallel streams.</returns>
    Task<int> GetIperf3UniFiParallelStreamsAsync();

    /// <summary>
    /// Sets the iperf3 parallel streams setting for UniFi devices.
    /// </summary>
    /// <param name="value">The number of parallel streams.</param>
    Task SetIperf3UniFiParallelStreamsAsync(int value);

    /// <summary>
    /// Gets the iperf3 parallel streams setting for other (non-UniFi) devices.
    /// </summary>
    /// <returns>The number of parallel streams.</returns>
    Task<int> GetIperf3OtherParallelStreamsAsync();

    /// <summary>
    /// Sets the iperf3 parallel streams setting for other (non-UniFi) devices.
    /// </summary>
    /// <param name="value">The number of parallel streams.</param>
    Task SetIperf3OtherParallelStreamsAsync(int value);

    /// <summary>
    /// Gets all iperf3 settings as a DTO.
    /// </summary>
    /// <returns>An Iperf3Settings object containing all iperf3-related settings.</returns>
    Task<Iperf3Settings> GetIperf3SettingsAsync();

    /// <summary>
    /// Saves all iperf3 settings from a DTO.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    Task SaveIperf3SettingsAsync(Iperf3Settings settings);

    /// <summary>
    /// Checks if local iperf3 is available on this server by running iperf3 --version.
    /// </summary>
    /// <param name="forceRefresh">If true, bypasses the cache and performs a fresh check.</param>
    /// <returns>A LocalIperf3Status containing availability information.</returns>
    Task<LocalIperf3Status> CheckLocalIperf3Async(bool forceRefresh = false);

    /// <summary>
    /// Gets cached local iperf3 status.
    /// </summary>
    /// <returns>The cached status, or null if cache expired or not set.</returns>
    Task<LocalIperf3Status?> GetCachedLocalIperf3StatusAsync();
}

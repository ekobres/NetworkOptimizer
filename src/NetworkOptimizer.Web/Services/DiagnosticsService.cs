using Microsoft.Extensions.Caching.Memory;
using NetworkOptimizer.Audit.Services;
using NetworkOptimizer.Diagnostics;
using NetworkOptimizer.Diagnostics.Models;

namespace NetworkOptimizer.Web.Services;

/// <summary>
/// Service for running network diagnostics and caching results.
/// </summary>
public class DiagnosticsService
{
    private const string CacheKeyLastResultPrefix = "DiagnosticsService_LastResult_";
    private const string CacheKeyLastRunTimePrefix = "DiagnosticsService_LastRunTime_";
    private const string CacheKeyIsRunningPrefix = "DiagnosticsService_IsRunning_";

    private readonly ILogger<DiagnosticsService> _logger;
    private readonly UniFiConnectionService _connectionService;
    private readonly FingerprintDatabaseService _fingerprintService;
    private readonly IeeeOuiDatabase _ieeeOuiDb;
    private readonly IMemoryCache _cache;
    private readonly ILoggerFactory _loggerFactory;

    public DiagnosticsService(
        ILogger<DiagnosticsService> logger,
        UniFiConnectionService connectionService,
        FingerprintDatabaseService fingerprintService,
        IeeeOuiDatabase ieeeOuiDb,
        IMemoryCache cache,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _connectionService = connectionService;
        _fingerprintService = fingerprintService;
        _ieeeOuiDb = ieeeOuiDb;
        _cache = cache;
        _loggerFactory = loggerFactory;
    }

    private static string GetCacheKeyLastResult(int siteId) => $"{CacheKeyLastResultPrefix}{siteId}";
    private static string GetCacheKeyLastRunTime(int siteId) => $"{CacheKeyLastRunTimePrefix}{siteId}";
    private static string GetCacheKeyIsRunning(int siteId) => $"{CacheKeyIsRunningPrefix}{siteId}";

    /// <summary>
    /// Get the last diagnostics result (if any).
    /// </summary>
    public DiagnosticsResult? GetLastResult(int siteId) => _cache.Get<DiagnosticsResult>(GetCacheKeyLastResult(siteId));

    /// <summary>
    /// Get the time of the last diagnostics run.
    /// </summary>
    public DateTime? GetLastRunTime(int siteId) => _cache.Get<DateTime?>(GetCacheKeyLastRunTime(siteId));

    /// <summary>
    /// Check if diagnostics are currently running.
    /// </summary>
    public bool IsRunning(int siteId) => _cache.Get<bool>(GetCacheKeyIsRunning(siteId));

    /// <summary>
    /// Clear cached diagnostics results.
    /// </summary>
    public void ClearCache(int siteId)
    {
        _cache.Remove(GetCacheKeyLastResult(siteId));
        _cache.Remove(GetCacheKeyLastRunTime(siteId));
        _logger.LogInformation("Diagnostics cache cleared for site {SiteId}", siteId);
    }

    /// <summary>
    /// Run network diagnostics.
    /// </summary>
    /// <param name="siteId">The site ID to run diagnostics for</param>
    /// <param name="options">Options to control which analyzers run</param>
    /// <returns>Diagnostics result</returns>
    public async Task<DiagnosticsResult> RunDiagnosticsAsync(int siteId, DiagnosticsOptions? options = null)
    {
        if (IsRunning(siteId))
        {
            _logger.LogWarning("Diagnostics already running for site {SiteId}, returning last result", siteId);
            return GetLastResult(siteId) ?? new DiagnosticsResult();
        }

        _cache.Set(GetCacheKeyIsRunning(siteId), true);

        try
        {
            _logger.LogInformation("Starting diagnostics run for site {SiteId}", siteId);

            var client = _connectionService.GetClient(siteId);
            if (!_connectionService.IsConnected(siteId) || client == null)
            {
                _logger.LogWarning("Cannot run diagnostics: UniFi controller not connected for site {SiteId}", siteId);
                return CreateErrorResult("Controller Not Connected",
                    "Cannot run diagnostics without an active connection to the UniFi controller.");
            }

            // Fetch all required data in parallel
            var devicesTask = client.GetDevicesAsync();
            var clientsTask = client.GetClientsAsync();
            var networksTask = client.GetNetworkConfigsAsync();
            var portProfilesTask = client.GetPortProfilesAsync();
            var clientHistoryTask = client.GetClientHistoryAsync(withinHours: 720); // 30 days

            await Task.WhenAll(devicesTask, clientsTask, networksTask, portProfilesTask, clientHistoryTask);

            var devices = await devicesTask;
            var clients = await clientsTask;
            var networks = await networksTask;
            var portProfiles = await portProfilesTask;
            var clientHistory = await clientHistoryTask;

            _logger.LogInformation(
                "Fetched data for diagnostics: {DeviceCount} devices, {ClientCount} clients, " +
                "{NetworkCount} networks, {ProfileCount} port profiles, {HistoryCount} history clients",
                devices.Count, clients.Count, networks.Count, portProfiles.Count, clientHistory.Count);

            // Get fingerprint database for device detection
            var fingerprintDb = await _fingerprintService.GetDatabaseAsync();

            // Create device detection service with all available data sources
            var deviceDetection = new DeviceTypeDetectionService(
                _loggerFactory.CreateLogger<DeviceTypeDetectionService>(),
                fingerprintDb,
                _ieeeOuiDb,
                _loggerFactory);

            // Create and run the diagnostics engine
            var engine = new DiagnosticsEngine(
                deviceDetection,
                _loggerFactory.CreateLogger<DiagnosticsEngine>(),
                _loggerFactory.CreateLogger<Diagnostics.Analyzers.ApLockAnalyzer>(),
                _loggerFactory.CreateLogger<Diagnostics.Analyzers.TrunkConsistencyAnalyzer>(),
                _loggerFactory.CreateLogger<Diagnostics.Analyzers.PortProfileSuggestionAnalyzer>());

            var result = engine.RunDiagnostics(clients, devices, portProfiles, networks, options, clientHistory);

            // Cache the result
            _cache.Set(GetCacheKeyLastResult(siteId), result);
            _cache.Set(GetCacheKeyLastRunTime(siteId), DateTime.UtcNow);

            _logger.LogInformation(
                "Diagnostics completed for site {SiteId}: {Total} issues found in {Duration}ms",
                siteId, result.TotalIssueCount, result.Duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diagnostics run failed for site {SiteId}", siteId);
            return CreateErrorResult("Diagnostics Failed", ex.Message);
        }
        finally
        {
            _cache.Set(GetCacheKeyIsRunning(siteId), false);
        }
    }

    private static DiagnosticsResult CreateErrorResult(string title, string message)
    {
        return new DiagnosticsResult
        {
            Timestamp = DateTime.UtcNow,
            // Add a synthetic issue to show the error
            ApLockIssues = new List<ApLockIssue>
            {
                new ApLockIssue
                {
                    ClientName = title,
                    Recommendation = message,
                    Severity = ApLockSeverity.Unknown
                }
            }
        };
    }
}

using Microsoft.Extensions.Logging;

namespace NetworkOptimizer.Storage;

/// <summary>
/// Base class for repositories providing common error handling patterns.
/// </summary>
public abstract class RepositoryBase
{
    protected readonly ILogger Logger;

    protected RepositoryBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an async operation with standardized error logging.
    /// </summary>
    protected async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{OperationName} failed", operationName);
            throw;
        }
    }

    /// <summary>
    /// Executes an async operation with standardized error logging.
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{OperationName} failed", operationName);
            throw;
        }
    }

    /// <summary>
    /// Executes an async operation with a default value on failure.
    /// </summary>
    protected async Task<T?> ExecuteWithDefaultAsync<T>(Func<Task<T>> operation, string operationName, T? defaultValue = default)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{OperationName} failed, returning default", operationName);
            return defaultValue;
        }
    }
}

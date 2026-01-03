using Microsoft.Extensions.DependencyInjection;

namespace NetworkOptimizer.Core.Extensions;

/// <summary>
/// Extension methods for IServiceProvider.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Creates a scope and resolves a service, executing an action with it.
    /// The scope is disposed after the action completes.
    /// </summary>
    public static T WithScopedService<TService, T>(this IServiceProvider provider, Func<TService, T> action)
        where TService : notnull
    {
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return action(service);
    }

    /// <summary>
    /// Creates a scope and resolves a service, executing an async action with it.
    /// The scope is disposed after the action completes.
    /// </summary>
    public static async Task<T> WithScopedServiceAsync<TService, T>(this IServiceProvider provider, Func<TService, Task<T>> action)
        where TService : notnull
    {
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(service);
    }

    /// <summary>
    /// Creates a scope and resolves a service, executing an async action with it.
    /// The scope is disposed after the action completes.
    /// </summary>
    public static async Task WithScopedServiceAsync<TService>(this IServiceProvider provider, Func<TService, Task> action)
        where TService : notnull
    {
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        await action(service);
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Lujke;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Single place to register services for dependency injection.
    /// Add new registrations here, e.g. services.AddSingleton&lt;IMyService, MyService&gt;();
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        return services;
    }
}

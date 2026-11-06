using Microsoft.Extensions.DependencyInjection;

namespace HR_System.Infrastructure.Decorators.Loggers;

public static class DecoratorRegistrationExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TImplementation"/> as a plain scoped service and exposes
    /// <typeparamref name="TService"/> as <typeparamref name="TDecorator"/> wrapping it.
    /// The decorator must take a <typeparamref name="TService"/> as its first constructor argument;
    /// every other argument (e.g. the logger) is resolved from the container.
    /// </summary>
    public static IServiceCollection AddScopedDecorated<TService, TImplementation, TDecorator>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
        where TDecorator : class, TService
    {
        services.AddScoped<TImplementation>();
        services.AddScoped<TService>(sp =>
            ActivatorUtilities.CreateInstance<TDecorator>(sp, sp.GetRequiredService<TImplementation>()));

        return services;
    }
}

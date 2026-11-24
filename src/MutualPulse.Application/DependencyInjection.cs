using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MutualPulse.Shared.Abstractions;
using MutualPulse.Shared.Decorators;
using MutualPulse.Shared.Validation;

namespace MutualPulse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Keep the ChatPulse CQRS/validation architecture intact for future features.
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddValidatorsFromAssembly(assembly);
        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableToAny(
                typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime());
        services.AddScoped(typeof(ValidationExecutor<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(CommandValidationDecorator<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(QueryValidationDecorator<,>));
        return services;
    }
}

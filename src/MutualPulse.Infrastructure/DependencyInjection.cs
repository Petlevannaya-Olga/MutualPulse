using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MutualPulse.Infrastructure.Persistence;

namespace MutualPulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        services.AddDbContext<MutualPulseDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        return services;
    }
}

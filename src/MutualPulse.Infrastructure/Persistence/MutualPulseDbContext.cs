using Microsoft.EntityFrameworkCore;

namespace MutualPulse.Infrastructure.Persistence;

/// <summary>
/// SQLite foundation retained for the future account, publication and interaction models.
/// No CRM tables are created by the onboarding-only version.
/// </summary>
public sealed class MutualPulseDbContext(DbContextOptions<MutualPulseDbContext> options) : DbContext(options)
{
}

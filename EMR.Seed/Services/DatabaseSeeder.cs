using EMR.Persistence.Contexts;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace EMR.Seed.Services;

public class DatabaseSeeder(
    AppDbContext db,
    ILogger<DatabaseSeeder> logger,
    IStringLocalizer<DatabaseSeeder> localizer)
    : IDatabaseSeeder
{
    public void Initialize()
    {
        db.SaveChanges();
    }
}
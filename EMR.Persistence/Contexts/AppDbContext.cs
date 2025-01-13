using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EMR.Domain.Contracts;
using EMR.Domain.Entities;
using EMR.Domain.Entities.Settings;
using EMR.Domain.Entities.Users;
using EMR.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EMR.Persistence.Contexts;

public class AppDbContext : AuditableContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService,
        IDateTimeService dateTimeService) : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public DbSet<AppVersion> AppVersions { get; set; }

    public DbSet<Device> Devices { get; set; }

    public DbSet<Preference> Preferences { get; set; }

    public DbSet<OTP> Otps { get; set; }

    public DbSet<Users> Users { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>().ToList())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = _dateTimeService.NowUtc;
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedOn = _dateTimeService.NowUtc;
                    entry.Entity.LastModifiedBy = _currentUserService.UserId;
                    break;
            }

        if (_currentUserService.UserId == null)
            return await base.SaveChangesAsync(cancellationToken);
        return await base.SaveChangesAsync(_currentUserService.UserId, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            property.SetColumnType("numeric(18,2)");

        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.Name is "LastModifiedBy" or "CreatedBy"))
            property.SetColumnType("varchar(128)");

        base.OnModelCreating(builder);
    }
}
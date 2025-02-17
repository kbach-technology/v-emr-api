using EMR.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.KeycloakId).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(100);
        builder.Property(u => u.FullName).HasMaxLength(255);
        builder.Property(u => u.Phone).HasMaxLength(20);
        
        builder.Property(u => u.IsActive).HasDefaultValue(true);

        builder.HasKey(x => x.Id);

        // Configure Dob as owned type
        builder.OwnsOne(
            u => u.DateOfBirth,
            dob =>
            {
                dob.Property(d => d.Value)
                    .HasColumnName("DateOfBirth")
                    .IsRequired();

                // Explicitly ignore the computed Age property
                dob.Ignore(d => d.Age);
            });
         
        builder.OwnsOne(
            u => u.Gender,
            gender =>
            {
                gender.Property(d => d.Value)
                    .HasColumnName("Gender")
                    .IsRequired();
            });
            
    }
}
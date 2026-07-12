using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BcgHub.Api.Infrastructure.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> entity)
    {
        entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
        entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
        entity.HasIndex(x => x.Email).IsUnique();
    }
}

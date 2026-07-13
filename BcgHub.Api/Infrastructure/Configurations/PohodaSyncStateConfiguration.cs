using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BcgHub.Api.Infrastructure.Configurations;

public sealed class PohodaSyncStateConfiguration : IEntityTypeConfiguration<PohodaSyncState>
{
    public void Configure(EntityTypeBuilder<PohodaSyncState> entity)
    {
        entity.Property(x => x.LastRunId).HasMaxLength(32);
        entity.Property(x => x.LastTrigger).HasMaxLength(50);
        entity.Property(x => x.LastError).HasMaxLength(4000);
    }
}

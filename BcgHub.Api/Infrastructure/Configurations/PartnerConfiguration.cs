using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BcgHub.Api.Infrastructure.Configurations;

public sealed class PartnerConfiguration : IEntityTypeConfiguration<BusinessPartner>, IEntityTypeConfiguration<ContactPerson>
{
    public void Configure(EntityTypeBuilder<BusinessPartner> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
        entity.Property(x => x.CompanyNumber).HasMaxLength(50);
        entity.Property(x => x.VatNumber).HasMaxLength(50);
        entity.Property(x => x.Email).HasMaxLength(320);
        entity.Property(x => x.Phone).HasMaxLength(100);
        entity.Property(x => x.Website).HasMaxLength(500);
        entity.Property(x => x.Street).HasMaxLength(300);
        entity.Property(x => x.City).HasMaxLength(200);
        entity.Property(x => x.PostalCode).HasMaxLength(30);
        entity.Property(x => x.CountryCode).HasMaxLength(2);
        entity.Property(x => x.Notes).HasMaxLength(10000);
        entity.Property(x => x.TransportCapabilities).HasMaxLength(2000);
        entity.HasIndex(x => new { x.Type, x.Name });
        entity.HasMany(x => x.Contacts).WithOne(x => x.BusinessPartner).HasForeignKey(x => x.BusinessPartnerId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ContactPerson> entity)
    {
        entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Position).HasMaxLength(200);
        entity.Property(x => x.Email).HasMaxLength(320);
        entity.Property(x => x.Phone).HasMaxLength(100);
        entity.HasIndex(x => x.Email);
        entity.HasIndex(x => x.BusinessPartnerId).IsUnique().HasFilter("\"IsPrimary\" = TRUE");
    }
}

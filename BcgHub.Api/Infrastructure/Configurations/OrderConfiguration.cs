using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BcgHub.Api.Infrastructure.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>, IEntityTypeConfiguration<OrderWorkflowStep>, IEntityTypeConfiguration<TransportQuote>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.Property(x => x.Number).HasMaxLength(50).IsRequired();
        entity.Property(x => x.PohodaOrderNumber).HasMaxLength(50);
        entity.Property(x => x.PohodaOrderId).HasMaxLength(200);
        entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
        entity.Property(x => x.WarehouseInstructions).HasMaxLength(10000);
        entity.Property(x => x.ValueCzk).HasPrecision(18, 2);
        entity.Property(x => x.WeightKg).HasPrecision(18, 3);
        entity.Property(x => x.VolumeM3).HasPrecision(18, 3);
        entity.HasIndex(x => x.Number).IsUnique();
        entity.HasIndex(x => x.PohodaOrderId).IsUnique().HasFilter("\"PohodaOrderId\" IS NOT NULL");
        entity.HasIndex(x => new { x.Status, x.PlannedDeliveryOn });
        entity.ToTable(table => table.HasCheckConstraint("CK_Orders_NonNegativeValues", "\"ValueCzk\" >= 0 AND \"WeightKg\" >= 0 AND \"VolumeM3\" >= 0"));
        entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CustomerContact).WithMany().HasForeignKey(x => x.CustomerContactId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(x => x.Carrier).WithMany().HasForeignKey(x => x.CarrierId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(x => x.CustomsDeclarant).WithMany().HasForeignKey(x => x.CustomsDeclarantId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<OrderWorkflowStep> entity)
    {
        entity.Property(x => x.Notes).HasMaxLength(5000);
        entity.HasIndex(x => new { x.OrderId, x.Type }).IsUnique();
        entity.HasOne(x => x.Order).WithMany(x => x.WorkflowSteps).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<TransportQuote> entity)
    {
        entity.Property(x => x.Price).HasPrecision(18, 2);
        entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        entity.Property(x => x.Notes).HasMaxLength(5000);
        entity.ToTable(table => table.HasCheckConstraint("CK_TransportQuotes_NonNegativePrice", "\"Price\" >= 0"));
        entity.HasIndex(x => x.OrderId).IsUnique().HasFilter("\"IsSelected\" = TRUE");
        entity.HasOne(x => x.Order).WithMany(x => x.TransportQuotes).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Carrier).WithMany().HasForeignKey(x => x.CarrierId).OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyTransfer.Domain.Entities;

namespace MoneyTransfer.Infrastructure.Data.Configurations;

public sealed class ClientPaymentHeaderConfiguration : IEntityTypeConfiguration<ClientPaymentHeader>
{
    public void Configure(EntityTypeBuilder<ClientPaymentHeader> builder)
    {
        builder.ToTable("ClientPaymentHeaders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientId)
            .IsRequired();

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.PaymentReference)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.ClientId);

        builder.HasIndex(x => new { x.OrganizationId, x.PaymentReference })
            .IsUnique();

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Allocations)
            .WithOne(x => x.ClientPaymentHeader)
            .HasForeignKey(x => x.ClientPaymentHeaderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
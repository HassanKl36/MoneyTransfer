using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyTransfer.Domain.Entities;

namespace MoneyTransfer.Infrastructure.Data.Configurations;

public sealed class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("Discounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectId)
            .IsRequired();

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.DiscountReference)
            .IsRequired()
            .HasMaxLength(31);

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(x => new { x.OrganizationId, x.DiscountReference })
            .IsUnique();

        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
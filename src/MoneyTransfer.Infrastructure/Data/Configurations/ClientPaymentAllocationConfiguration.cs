using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyTransfer.Domain.Entities;

namespace MoneyTransfer.Infrastructure.Data.Configurations;

public sealed class ClientPaymentAllocationConfiguration : IEntityTypeConfiguration<ClientPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<ClientPaymentAllocation> builder)
    {
        builder.ToTable("ClientPaymentAllocations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientPaymentHeaderId)
            .IsRequired();

        builder.Property(x => x.ProjectId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(x => x.ClientPaymentHeaderId);
        builder.HasIndex(x => x.ProjectId);

        builder.HasOne(x => x.ClientPaymentHeader)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.ClientPaymentHeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
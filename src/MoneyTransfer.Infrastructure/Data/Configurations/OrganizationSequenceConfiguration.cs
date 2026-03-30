using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyTransfer.Domain.Entities;

namespace MoneyTransfer.Infrastructure.Data.Configurations;

public sealed class OrganizationSequenceConfiguration : IEntityTypeConfiguration<OrganizationSequence>
{
    public void Configure(EntityTypeBuilder<OrganizationSequence> builder)
    {
        builder.ToTable("OrganizationSequences");

        builder.HasKey(x => x.OrganizationId);

        builder.Property(x => x.NextProjectNumber)
            .IsRequired();

        builder.Property(x => x.NextInvoiceNumber)
            .IsRequired();

        builder.Property(x => x.NextPaymentNumber)
            .IsRequired();

        builder.HasOne(x => x.Organization)
            .WithOne(x => x.Sequence)
            .HasForeignKey<OrganizationSequence>(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
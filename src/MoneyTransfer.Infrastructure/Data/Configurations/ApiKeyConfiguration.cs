using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyTransfer.Domain.Entities;

namespace MoneyTransfer.Infrastructure.Data.Configurations;

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.KeyPrefix)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(x => x.HashedKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.HashedKey)
            .IsUnique();

        builder.HasIndex(x => x.OrganizationId);

        builder.HasIndex(x => new { x.OrganizationId, x.RevokedAt });

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
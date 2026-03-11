using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Infrastructure.Identity;

namespace MoneyTransfer.Infrastructure.Data;

public sealed class MoneyTransferDbContext : IdentityDbContext<ApplicationUser>
{
    public MoneyTransferDbContext(DbContextOptions<MoneyTransferDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Explicit configurations live in this assembly:
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MoneyTransferDbContext).Assembly);

        // Identity user tenant hooks (optional FKs)
        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.HasOne(u => u.Organization)
             .WithMany()
             .HasForeignKey(u => u.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(u => u.Client)
             .WithMany()
             .HasForeignKey(u => u.ClientId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(u => u.OrganizationId);
            b.HasIndex(u => u.ClientId);
        });
    }
}
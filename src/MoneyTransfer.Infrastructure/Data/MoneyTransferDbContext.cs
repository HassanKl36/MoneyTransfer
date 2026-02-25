using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Domain.Entities;

namespace MoneyTransfer.Infrastructure.Data;

public sealed class MoneyTransferDbContext : DbContext
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
    }
}
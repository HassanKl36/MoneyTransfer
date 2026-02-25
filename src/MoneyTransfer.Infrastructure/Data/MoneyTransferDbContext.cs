using Microsoft.EntityFrameworkCore;

namespace MoneyTransfer.Infrastructure.Data;

public sealed class MoneyTransferDbContext : DbContext
{
    public MoneyTransferDbContext(DbContextOptions<MoneyTransferDbContext> options)
        : base(options)
    {
    }

    // Baseline model only (no business entities yet).
    // This DbSet exists solely to ensure EF Core creates at least one table in the initial migration.
    public DbSet<DbPing> DbPings => Set<DbPing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DbPing>(entity =>
        {
            entity.ToTable("DbPings");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Note)
                .HasMaxLength(200);

            entity.Property(x => x.CreatedUtc)
                .HasPrecision(0);
        });
    }
}

public sealed class DbPing
{
    public int Id { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MoneyTransfer.Infrastructure.Data;

public sealed class MoneyTransferDbContextFactory : IDesignTimeDbContextFactory<MoneyTransferDbContext>
{
    public MoneyTransferDbContext CreateDbContext(string[] args)
    {
        // Used only by `dotnet ef` at design-time.
        // Keep this dev-safe and self-contained.
        var connectionString =
            "Server=.\\SQLEXPRESS;Database=MoneyTransferDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<MoneyTransferDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new MoneyTransferDbContext(optionsBuilder.Options);
    }
}
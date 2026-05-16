using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MoneyTransfer.Infrastructure.Data;

public sealed class MoneyTransferDbContextFactory : IDesignTimeDbContextFactory<MoneyTransferDbContext>
{
    public MoneyTransferDbContext CreateDbContext(string[] args)
    {
        // Used only by EF Core design-time tooling.
        // Runtime DbContext configuration is done in MoneyTransfer.Web/Program.cs.
        var connectionString = ResolveConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<MoneyTransferDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new MoneyTransferDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var webProjectDirectory = FindWebProjectDirectory();

        var appsettingsPath = Path.Combine(webProjectDirectory, "appsettings.json");
        var developmentSettingsPath = Path.Combine(webProjectDirectory, "appsettings.Development.json");

        var connectionString = ReadDefaultConnectionString(appsettingsPath);

        if (File.Exists(developmentSettingsPath))
        {
            var developmentConnectionString = ReadDefaultConnectionString(developmentSettingsPath);

            if (!string.IsNullOrWhiteSpace(developmentConnectionString))
            {
                connectionString = developmentConnectionString;
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection was not found in the Web project's appsettings files.");
        }

        return connectionString;
    }

    private static string FindWebProjectDirectory()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory is not null)
        {
            var directCandidate = Path.Combine(
                currentDirectory.FullName,
                "MoneyTransfer.Web");

            if (Directory.Exists(directCandidate) &&
                File.Exists(Path.Combine(directCandidate, "appsettings.json")))
            {
                return directCandidate;
            }

            var srcCandidate = Path.Combine(
                currentDirectory.FullName,
                "src",
                "MoneyTransfer.Web");

            if (Directory.Exists(srcCandidate) &&
                File.Exists(Path.Combine(srcCandidate, "appsettings.json")))
            {
                return srcCandidate;
            }

            if (currentDirectory.Name == "MoneyTransfer.Web" &&
                File.Exists(Path.Combine(currentDirectory.FullName, "appsettings.json")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the MoneyTransfer.Web project directory.");
    }

    private static string? ReadDefaultConnectionString(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
        {
            return null;
        }

        if (!connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection))
        {
            return null;
        }

        return defaultConnection.GetString();
    }
}
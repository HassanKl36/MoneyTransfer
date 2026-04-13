using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Clients;
using MoneyTransfer.Domain.Enums;
using MoneyTransfer.Infrastructure.Data;
using MoneyTransfer.Infrastructure.Identity;

namespace MoneyTransfer.Infrastructure.Services.Clients;

public sealed class ClientService : IClientService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentClient _currentClient;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClientService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization,
        ICurrentUser currentUser,
        ICurrentClient currentClient,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _currentClient = currentClient;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<ClientListItemDto>> GetClientsAsync(
        string? search = null,
        ClientStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var query = _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(c =>
                c.Name.Contains(term) ||
                c.PhoneNumber.Contains(term) ||
                (c.Email != null && c.Email.Contains(term)));
        }

        query = status.HasValue
            ? query.Where(c => c.Status == status.Value)
            : query.Where(c => c.Status == ClientStatus.Active);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new ClientListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Status = c.Status
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientDetailsDto?> GetDetailsAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        return await BuildClientOverviewAsync(
            organizationId,
            clientId,
            includeArchived: true,
            cancellationToken);
    }

    public async Task<ClientDetailsDto?> GetClientOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var clientId = _currentClient.ClientId
            ?? throw new InvalidOperationException("Current customer client is not resolved.");

        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        return await BuildClientOverviewAsync(
            organizationId,
            clientId,
            includeArchived: false,
            cancellationToken);
    }

    public async Task<ClientLedgerDetailsDto?> GetClientLedgerAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? projectId = null,
        LedgerEntryType? transactionType = null,
        CancellationToken cancellationToken = default)
    {
        var clientId = _currentClient.ClientId
            ?? throw new InvalidOperationException("Current customer client is not resolved.");

        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var client = await _dbContext.Clients
            .AsNoTracking()
            .Where(c =>
                c.Id == clientId &&
                c.OrganizationId == organizationId &&
                c.Status == ClientStatus.Active)
            .Select(c => new
            {
                c.Id
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            return null;
        }

        var allEntries = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.OrganizationId == organizationId &&
                x.ClientId == clientId &&
                !x.IsVoided)
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.ProjectId,
                ProjectName = x.Project != null ? x.Project.Name : string.Empty,
                ProjectCode = x.Project != null ? x.Project.Code : null,
                x.OccurredAt,
                x.Type,
                x.Amount,
                x.InvoiceNumber,
                x.Notes,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var runningBalance = 0m;
        var historicalEntries = new List<ClientLedgerEntryDto>(allEntries.Count);

        foreach (var entry in allEntries)
        {
            runningBalance += entry.Amount;

            historicalEntries.Add(new ClientLedgerEntryDto
            {
                ProjectId = entry.ProjectId,
                ProjectName = entry.ProjectName,
                ProjectCode = entry.ProjectCode,
                OccurredAt = entry.OccurredAt,
                Type = entry.Type,
                Amount = entry.Amount,
                RunningBalance = runningBalance,
                Reference = entry.Type == LedgerEntryType.Invoice
                    ? entry.InvoiceNumber
                    : entry.Type == LedgerEntryType.Payment
                        ? GetPaymentReference(entry.Notes)
                        : entry.Type == LedgerEntryType.Discount
                            ? GetDiscountReference(entry.Notes)
                            : null,
                Notes = entry.Type == LedgerEntryType.Payment
                    ? GetPaymentNotes(entry.Notes)
                    : entry.Type == LedgerEntryType.Discount
                        ? GetDiscountNotes(entry.Notes)
                        : entry.Notes
            });
        }

        IEnumerable<ClientLedgerEntryDto> filteredEntries = historicalEntries;

        if (fromDate.HasValue)
        {
            var fromDateValue = fromDate.Value.Date;
            filteredEntries = filteredEntries.Where(x => x.OccurredAt >= fromDateValue);
        }

        if (toDate.HasValue)
        {
            var toDateExclusive = toDate.Value.Date.AddDays(1);
            filteredEntries = filteredEntries.Where(x => x.OccurredAt < toDateExclusive);
        }

        if (projectId.HasValue)
        {
            filteredEntries = filteredEntries.Where(x => x.ProjectId == projectId.Value);
        }

        if (transactionType.HasValue)
        {
            filteredEntries = filteredEntries.Where(x => x.Type == transactionType.Value);
        }

        var displayEntries = filteredEntries
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.ProjectName)
            .ThenByDescending(x => x.ProjectId)
            .ToList();

        var overview = await BuildClientOverviewAsync(
            organizationId,
            clientId,
            includeArchived: false,
            cancellationToken);

        if (overview is null)
        {
            return null;
        }

        return new ClientLedgerDetailsDto
        {
            TotalBalance = overview.TotalBalance,
            AsOfDate = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            ProjectId = projectId,
            TransactionType = transactionType,
            Projects = overview.Projects,
            Entries = displayEntries
        };
    }

    public async Task<ClientEditDto?> GetForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var client = await _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.Id == id)
            .Select(c => new ClientEditDto
            {
                Id = c.Id,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Status = c.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            return null;
        }

        client.HasPortalAccount = await _dbContext.Users
            .AnyAsync(u => u.ClientId == id, cancellationToken);

        return client;
    }

    public async Task<(bool Succeeded, List<string> Errors)> CreateAsync(
        ClientEditDto model,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var client = new Domain.Entities.Client
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = model.Name.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
            Status = ClientStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = GetRequiredUserId()
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _dbContext.Clients.Add(client);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (model.CreatePortalAccount)
            {
                var hasExistingUser = await _dbContext.Users
                    .AnyAsync(u => u.ClientId == client.Id, cancellationToken);

                if (hasExistingUser)
                {
                    errors.Add("This client already has a portal account.");
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, errors);
                }

                if (string.IsNullOrWhiteSpace(model.PortalUsername))
                    errors.Add("Username is required.");

                if (string.IsNullOrWhiteSpace(model.PortalPassword))
                    errors.Add("Password is required.");

                if (errors.Count > 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, errors);
                }

                var existingUser = await _userManager.FindByNameAsync(model.PortalUsername!);
                if (existingUser is not null)
                {
                    errors.Add("A user with this username already exists.");
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, errors);
                }

                var user = new ApplicationUser
                {
                    UserName = model.PortalUsername,
                    Email = model.PortalUsername,
                    ClientId = client.Id,
                    OrganizationId = client.OrganizationId
                };

                var createResult = await _userManager.CreateAsync(user, model.PortalPassword!);

                if (!createResult.Succeeded)
                {
                    errors.AddRange(createResult.Errors.Select(e => e.Description));
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, errors);
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "Customer");

                if (!roleResult.Succeeded)
                {
                    errors.AddRange(roleResult.Errors.Select(e => e.Description));
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, errors);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, errors);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Succeeded, List<string> Errors)> UpdateAsync(
        ClientEditDto model,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId && c.Id == model.Id,
                cancellationToken);

        if (client is null)
        {
            return (false, errors);
        }

        client.Name = model.Name.Trim();
        client.PhoneNumber = model.PhoneNumber.Trim();
        client.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        client.Status = model.Status;

        var hasExistingUser = await _dbContext.Users
            .AnyAsync(u => u.ClientId == client.Id, cancellationToken);

        if (model.CreatePortalAccount && !hasExistingUser)
        {
            if (string.IsNullOrWhiteSpace(model.PortalUsername))
                errors.Add("Username is required.");

            if (string.IsNullOrWhiteSpace(model.PortalPassword))
                errors.Add("Password is required.");

            if (errors.Count > 0)
                return (false, errors);

            var user = new ApplicationUser
            {
                UserName = model.PortalUsername,
                Email = model.PortalUsername,
                ClientId = client.Id,
                OrganizationId = client.OrganizationId
            };

            var createResult = await _userManager.CreateAsync(user, model.PortalPassword!);

            if (!createResult.Succeeded)
            {
                errors.AddRange(createResult.Errors.Select(e => e.Description));
                return (false, errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Customer");

            if (!roleResult.Succeeded)
            {
                errors.AddRange(roleResult.Errors.Select(e => e.Description));
                return (false, errors);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (true, errors);
    }

    public async Task<bool> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId && c.Id == id,
                cancellationToken);

        if (client is null)
        {
            return false;
        }

        client.Status = ClientStatus.Archived;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<ClientDetailsDto?> BuildClientOverviewAsync(
        Guid organizationId,
        Guid clientId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var clientQuery = _dbContext.Clients
            .AsNoTracking()
            .Where(c =>
                c.OrganizationId == organizationId &&
                c.Id == clientId);

        if (!includeArchived)
        {
            clientQuery = clientQuery.Where(c => c.Status == ClientStatus.Active);
        }

        var client = await clientQuery
            .Select(c => new ClientDetailsDto
            {
                Id = c.Id,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                AsOfDate = DateTime.UtcNow
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            return null;
        }

        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Where(p =>
                p.OrganizationId == organizationId &&
                p.ClientId == clientId)
            .OrderBy(p => p.Name)
            .Select(p => new ClientProjectBalanceDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                ProjectCode = p.Code,
                Balance = 0m
            })
            .ToListAsync(cancellationToken);

        var projectBalances = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.OrganizationId == organizationId &&
                x.ClientId == clientId &&
                !x.IsVoided)
            .GroupBy(x => x.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                Balance = g.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(
                x => x.ProjectId,
                x => x.Balance,
                cancellationToken);

        foreach (var project in projects)
        {
            if (projectBalances.TryGetValue(project.ProjectId, out var balance))
            {
                project.Balance = balance;
            }
        }

        client.Projects = projects;
        client.TotalBalance = projects.Sum(x => x.Balance);

        return client;
    }

    private static string? GetPaymentReference(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var firstSegment = notes.Split('|', 2)[0].Trim();

        const string prefix = "Payment Ref:";

        if (firstSegment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return firstSegment[prefix.Length..].Trim();
        }

        return firstSegment;
    }

    private static string? GetPaymentNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var parts = notes.Split('|', 2);

        if (parts.Length < 2)
        {
            return null;
        }

        var remainingNotes = parts[1].Trim();

        return string.IsNullOrWhiteSpace(remainingNotes)
            ? null
            : remainingNotes;
    }

    private static string? GetDiscountReference(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var firstSegment = notes.Split('|', 2)[0].Trim();

        const string prefix = "Discount Ref:";

        if (firstSegment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return firstSegment[prefix.Length..].Trim();
        }

        return firstSegment;
    }

    private static string? GetDiscountNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var parts = notes.Split('|', 2);

        if (parts.Length < 2)
        {
            return null;
        }

        var remainingNotes = parts[1].Trim();

        return string.IsNullOrWhiteSpace(remainingNotes)
            ? null
            : remainingNotes;
    }

    private string GetRequiredUserId()
    {
        return _currentUser.UserId
            ?? throw new InvalidOperationException("Current user is not authenticated.");
    }
}
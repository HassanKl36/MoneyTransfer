using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Projects;
using MoneyTransfer.Domain.Enums;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Projects;

public sealed class ProjectService : IProjectService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ICurrentUser _currentUser;
    private readonly IFinancialIdentityGenerator _financialIdentityGenerator;

    public ProjectService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization,
        ICurrentUser currentUser,
        IFinancialIdentityGenerator financialIdentityGenerator)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _financialIdentityGenerator = financialIdentityGenerator;
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> GetProjectsAsync(
        string? search = null,
        ProjectStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var query = _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Code.Contains(term) ||
                (p.Description != null && p.Description.Contains(term)));
        }

        query = status.HasValue
            ? query.Where(p => p.Status == status.Value)
            : query.Where(p =>
                p.Status == ProjectStatus.Active ||
                p.Status == ProjectStatus.Completed);

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProjectListItemDto
            {
                Id = p.Id,
                ClientId = p.ClientId,
                ClientName = p.Client != null ? p.Client.Name : string.Empty,
                Name = p.Name,
                Code = p.Code,
                Description = p.Description,
                Status = p.Status
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectEditDto?> GetForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        return await _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.Id == id)
            .Select(p => new ProjectEditDto
            {
                Id = p.Id,
                ClientId = p.ClientId,
                Name = p.Name,
                Code = p.Code,
                Description = p.Description,
                Status = p.Status
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(
        ProjectEditDto model,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var clientExists = await _dbContext.Clients
            .AnyAsync(c =>
                c.OrganizationId == organizationId &&
                c.Id == model.ClientId,
                cancellationToken);

        if (!clientExists)
        {
            throw new InvalidOperationException("Invalid client selection.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var code = await _financialIdentityGenerator.GenerateProjectCodeAsync(
                organizationId,
                cancellationToken);

            var project = new Domain.Entities.Project
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ClientId = model.ClientId,
                Name = model.Name.Trim(),
                Code = code,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                Status = ProjectStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetRequiredUserId()
            };

            _dbContext.Projects.Add(project);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task<bool> UpdateAsync(
        ProjectEditDto model,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId && p.Id == model.Id,
                cancellationToken);

        if (project is null)
        {
            return false;
        }

        var clientExists = await _dbContext.Clients
            .AnyAsync(c =>
                c.OrganizationId == organizationId &&
                c.Id == model.ClientId,
                cancellationToken);

        if (!clientExists)
        {
            return false;
        }

        project.ClientId = model.ClientId;
        project.Name = model.Name.Trim();
        project.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        project.Status = model.Status;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId && p.Id == id,
                cancellationToken);

        if (project is null)
        {
            return false;
        }

        project.Status = ProjectStatus.Archived;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private string GetRequiredUserId()
    {
        return _currentUser.UserId
            ?? throw new InvalidOperationException(
                "Current user is not authenticated.");
    }
}
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Projects;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Projects;

public sealed class ProjectService : IProjectService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly IFinancialIdentityGenerator _financialIdentityGenerator;

    public ProjectService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization,
        IFinancialIdentityGenerator financialIdentityGenerator)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
        _financialIdentityGenerator = financialIdentityGenerator;
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> GetProjectsAsync(
        string? search = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

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

        if (!includeArchived)
        {
            query = query.Where(p => !p.IsArchived);
        }

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
                IsArchived = p.IsArchived
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectEditDto?> GetForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

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
                IsArchived = p.IsArchived
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(
        ProjectEditDto model,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var clientExists = await _dbContext.Clients
            .AnyAsync(c =>
                c.OrganizationId == organizationId &&
                c.Id == model.ClientId,
                cancellationToken);

        if (!clientExists)
        {
            throw new InvalidOperationException("Invalid client selection.");
        }

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
            IsArchived = false
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        ProjectEditDto model,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

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
        project.IsArchived = model.IsArchived;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId && p.Id == id,
                cancellationToken);

        if (project is null)
        {
            return false;
        }

        project.IsArchived = true;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Guid GetRequiredOrganizationId()
    {
        return _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException(
                "Current user is not associated with an organization.");
    }
}
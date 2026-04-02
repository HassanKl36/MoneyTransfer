using MoneyTransfer.Application.Services.Projects;
using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Application.Services.Projects;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectListItemDto>> GetProjectsAsync(
        string? search = null,
        ProjectStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<ProjectEditDto?> GetForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        ProjectEditDto model,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        ProjectEditDto model,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
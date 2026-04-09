namespace MoneyTransfer.Application.Services.OrgUsers;

public interface IOrgUserService
{
    Task<IReadOnlyList<OrgUserListItemDto>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, List<string> Errors)> CreateAsync(
        OrgUserCreateDto model,
        CancellationToken cancellationToken = default);
}
namespace MoneyTransfer.Application.Services.Projects;

public sealed class ProjectListItemDto
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsArchived { get; set; }
}
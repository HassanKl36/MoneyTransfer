using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Application.Services.Projects;

public sealed class ProjectEditDto
{
    public Guid Id { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Code { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsArchived { get; set; }
}
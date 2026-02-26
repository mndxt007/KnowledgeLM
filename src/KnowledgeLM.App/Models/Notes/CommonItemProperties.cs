using System.ComponentModel.DataAnnotations;

namespace KnowledgeLM.App.Models.Notes;

public abstract class CommonItemProperties
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime LastModified { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }
}

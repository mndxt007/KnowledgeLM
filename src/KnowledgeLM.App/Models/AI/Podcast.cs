using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KnowledgeLM.App.Models.Notes;

namespace KnowledgeLM.App.Models.AI;

public class Podcast
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ResourceId { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public Resource Resource { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? AudioFilePath { get; set; }

    public string? SsmlContent { get; set; }

    public long DurationTicks { get; set; }

    public TimeSpan Duration
    {
        get => TimeSpan.FromTicks(DurationTicks);
        set => DurationTicks = value.Ticks;
    }

    public DateTime CreatedAt { get; set; }
}

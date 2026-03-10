using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KnowledgeLM.App.Models.Notes;

namespace KnowledgeLM.App.Models.AI;

public class VectorizedResource
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ResourceId { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public Resource Resource { get; set; } = null!;

    public DateTime VectorizedAt { get; set; }

    public int ChunkCount { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
}

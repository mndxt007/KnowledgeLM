using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeLM.App.Models.Notes;

public class Resource : CommonItemProperties
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int SectionId { get; set; }

    [ForeignKey(nameof(SectionId))]
    public Section Section { get; set; } = null!;

    public ResourceType ResourceType { get; set; }
}

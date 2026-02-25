using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeLM.App.Models.Notes;

public class Section : CommonItemProperties
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public List<Resource> Resources { get; set; } = [];
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeLM.App.Models.Notes;

public class Course : CommonItemProperties
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public List<Section> Sections { get; set; } = [];
}

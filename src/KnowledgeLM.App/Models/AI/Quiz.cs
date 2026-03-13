using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KnowledgeLM.App.Models.Notes;

namespace KnowledgeLM.App.Models.AI;

public class Quiz
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ResourceId { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public Resource Resource { get; set; } = null!;

    [Required]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of option strings, e.g. ["Option A", "Option B", "Option C", "Option D"]
    /// </summary>
    [Required]
    public string OptionsJson { get; set; } = "[]";

    /// <summary>
    /// Zero-based index of the correct option in the OptionsJson array.
    /// </summary>
    public int CorrectOptionIndex { get; set; }

    /// <summary>
    /// Explanation shown after the user answers.
    /// </summary>
    public string? Explanation { get; set; }

    public DateTime CreatedAt { get; set; }
}

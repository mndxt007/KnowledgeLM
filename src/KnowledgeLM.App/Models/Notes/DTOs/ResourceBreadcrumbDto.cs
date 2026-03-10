using System.ComponentModel;

namespace KnowledgeLM.App.Models.Notes.DTOs;

public class ResourceBreadcrumbDto
{
    [Description("The unique identifier of the course")]
    public int CourseId { get; set; }

    [Description("The name of the course")]
    public string CourseName { get; set; } = string.Empty;

    [Description("The unique identifier of the section")]
    public int SectionId { get; set; }

    [Description("The name of the section")]
    public string SectionName { get; set; } = string.Empty;

    [Description("The unique identifier of the resource")]
    public int ResourceId { get; set; }

    [Description("The name of the resource")]
    public string ResourceName { get; set; } = string.Empty;
}

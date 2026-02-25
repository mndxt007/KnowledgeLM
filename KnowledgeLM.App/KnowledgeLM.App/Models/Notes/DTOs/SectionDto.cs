using System.ComponentModel;

namespace KnowledgeLM.App.Models.Notes.DTOs;

public class CreateSectionRequest
{
    [Description("The unique identifier of the parent course")]
    public int CourseId { get; set; }

    [Description("The name of the section")]
    public string Name { get; set; } = string.Empty;

    [Description("A brief description of the section content")]
    public string? Description { get; set; }

    [Description("The file path or URL where the section material is located")]
    public string? Location { get; set; }
}

public class UpdateSectionRequest
{
    [Description("The unique identifier of the section to update")]
    public int Id { get; set; }

    [Description("The updated name of the section")]
    public string Name { get; set; } = string.Empty;

    [Description("The updated description of the section content")]
    public string? Description { get; set; }

    [Description("The updated file path or URL where the section material is located")]
    public string? Location { get; set; }
}

public class SectionResponse
{
    [Description("The unique identifier of the section")]
    public int Id { get; set; }

    [Description("The unique identifier of the parent course")]
    public int CourseId { get; set; }

    [Description("The name of the section")]
    public string Name { get; set; } = string.Empty;

    [Description("A brief description of the section content")]
    public string? Description { get; set; }

    [Description("The file path or URL where the section material is located")]
    public string? Location { get; set; }

    [Description("The date and time when the section was created")]
    public DateTime CreatedAt { get; set; }

    [Description("The date and time when the section was last modified")]
    public DateTime LastModified { get; set; }

    [Description("The number of resources in this section")]
    public int ResourceCount { get; set; }
}

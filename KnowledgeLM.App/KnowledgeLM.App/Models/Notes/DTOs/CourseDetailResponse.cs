using System.ComponentModel;

namespace KnowledgeLM.App.Models.Notes.DTOs;

public class CourseDetailResponse
{
    [Description("The unique identifier of the course")]
    public int Id { get; set; }

    [Description("The name of the course")]
    public string Name { get; set; } = string.Empty;

    [Description("A brief description of the course content")]
    public string? Description { get; set; }

    [Description("The file path or URL where the course material is located")]
    public string? Location { get; set; }

    [Description("The date and time when the course was created")]
    public DateTime CreatedAt { get; set; }

    [Description("The date and time when the course was last modified")]
    public DateTime LastModified { get; set; }

    [Description("The list of sections belonging to this course")]
    public List<SectionDetailResponse> Sections { get; set; } = [];
}

public class SectionDetailResponse
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

    [Description("The list of resources belonging to this section")]
    public List<ResourceResponse> Resources { get; set; } = [];
}

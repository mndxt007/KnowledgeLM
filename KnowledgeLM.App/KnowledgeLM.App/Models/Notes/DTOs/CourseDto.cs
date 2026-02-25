using System.ComponentModel;

namespace KnowledgeLM.App.Models.Notes.DTOs;

public class CreateCourseRequest
{
    [Description("The name of the course")]
    public string Name { get; set; } = string.Empty;

    [Description("A brief description of the course content")]
    public string? Description { get; set; }

    [Description("The file path or URL where the course material is located")]
    public string? Location { get; set; }
}

public class UpdateCourseRequest
{
    [Description("The unique identifier of the course to update")]
    public int Id { get; set; }

    [Description("The updated name of the course")]
    public string Name { get; set; } = string.Empty;

    [Description("The updated description of the course content")]
    public string? Description { get; set; }

    [Description("The updated file path or URL where the course material is located")]
    public string? Location { get; set; }
}

public class CourseResponse
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

    [Description("The number of sections in this course")]
    public int SectionCount { get; set; }
}

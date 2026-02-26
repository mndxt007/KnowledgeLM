using System.ComponentModel;

namespace KnowledgeLM.App.Models.Notes.DTOs;

public class CreateResourceRequest
{
    [Description("The unique identifier of the parent section")]
    public int SectionId { get; set; }

    [Description("The name of the resource")]
    public string Name { get; set; } = string.Empty;

    [Description("The file path or URL where the resource is located")]
    public string? Location { get; set; }

    [Description("The type of resource: Image, Video, or Other")]
    public ResourceType ResourceType { get; set; }
}

public class UpdateResourceRequest
{
    [Description("The unique identifier of the resource to update")]
    public int Id { get; set; }

    [Description("The updated name of the resource")]
    public string Name { get; set; } = string.Empty;

    [Description("The updated file path or URL where the resource is located")]
    public string? Location { get; set; }

    [Description("The updated type of resource: Image, Video, or Other")]
    public ResourceType ResourceType { get; set; }
}

public class ResourceResponse
{
    [Description("The unique identifier of the resource")]
    public int Id { get; set; }

    [Description("The unique identifier of the parent section")]
    public int SectionId { get; set; }

    [Description("The name of the resource")]
    public string Name { get; set; } = string.Empty;

    [Description("The file path or URL where the resource is located")]
    public string? Location { get; set; }

    [Description("The type of resource: Image, Video, or Other")]
    public ResourceType ResourceType { get; set; }

    [Description("The date and time when the resource was created")]
    public DateTime CreatedAt { get; set; }

    [Description("The date and time when the resource was last modified")]
    public DateTime LastModified { get; set; }
}

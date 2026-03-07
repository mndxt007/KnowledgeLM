using System.ComponentModel;
using System.Text.Json.Serialization;

namespace KnowledgeLM.App.Models.Notes;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResourceType
{
    [Description("An image resource such as PNG, JPG, or SVG")]
    Image,

    [Description("A video resource such as MP4 or a streaming URL")]
    Video,

    [Description("A generic resource that does not fit other categories")]
    Other,

    [Description("A Markdown-formatted text resource")]
    Markdown
}

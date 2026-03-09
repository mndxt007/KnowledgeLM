namespace KnowledgeLM.MonacoEditor.Markdown;

/// <summary>
/// Options for configuring which Markdig extensions are enabled when rendering Markdown.
/// </summary>
public sealed class MarkdownOptions
{
    public static MarkdownOptions Default => new();

    public bool EnableFrontMatter { get; set; } = true;
    public bool EnableTables { get; set; } = true;
    public bool EnableGridTables { get; set; } = false;
    public bool EnableTaskLists { get; set; } = true;
    public bool EnableFootnotes { get; set; } = false;
    public bool EnableGenericAttributes { get; set; } = true;
    public bool EnableAutoIdentifiers { get; set; } = true;
    public bool EnableAutoLinks { get; set; } = true;
    public bool EnableSmartyPants { get; set; } = false;
    public bool EnableFigures { get; set; } = false;
    public bool EnableEmphasisExtras { get; set; } = true;
    public bool EnableListExtras { get; set; } = true;
    public bool EnableFencedCodeBlocks { get; set; } = true;
}

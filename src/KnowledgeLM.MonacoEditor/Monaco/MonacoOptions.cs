namespace KnowledgeLM.MonacoEditor.Monaco;

public sealed class MonacoOptions
{
    public bool WordWrap { get; set; } = true;
    public bool Minimap { get; set; } = false;
    public bool LineNumbers { get; set; } = true;
    public bool SmoothScrolling { get; set; } = true;
    public bool RenderWhitespace { get; set; } = false;

    public int FontSize { get; set; } = 14;
    public string? FontFamily { get; set; }
    public double? LineHeight { get; set; }

    public bool EnableFrontMatter { get; set; } = false;
}

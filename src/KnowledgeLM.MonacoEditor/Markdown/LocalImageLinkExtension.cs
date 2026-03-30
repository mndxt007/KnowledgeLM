using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace KnowledgeLM.MonacoEditor.Markdown;

/// <summary>
/// Markdig extension that rewrites relative image URLs to routed paths
/// that can be intercepted by BlazorWebView's WebResourceRequested handler.
/// </summary>
public class LocalImageLinkExtension : IMarkdownExtension
{
    private readonly string _baseDirectory;

    public LocalImageLinkExtension(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        _baseDirectory = baseDirectory;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.DocumentProcessed += RewriteImageUrls;
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) { }

    private void RewriteImageUrls(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!link.IsImage || string.IsNullOrEmpty(link.Url))
                continue;

            // Skip absolute URLs (http://, https://, etc.) and already-rooted paths
            if (Uri.IsWellFormedUriString(link.Url, UriKind.Absolute) || link.Url.StartsWith("/"))
                continue;

            var fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, link.Url));
            link.Url = $"/LearnLoomResource?fullpath={Uri.EscapeDataString(fullPath)}";
        }
    }
}

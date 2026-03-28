# Implementation Plan: Markdig Extension for Local Image Serving

## Problem Statement

Markdown files contain **relative image paths** (e.g., `![diagram](Images/arch.png)`). When rendered in the BlazorWebView preview panel, the browser resolves these relative to the WebView's base URL (`https://0.0.0.0/`), not the markdown file's directory on disk. Images fail to load.

**Goal:** Rewrite image URLs at parse time via a custom Markdig extension so that `BlazorWebView_WebResourceRequested` can intercept and serve them from the local file system.

**Key constraint:** Markdown files must remain compatible with other standard markdown readers — images use relative file paths in source. Only the rendered preview HTML gets rewritten.

## Current Architecture

```
MarkDownView.razor.cs
  → ExtractContent(filePath) reads raw markdown from disk via ResourceResponse.Location
  → Sets MarkDownContent (string)

MonacoMarkdownEditor.razor.cs
  → Left panel: Monaco editor (edits raw markdown)
  → Right panel: CreateMarkupString() converts MD→HTML via Markdig + HtmlSanitizer
  → @Html renders the preview

MainPage.xaml.cs
  → WebResourceRequested intercepts /Notes/* requests
  → Maps to local disk, serves with correct MIME type via ManagedStream
```

**Key files:**
- `src/KnowledgeLM.App/Components/Widgets/MarkDownView.razor.cs` — loads markdown
- `src/KnowledgeLM.MonacoEditor/Components/MonacoMarkdownEditor.razor.cs` — renders preview
- `src/KnowledgeLM.App/MainPage.xaml.cs` — serves local files
- `src/KnowledgeLM.MonacoEditor/Markdown/MarkdownPipelineFactory.cs` — builds Markdig pipeline

## Chosen Approach: Markdig Custom Extension (Option B)

Create a custom `IMarkdownExtension` that hooks into Markdig's `DocumentProcessed` event, walks the AST, and rewrites `LinkInline` nodes (where `IsImage=true`) from relative paths to `/LearnLoomResource?fullpath=<encoded-absolute-path>`.

**Why this approach:**
- Cleanest integration with Markdig pipeline — operates at the AST level
- Handles all image syntax variants (including reference-style links)
- No fragile HTML post-processing (regex on HTML)
- Raw markdown stays portable — rewriting only affects the rendered output

**Key design decision:** The pipeline is currently cached as a `static` field. Since `BaseDirectory` changes per document, we build a fresh pipeline with the extension when `BaseDirectory` is set. When not set, the cached default pipeline is used unchanged. Building a Markdig pipeline is lightweight, and this pattern already exists in the codebase.

---

## Implementation Todos

### 1. Create `LocalImageLinkExtension`

**New file:** `src/KnowledgeLM.MonacoEditor/Markdown/LocalImageLinkExtension.cs`

A proper `IMarkdownExtension` that:
- Takes `baseDirectory` (the markdown file's parent directory) in its constructor
- In `Setup(MarkdownPipelineBuilder)`, registers a `DocumentProcessed` handler
- The handler walks `document.Descendants<LinkInline>()` for images with relative URLs
- Resolves each relative path against `baseDirectory` using `Path.GetFullPath(Path.Combine(...))`
- Rewrites URL to `/LearnLoomResource?fullpath={Uri.EscapeDataString(fullPath)}`
- Skips absolute URLs (`Uri.IsWellFormedUriString`) and already-rooted paths (`StartsWith("/")`)

```csharp
public class LocalImageLinkExtension : IMarkdownExtension
{
    private readonly string _baseDirectory;

    public LocalImageLinkExtension(string baseDirectory)
    {
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
            if (Uri.IsWellFormedUriString(link.Url, UriKind.Absolute) || link.Url.StartsWith("/"))
                continue;

            var fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, link.Url));
            link.Url = $"/LearnLoomResource?fullpath={Uri.EscapeDataString(fullPath)}";
        }
    }
}
```

### 2. Add `BaseDirectory` parameter to `MonacoMarkdownEditor`

**Edit:** `src/KnowledgeLM.MonacoEditor/Components/MonacoMarkdownEditor.razor.cs`

- Add `[Parameter] public string? BaseDirectory { get; set; }`
- Modify `GetMarkdownPipeline()`: when `BaseDirectory` is set, build a fresh pipeline with legacy defaults + `LocalImageLinkExtension`
- When `BaseDirectory` is null/empty, use existing cached pipeline (no behavior change)
- When both `MarkdownOptions` and `BaseDirectory` are set, use factory with extension

### 3. Update `MarkdownPipelineFactory`

**Edit:** `src/KnowledgeLM.MonacoEditor/Markdown/MarkdownPipelineFactory.cs`

Add optional `baseDirectory` parameter to `BuildPipeline()` so the extension is included when both `MarkdownOptions` and `BaseDirectory` are provided.

### 4. Pass `ResourceResponse.Location` from `MarkDownView`

**Edit:** `src/KnowledgeLM.App/Components/Widgets/MarkDownView.razor`

```razor
<MonacoMarkdownEditor Markdown="@MarkDownContent"
                      BaseDirectory="@(System.IO.Path.GetDirectoryName(ResourceResponse?.Location))"
                      MarkdownChanged="@OnChanged" />
```

### 5. Handle `/LearnLoomResource` in `MainPage.xaml.cs`

**Edit:** `src/KnowledgeLM.App/MainPage.xaml.cs`

- Check for `/LearnLoomResource` path prefix
- Parse & URL-decode the `fullpath` query parameter
- Serve the file with correct MIME type using existing `ManagedStream` pattern
- Extend MIME types for `.gif`, `.svg`, `.webp`, `.bmp`

### 6. Verify HtmlSanitizer allows rewritten URLs

HtmlSanitizer's default config allows relative URLs. Rewritten URLs (`/LearnLoomResource?fullpath=...`) are relative paths so should pass through. Verify and add config if needed.

---

## Key Considerations

- **Editor vs Preview:** Only preview HTML gets rewritten. Monaco editor shows original relative paths — saves remain portable.
- **URL Encoding:** `Uri.EscapeDataString()` handles Windows backslashes, spaces, and Unicode.
- **HtmlSanitizer:** Rewrite happens during Markdig parsing (before sanitization in `CreateMarkupString`). Relative URLs pass through by default.
- **Performance:** Fresh pipeline per render is negligible cost (~microseconds).
- **Pipeline caching:** When `BaseDirectory` is set, pipeline is not cached. When not set, existing caching behavior is preserved.

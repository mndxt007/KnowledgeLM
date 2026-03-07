# Markdown Viewer & Editor Options for MAUI Hybrid App

## Context

KnowledgeLM is a .NET MAUI Blazor Hybrid app that needs a markdown editor/viewer for learning notes. The current implementation uses `PSC.Blazor.Components.MarkdownEditor` (v8.0.8), which wraps EasyMDE (JavaScript) and uses Markdig for server-side rendering. The app faces a **fundamental constraint**: WebView2 blocks `file:///` URLs from an HTTPS origin, meaning local images referenced in markdown (e.g., `![](C:\Users\...\image.jpg)`) cannot load. See `docs/local-content-loading-plan.md` for the detailed local file loading analysis.

**Key requirements:**
1. Markdown files must remain compatible with standard readers (VS Code, Obsidian, GitHub, etc.)
2. Images use local file paths (relative or absolute), not HTTP URLs
3. Editor should support live preview
4. Must work in MAUI Blazor Hybrid (WebView2 on Windows, WKWebView on Mac/iOS)

---

## Current State

- **Component:** `PSC.Blazor.Components.MarkdownEditor` v8.0.8
- **Location:** `Components/Widgets/MarkdownView.razor`
- **Rendering:** EasyMDE handles editing in the browser; Markdig handles server-side MD→HTML
- **Problem:** No control over how EasyMDE renders image previews (it uses browser-native `<img src="file:///...">`), which gets blocked before `WebResourceRequested` fires

The `implementation-plan.md` already targets **Markdig (parsing) + CodeMirror/Monaco (editor)** — this document evaluates options to get there.

---

## Option Comparison

### Option A: Keep PSC.Blazor.Components.MarkdownEditor + Post-Process HTML

**Approach:** Stay with current component but intercept the rendered HTML to rewrite image sources.

**How it works:**
- Use `ValueHTMLChanged` callback to intercept rendered HTML
- Replace `<img src="file:///...">` or `<img src="C:\...">` with virtual paths
- Serve images via `CreateFileProvider` or `WebResourceRequested` (per local-content-loading-plan)
- EasyMDE's live preview will still show broken images in the editor pane

**Pros:**
- Zero migration effort — keeps current component
- HTML post-processing is straightforward

**Cons:**
- **Editor preview broken** — EasyMDE renders its own preview; we can only fix the final HTML output, not the editor's live preview
- Limited customization of the rendering pipeline
- EasyMDE is a JavaScript library — no direct C# control over its internals
- Package not actively maintained (last release targets .NET 8)
- No Markdig pipeline access — HTML comes pre-rendered from JS

**Verdict:** Stopgap only. Does not solve the preview problem.

---

### Option B: Markdig + Custom Blazor Viewer (Read-Only) + Separate Editor

**Approach:** Use Markdig directly in C# for rendering with a custom `LinkInlineRenderer` to rewrite image URLs. Pair with a plain `<textarea>` or basic code input for editing.

**How it works:**
- Parse markdown with `Markdig.Markdown.Parse()` to get AST
- Walk AST to rewrite `LinkInline.Url` for images before rendering
- Render to HTML with `Markdig.Markdown.ToHtml()` using custom pipeline
- Display via Blazor `MarkupString`
- Editing in a simple `<textarea>` with two-way binding

**Pros:**
- **Full control over image URL rewriting** at the AST level — the core problem is solved cleanly
- Markdig pipeline is highly extensible (custom extensions, renderers)
- Can use `MarkdigExtensions.UrlRewriter` NuGet for simple cases
- No JavaScript dependency for rendering
- CommonMark compliant, well-maintained library

**Cons:**
- No syntax highlighting in the editor (just a textarea)
- No toolbar, no keyboard shortcuts, no split-pane preview
- Must build the viewer UI yourself (but it's just rendering HTML)

**Verdict:** Excellent for viewer. Editor UX too basic for a note-taking app.

---

### Option C: BlazorMonaco (Monaco Editor) + Markdig Viewer

**Approach:** Use Monaco Editor (via BlazorMonaco NuGet) for editing with markdown language mode, and Markdig with custom image renderer for the preview pane.

**How it works:**
- `BlazorMonaco` provides `<StandaloneCodeEditor>` Blazor component
- Set language to `"markdown"` for syntax highlighting
- On content change, pipe markdown through custom Markdig pipeline
- Custom `LinkInlineRenderer` rewrites local paths to virtual paths
- Display rendered HTML in a preview pane via `MarkupString`
- Serve images via `CreateFileProvider` or `WebResourceRequested`

**Pros:**
- VS Code-quality editing experience (syntax highlighting, search, minimap, etc.)
- Full control over rendering pipeline via Markdig
- **Image URL rewriting happens at the C# AST level** — clean solution
- Monaco is actively maintained by Microsoft
- BlazorMonaco supports .NET 9+ and MAUI Hybrid
- Aligns with `implementation-plan.md` tech stack target

**Cons:**
- No built-in toolbar (bold, italic, etc.) — must build or use keybindings
- Monaco is a heavy JS bundle (~2MB) — acceptable for desktop, but large
- Preview pane must be built separately (split view layout)
- Two-component architecture (editor + viewer) vs. single integrated component

**Verdict:** Strong candidate. Best editor UX, full rendering control.

---

### Option D: FluentUI.Blazor.Monaco.EditorPack

**Approach:** An integrated package that wraps Monaco + Markdig with FluentUI theming, providing editor + live preview out of the box.

**How it works:**
- Install `FluentUI.Blazor.Monaco.EditorPack` NuGet
- Provides `<MarkdownEditor>` component with built-in split editor/preview
- Uses Monaco for editing, Markdig for rendering
- Already styled with FluentUI (matches app's existing FluentUI components)
- Can extend the Markdig pipeline for custom image handling

**Pros:**
- **Integrated editor + preview** — no need to build split-pane layout
- **FluentUI-styled** — matches existing app design system
- Uses Monaco + Markdig under the hood
- Toolbar, undo/redo, theme support included
- Customizable Markdig pipeline for image URL rewriting

**Cons:**
- Newer package, smaller community — potential stability concerns
- May need to verify MAUI Hybrid compatibility (designed for Blazor Server/WASM)
- Less control than building your own Monaco + Markdig combo
- Dependency on package maintainer for updates

**Verdict:** Best balance of features and effort if MAUI Hybrid compatible.

---

### Option E: Syncfusion Blazor Rich Text Editor (Markdown Mode)

**Approach:** Commercial component with native markdown editing mode.

**How it works:**
- Syncfusion's Rich Text Editor has a markdown mode
- Supports MAUI Hybrid officially
- Handles image insertion, toolbar, formatting
- Can customize image handling via event callbacks

**Pros:**
- Production-ready, enterprise-grade
- Full-featured toolbar, media embedding, export
- Official MAUI Hybrid support
- Documentation and support

**Cons:**
- **Commercial license required** (free community license for individuals/small companies)
- Heavy dependency on Syncfusion ecosystem
- Less transparent rendering pipeline — harder to customize image URL rewriting
- Vendor lock-in

**Verdict:** Good option if licensing is acceptable and custom rendering control isn't critical.

---

## Local Image Loading Strategy (Cross-Cutting)

Regardless of which editor/viewer option is chosen, local images must be served to the WebView. The strategy has two layers:

### Layer 1: Image URL Rewriting (at Markdown/HTML level)

**For options using Markdig (B, C, D):**
```csharp
// Custom Markdig extension to rewrite local image paths
var document = Markdown.Parse(markdown, pipeline);
foreach (var link in document.Descendants<LinkInline>())
{
    if (link.IsImage && IsLocalPath(link.Url))
    {
        // C:\Users\x\Downloads\img.jpg → /_localcontent/C/Users/x/Downloads/img.jpg
        link.Url = ToVirtualPath(link.Url);
    }
}
var html = document.ToHtml(pipeline);
```

**For EasyMDE-based options (A):**
- Post-process HTML string with regex to replace `src="file:///..."` patterns
- Less reliable, can't fix EasyMDE's own preview pane

### Layer 2: File Serving (at WebView level)

Two approaches (detailed in `local-content-loading-plan.md`):

1. **Custom FileProvider** — Override `CreateFileProvider` to expose local directories
2. **WebResourceRequested** — Intercept virtual path requests and serve local files

---

## Recommendation

### Primary: Option C (BlazorMonaco + Markdig) or Option D (FluentUI.Blazor.Monaco.EditorPack)

**Evaluate Option D first** — if it works well in MAUI Hybrid and allows custom Markdig pipeline injection, it provides the best effort-to-value ratio:
- Integrated editor + preview
- FluentUI theming (matches app)
- Monaco-quality editing
- Customizable Markdig rendering for image URL rewriting

**Fall back to Option C** if Option D has MAUI Hybrid issues or insufficient pipeline customization. Option C requires building the split-pane layout, but gives maximum control.

### Migration Path

```
Current State (PSC.MarkdownEditor/EasyMDE)
    │
    ├─► Quick Fix: Post-process HTML (Option A) — immediate partial fix
    │
    └─► Target: Monaco + Markdig (Option C or D)
            │
            ├── Step 1: Add Markdig to project, build custom image renderer
            ├── Step 2: Build read-only markdown viewer with Markdig pipeline
            ├── Step 3: Integrate Monaco editor (via BlazorMonaco or EditorPack)
            ├── Step 4: Wire up local file serving (CreateFileProvider or WebResourceRequested)
            └── Step 5: Remove PSC.Blazor.Components.MarkdownEditor dependency
```

### Implementation Todos

1. **eval-fluentui-monaco-pack** — Install and evaluate `FluentUI.Blazor.Monaco.EditorPack` in MAUI Hybrid: verify it loads, renders, and supports custom Markdig pipeline injection
2. **eval-blazormonaco** — If Option D doesn't work, evaluate `BlazorMonaco` standalone: install, verify markdown mode works in MAUI Hybrid
3. **build-markdig-image-rewriter** — Create a custom Markdig extension (`LocalImageRewriterExtension`) that rewrites local file paths to virtual paths in `LinkInline` nodes
4. **build-markdown-viewer** — Create a Blazor component that renders markdown to HTML using custom Markdig pipeline and displays via `MarkupString`
5. **build-split-pane-editor** — (Only if Option C) Build a split-pane layout with Monaco editor on left and Markdig viewer on right
6. **wire-local-file-serving** — Implement either `CreateFileProvider` override or `WebResourceRequested` handler to serve local images via virtual paths
7. **migrate-markdownview** — Replace `MarkdownView.razor` with new component, update `Home.razor`, remove PSC.Blazor.Components.MarkdownEditor dependency
8. **test-local-images** — Verify local images load correctly in both editor preview and viewer for Windows target

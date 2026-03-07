# Plan: Local Content Loading for MarkdownViewer in MAUI Hybrid App

## Problem Statement

The MarkdownViewer (`MarkdownView.razor`) uses `PSC.Blazor.Components.MarkdownEditor` inside a MAUI BlazorWebView. When markdown references local images (e.g., `![](C:\Users\manojdixit\Downloads\image.jpg)`), the underlying WebView2 engine blocks `file:///` URLs at the browser security level **before** the `WebResourceRequested` event handler fires. This means the existing handler in `MainPage.xaml.cs` never gets a chance to intercept these requests.

**Key constraint:** Markdown files must remain compatible with other standard markdown readers — so images should use local file paths (relative or absolute), not HTTP-based or custom-scheme URLs in the source files.

## Root Cause Analysis

1. BlazorWebView hosts content at `https://0.0.0.1/` (internal origin)
2. WebView2's security model prohibits navigation from an HTTPS origin to `file:///` URLs
3. The block happens at the browser engine level, before `WebResourceRequested` fires
4. The current handler in `MainPage.xaml.cs` is correct in concept but never receives these requests

## Options Analysis

### Option 1: Custom FileProvider (via `CreateFileProvider` override)
**Approach:** Subclass `BlazorWebView`, override `CreateFileProvider` to register local directories as static content roots. Pre-process markdown at render time to rewrite absolute local paths to relative paths the file provider can resolve.

**How it works:**
- Create `CustomBlazorWebView : BlazorWebView` that overrides `CreateFileProvider`
- Use `PhysicalFileProvider` + `CompositeFileProvider` to expose local directories
- At render time, transform `C:\Users\manojdixit\Downloads\image.jpg` → `_local/Users/manojdixit/Downloads/image.jpg`
- The file provider serves the file from the real filesystem path

**Pros:**
- Official MAUI API, well-supported
- Efficient streaming (no base64 overhead)
- Works naturally with BlazorWebView's content pipeline
- Original markdown files stay untouched on disk

**Cons:**
- Must pre-register root directories (e.g., drive roots or known folders)
- Path rewriting logic needed in the rendering pipeline
- Security consideration: exposes filesystem to WebView

**Files to change:**
- New: `CustomBlazorWebView.cs`
- Edit: `MainPage.xaml` (use custom control)
- Edit: `MarkdownView.razor` (add path transformation before rendering)

---

### Option 2: WebResourceRequested with Path Rewriting
**Approach:** Pre-process markdown to rewrite local `file:///` paths to a custom URL pattern (e.g., `https://0.0.0.1/_localfile/C/Users/.../image.jpg`). The `WebResourceRequested` handler intercepts this pattern and serves the local file.

**How it works:**
- At render time, transform image paths: `C:\path\image.jpg` → `/_localfile/C/path/image.jpg`
- In `WebResourceRequested`, detect `/_localfile/` prefix, reconstruct the real path, serve the file
- Markdown source files remain untouched

**Pros:**
- Uses existing `WebResourceRequested` infrastructure already in `MainPage.xaml.cs`
- More explicit control over which files are served
- Can add access control/validation logic

**Cons:**
- URL rewriting at the markdown level adds complexity
- Must handle the `WebViewWebResourceRequestedEventArgs` correctly (synchronous requirement)
- The `WebResourceRequested` event only fires for requests to the BlazorWebView's origin host

**Files to change:**
- Edit: `MainPage.xaml.cs` (modify handler to detect `_localfile` prefix)
- Edit: `MarkdownView.razor` (add path transformation)

---

### Option 3: Base64 Data URL Injection at Render Time
**Approach:** Pre-process markdown before passing to the editor — detect local image paths, read files, convert to `data:image/...;base64,...` URLs inline.

**How it works:**
- Regex scan markdown for `![...](local-path)` patterns
- Read each referenced file, encode as base64
- Replace the path with a data URI in the markdown string
- Pass transformed markdown to `MarkdownEditor`

**Pros:**
- Simplest implementation, no infrastructure changes
- Works on all platforms immediately
- No security concerns (files read through C# with normal file permissions)
- Original markdown files remain untouched

**Cons:**
- **Memory-intensive** for large images (base64 is ~33% larger than binary)
- Slow for many/large images
- Not suitable for video or large assets
- MarkdownEditor may struggle with very large value strings

**Files to change:**
- Edit: `MarkdownView.razor` (add preprocessing logic)
- Potentially new: `Helpers/MarkdownImageProcessor.cs`

---

### Option 4: JavaScript Interop with Blob URLs
**Approach:** After markdown renders to HTML, use JS interop to find `<img>` elements with failed `file:///` sources, read the files via C# interop, create Blob URLs in JavaScript, and update the image sources.

**How it works:**
- Let markdown render normally (images will fail to load)
- JS mutation observer detects broken images
- JS calls back to C# to read the file bytes
- C# returns bytes, JS creates a Blob URL, updates `<img src>`

**Pros:**
- Efficient memory use (Blob URLs are managed by the browser)
- No markdown preprocessing needed
- Works with any markdown rendering library

**Cons:**
- Complex JS ↔ C# interop
- Images briefly show as broken before loading
- Requires custom JavaScript in the page
- More moving parts, harder to debug

**Files to change:**
- New: `wwwroot/js/localImageLoader.js`
- Edit: `wwwroot/index.html` (include script)
- Edit: `MarkdownView.razor` (add JS interop calls)

---

### Option 5: Hybrid — Custom FileProvider + Markdown Path Normalization Service
**Approach:** Combine Option 1 with a dedicated service that manages path normalization. The service understands the local filesystem, normalizes paths, and the FileProvider serves content.

**How it works:**
- `LocalContentService` manages mappings of local directories to virtual paths
- Custom `BlazorWebView` exposes those directories via `CreateFileProvider`
- `MarkdownRenderService` normalizes image paths in markdown before rendering
- Paths like `C:\Users\x\Downloads\img.jpg` → `/local-content/downloads/img.jpg`
- FileProvider maps `/local-content/downloads/` → `C:\Users\x\Downloads\`

**Pros:**
- Clean separation of concerns
- Reusable across multiple components
- Efficient file serving
- Extensible for future needs (video, PDF, etc.)

**Cons:**
- Most complex to implement
- Requires user to configure/approve directory access

**Files to change:**
- New: `CustomBlazorWebView.cs`
- New: `Services/LocalContentService.cs`
- New: `Helpers/MarkdownPathNormalizer.cs`
- Edit: `MainPage.xaml`
- Edit: `MarkdownView.razor`
- Edit: `MauiProgram.cs` (register service)

---

## Recommendation

**Option 2 (WebResourceRequested with Path Rewriting)** is recommended as the starting point for a PoC because:

1. It builds on the existing `WebResourceRequested` handler already in `MainPage.xaml.cs`
2. It requires minimal new infrastructure
3. It keeps image serving explicit and auditable
4. The markdown source files remain untouched — transformation happens only at render time
5. The path rewriting in the markdown string is straightforward (regex replacement)

If Option 2 encounters issues (e.g., `WebResourceRequested` doesn't fire for internal-origin URLs), **Option 1 (Custom FileProvider)** is the fallback — it uses the official MAUI API and is well-documented.

**Option 3 (Base64)** is a quick-and-dirty fallback for immediate testing but should not be the long-term solution.

## Implementation Todos

1. **research-webresourcerequested-scope** — Verify whether `WebResourceRequested` fires for requests to paths under the BlazorWebView's own origin (e.g., `https://0.0.0.1/_localfile/...`). This determines if Option 2 is viable.
2. **create-markdown-path-rewriter** — Build a helper that transforms local file paths in markdown to virtual paths (`/_localfile/...` or relative paths depending on chosen option)
3. **update-webresource-handler** — Update `MainPage.xaml.cs` handler to detect and serve `_localfile` requests (Option 2) OR create `CustomBlazorWebView` with FileProvider (Option 1)
4. **integrate-with-markdownview** — Wire up the path rewriting in `MarkdownView.razor` so markdown is pre-processed before passing to `MarkdownEditor`
5. **test-cross-platform** — Verify the solution works on Windows (primary target) and note any platform-specific considerations
6. **fallback-base64-poc** — (Optional) Implement base64 fallback as a quick proof-of-concept for immediate testing

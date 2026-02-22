# KnowledgeLM — Architecture

## System Architecture — Agent-First Design

The core architecture is **agent-first**: the GitHub Copilot CLI SDK agent is the orchestrator. Playwright is exposed as a **skill/tool** that the agent invokes for browser automation, screenshots, and content extraction. Custom code is only written for capabilities Playwright cannot provide.

### Agent ↔ Skills Model

```
┌─────────────────────────────────────────────────────────────┐
│                   .NET MAUI Blazor Shell                    │
│  ┌───────────┐  ┌──────────┐  ┌──────────────────────────┐  │
│  │ Notebook  │  │ Markdown │  │  Chat / Quiz / Podcast   │  │
│  │ Manager   │  │ Editor   │  │  UI                      │  │
│  └─────┬─────┘  └────┬─────┘  └────────┬─────────────────┘  │
│        └──────────────┴─────────────────┘                   │
│                         │                                   │
│  ┌──────────────────────┴──────────────────────────────┐    │
│  │         Copilot SDK Agent (Orchestrator)             │    │
│  │                                                      │    │
│  │  The agent reasons, plans, and invokes skills/tools  │    │
│  │  to accomplish learning tasks autonomously.          │    │
│  │                                                      │    │
│  │  ┌─────────────────────────────────────────────┐     │    │
│  │  │              Registered Skills              │     │    │
│  │  │                                             │     │    │
│  │  │  ┌─────────────────────────────────────┐    │     │    │
│  │  │  │  🌐 Playwright Skill (Browser)      │    │     │    │
│  │  │  │  - NavigateToUrl(url)               │    │     │    │
│  │  │  │  - TakeScreenshot()                 │    │     │    │
│  │  │  │  - ExtractPageContent()             │    │     │    │
│  │  │  │  - ClickElement(selector)           │    │     │    │
│  │  │  │  - ScrollPage()                     │    │     │    │
│  │  │  │  - GetVideoTranscript()             │    │     │    │
│  │  │  │  - WaitForContent(selector)         │    │     │    │
│  │  │  │  - AuthenticateWithSSO()            │    │     │    │
│  │  │  └─────────────────────────────────────┘    │     │    │
│  │  │                                             │     │    │
│  │  │  ┌─────────────────────────────────────┐    │     │    │
│  │  │  │  📝 Note Management Skill           │    │     │    │
│  │  │  │  - CreateNote(notebook, title, md)  │    │     │    │
│  │  │  │  - AppendToNote(noteId, content)    │    │     │    │
│  │  │  │  - ReadNote(noteId)                 │    │     │    │
│  │  │  │  - SaveScreenshot(bytes, name)      │    │     │    │
│  │  │  └─────────────────────────────────────┘    │     │    │
│  │  │                                             │     │    │
│  │  │  ┌─────────────────────────────────────┐    │     │    │
│  │  │  │  📄 File Processing Skill (Custom)  │    │     │    │
│  │  │  │  - ExtractPdfText(filePath)         │    │     │    │
│  │  │  │  - ParseYouTubeTranscript(url)      │    │     │    │
│  │  │  │  - ReadTextFile(filePath)           │    │     │    │
│  │  │  └─────────────────────────────────────┘    │     │    │
│  │  │                                             │     │    │
│  │  │  ┌─────────────────────────────────────┐    │     │    │
│  │  │  │  🔍 Screenshot Analysis Skill       │    │     │    │
│  │  │  │  - AnalyzeScreenshot(imagePath)     │    │     │    │
│  │  │  │  - DetectSlideChange(img1, img2)    │    │     │    │
│  │  │  │  - ExtractTextFromImage(imagePath)  │    │     │    │
│  │  │  └─────────────────────────────────────┘    │     │    │
│  │  │                                             │     │    │
│  │  │  ┌─────────────────────────────────────┐    │     │    │
│  │  │  │  💬 Learning Interaction Skill      │    │     │    │
│  │  │  │  - ChatWithNotes(question, nbId)    │    │     │    │
│  │  │  │  - GenerateQuiz(nbId, options)      │    │     │    │
│  │  │  │  - GeneratePodcast(nbId)            │    │     │    │
│  │  │  └─────────────────────────────────────┘    │     │    │
│  │  └─────────────────────────────────────────────┘     │    │
│  └──────────────────────────────────────────────────────┘    │
│                         │                                    │
│  ┌──────────────────────┴──────────────────────────────┐     │
│  │                  Storage Layer                       │     │
│  │  SQLite (metadata) │ File System (.md + media)       │     │
│  └──────────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────────┘
```

### How It Works

1. **User adds a source** (URL, file) to a notebook
2. **Copilot SDK agent** reasons about the source type and plans the extraction
3. **Agent invokes Playwright skill** to navigate, authenticate, screenshot, and extract content
4. **Agent uses its own intelligence** to structure extracted content into Markdown notes
5. **Agent invokes Note Management skill** to save notes and screenshots
6. **For capabilities Playwright can't handle** (PDF parsing, YouTube API, image analysis), the agent invokes custom skills

### Agent-First Principle
> **Playwright is a tool the agent uses, not a layer the app calls.**
> The agent decides *when* to navigate, *what* to screenshot, *how* to structure notes.
> Custom code is only written when Playwright + agent reasoning isn't sufficient.

---

## Core Domain Models

```csharp
// Notebook — a collection of notes from various sources
public class Notebook
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Source> Sources { get; set; }
    public List<Note> Notes { get; set; }
}

// Source — a content origin (YouTube video, PDF, LinkedIn course, etc.)
public class Source
{
    public Guid Id { get; set; }
    public Guid NotebookId { get; set; }
    public SourceType Type { get; set; }
    public string Uri { get; set; }           // URL or file path
    public string Title { get; set; }
    public string Metadata { get; set; }       // JSON — duration, author, etc.
    public SourceStatus Status { get; set; }
    public DateTime AddedAt { get; set; }
}

public enum SourceType
{
    YouTube,
    Pdf,
    TextFile,
    LinkedInLearning,
    Pluralsight,
    SharePoint,
    MicrosoftLearn,
    WebPage
}

public enum SourceStatus
{
    Pending,
    Extracting,
    Extracted,
    NoteGenerated,
    Failed
}

// Note — a generated markdown note linked to a source
public class Note
{
    public Guid Id { get; set; }
    public Guid NotebookId { get; set; }
    public Guid? SourceId { get; set; }
    public string Title { get; set; }
    public string FilePath { get; set; }       // Relative path to .md file
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// CaptureMode — how notes are generated
public enum CaptureMode
{
    ReadAlong,      // Real-time while learner browses
    SitBack         // Autonomous navigation + capture
}
```

---

## Agent Skills (Tools) — Copilot SDK Function Registration

The agent's capabilities come from **skills registered as AIFunction tools** with the Copilot SDK. The agent decides when and how to invoke them.

### Playwright Skill (Browser Automation)

The Playwright skill gives the agent full browser control. The agent uses it for navigation, authentication, content extraction, and screenshots.

```csharp
// Registered as AIFunction tools with the Copilot SDK agent
public class PlaywrightSkill
{
    private readonly IPage _page;

    [AIFunction("Navigate to a URL in the browser")]
    public async Task<string> NavigateToUrl(string url)
    {
        await _page.GotoAsync(url);
        return $"Navigated to {url}. Title: {await _page.TitleAsync()}";
    }

    [AIFunction("Take a screenshot of the current page or element")]
    public async Task<string> TakeScreenshot(string? selector = null, string? savePath = null)
    {
        var bytes = selector != null
            ? await _page.Locator(selector).ScreenshotAsync()
            : await _page.ScreenshotAsync(new() { FullPage = true });
        var path = savePath ?? $"screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.png";
        await File.WriteAllBytesAsync(path, bytes);
        return $"Screenshot saved to {path}";
    }

    [AIFunction("Extract text content from the current page")]
    public async Task<string> ExtractPageContent(string? selector = null)
    {
        return selector != null
            ? await _page.Locator(selector).InnerTextAsync()
            : await _page.InnerTextAsync("body");
    }

    [AIFunction("Click an element on the page")]
    public async Task<string> ClickElement(string selector)
    {
        await _page.Locator(selector).ClickAsync();
        return $"Clicked {selector}";
    }

    [AIFunction("Scroll the page in a direction")]
    public async Task<string> ScrollPage(string direction = "down", int pixels = 500)
    {
        var delta = direction == "up" ? -pixels : pixels;
        await _page.Mouse.WheelAsync(0, delta);
        return $"Scrolled {direction} {pixels}px";
    }

    [AIFunction("Wait for an element to appear on the page")]
    public async Task<string> WaitForContent(string selector, int timeoutMs = 10000)
    {
        await _page.Locator(selector).WaitForAsync(new() { Timeout = timeoutMs });
        return $"Element {selector} is now visible";
    }

    [AIFunction("Get all links on the current page, optionally filtered by selector")]
    public async Task<string> GetPageLinks(string? containerSelector = null)
    {
        var container = containerSelector != null
            ? _page.Locator(containerSelector)
            : _page.Locator("body");
        var links = await container.Locator("a[href]").AllAsync();
        var results = new List<string>();
        foreach (var link in links)
        {
            var href = await link.GetAttributeAsync("href");
            var text = await link.InnerTextAsync();
            results.Add($"{text.Trim()} -> {href}");
        }
        return string.Join("\n", results);
    }

    [AIFunction("Authenticate via SSO using stored browser context")]
    public async Task<string> AuthenticateWithSSO(string loginUrl)
    {
        await _page.GotoAsync(loginUrl);
        // Agent can further interact with login form via ClickElement/TypeText
        return $"Navigated to login page: {loginUrl}";
    }

    [AIFunction("Type text into an input field")]
    public async Task<string> TypeText(string selector, string text)
    {
        await _page.Locator(selector).FillAsync(text);
        return $"Typed into {selector}";
    }
}
```

### Note Management Skill

```csharp
public class NoteManagementSkill
{
    private readonly IFileSystemNoteStore _store;

    [AIFunction("Create a new markdown note in a notebook")]
    public async Task<string> CreateNote(string notebookId, string title, string markdownContent)
    {
        var path = await _store.CreateNoteAsync(Guid.Parse(notebookId), title, markdownContent);
        return $"Note created: {path}";
    }

    [AIFunction("Append content to an existing note")]
    public async Task<string> AppendToNote(string noteId, string markdownContent)
    {
        await _store.AppendAsync(Guid.Parse(noteId), markdownContent);
        return $"Appended to note {noteId}";
    }

    [AIFunction("Read the content of a note")]
    public async Task<string> ReadNote(string noteId)
    {
        return await _store.ReadAsync(Guid.Parse(noteId));
    }

    [AIFunction("Save a screenshot image to a notebook's media folder")]
    public async Task<string> SaveScreenshot(string notebookId, string imagePath, string description)
    {
        var savedPath = await _store.SaveMediaAsync(Guid.Parse(notebookId), imagePath, description);
        return $"Screenshot saved: {savedPath}. Markdown ref: ![{description}]({savedPath})";
    }
}
```

### File Processing Skill (Custom — for what Playwright can't do)

```csharp
public class FileProcessingSkill
{
    [AIFunction("Extract text from a PDF file, preserving structure")]
    public async Task<string> ExtractPdfText(string filePath)
    {
        // Uses PdfPig or iText7 — Playwright can't parse PDFs
        using var document = PdfDocument.Open(filePath);
        var text = string.Join("\n\n", document.GetPages().Select(p => p.Text));
        return text;
    }

    [AIFunction("Get transcript from a YouTube video URL")]
    public async Task<string> ParseYouTubeTranscript(string youtubeUrl)
    {
        // Uses YouTube Data API — Playwright not needed for public transcripts
        var transcript = await YouTubeTranscriptApi.GetTranscriptAsync(youtubeUrl);
        return transcript;
    }

    [AIFunction("Read a text file from disk")]
    public async Task<string> ReadTextFile(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }
}
```

### Screenshot Analysis Skill

```csharp
public class ScreenshotAnalysisSkill
{
    [AIFunction("Compare two screenshots to detect if the slide/content changed")]
    public async Task<string> DetectSlideChange(string imagePath1, string imagePath2)
    {
        // Perceptual hash comparison — custom code, Playwright can't do this
        var hash1 = ComputePerceptualHash(imagePath1);
        var hash2 = ComputePerceptualHash(imagePath2);
        var distance = HammingDistance(hash1, hash2);
        var changed = distance > 10;
        return changed
            ? $"Content CHANGED (distance: {distance}). New slide detected."
            : $"Content SAME (distance: {distance}). Skip screenshot.";
    }

    [AIFunction("Extract and describe text/diagrams from a screenshot using vision AI")]
    public async Task<string> AnalyzeScreenshot(string imagePath)
    {
        // Sends to vision model — the agent's own LLM capability
        // This is handled by the Copilot SDK's multi-modal support
        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        return $"[Image at {imagePath} ready for vision analysis]";
    }
}
```

### Agent Wiring (DI Registration)

```csharp
// In MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();

    // Copilot SDK Agent setup
    builder.Services.AddCopilotAgent(options =>
    {
        options.SystemPrompt = @"You are KnowledgeLM, an AI learning companion.
            You help users create structured Markdown notes from any content source.
            Use the Playwright skill to navigate websites, take screenshots, and extract content.
            Use the Note Management skill to save your generated notes.
            Only use File Processing skill for PDFs, YouTube transcripts, or local files.
            Structure notes with clear headings, bullet points, and embedded screenshots.";
    });

    // Register skills as agent tools
    builder.Services.AddSingleton<PlaywrightSkill>();
    builder.Services.AddSingleton<NoteManagementSkill>();
    builder.Services.AddSingleton<FileProcessingSkill>();
    builder.Services.AddSingleton<ScreenshotAnalysisSkill>();

    // Register as AIFunction tools
    builder.Services.AddAgentTools<PlaywrightSkill>();
    builder.Services.AddAgentTools<NoteManagementSkill>();
    builder.Services.AddAgentTools<FileProcessingSkill>();
    builder.Services.AddAgentTools<ScreenshotAnalysisSkill>();

    // ...
}
```

### Agent Flow Example: "Sit-Back & Relax" Mode

```
User: "Create notes from this LinkedIn Learning course: https://linkedin.com/learning/azure-fundamentals"

Agent thinks: "This is a LinkedIn Learning course. I need to:
  1. Authenticate (if needed) using PlaywrightSkill.AuthenticateWithSSO
  2. Navigate to the course using PlaywrightSkill.NavigateToUrl
  3. Get the course structure using PlaywrightSkill.GetPageLinks
  4. For each module/chapter:
     a. Navigate to it
     b. Extract content with PlaywrightSkill.ExtractPageContent
     c. Take screenshots of key slides with PlaywrightSkill.TakeScreenshot
     d. Check for slide changes with ScreenshotAnalysisSkill.DetectSlideChange
  5. Synthesize all content into structured Markdown notes
  6. Save using NoteManagementSkill.CreateNote"

Agent executes tools in sequence, reasoning between each step.
```

---

## Storage Architecture

```
KnowledgeLM Data/
├── knowledgelm.db                    # SQLite database (metadata)
├── notebooks/
│   ├── {notebook-id}/
│   │   ├── notebook.json             # Notebook metadata
│   │   ├── notes/
│   │   │   ├── intro-to-azure.md     # Generated notes
│   │   │   ├── chapter-2-networking.md
│   │   │   └── ...
│   │   ├── media/
│   │   │   ├── screenshots/
│   │   │   │   ├── slide-001.png
│   │   │   │   └── ...
│   │   │   └── audio/
│   │   │       └── podcast-ep1.mp3
│   │   └── sources/
│   │       ├── uploaded.pdf
│   │       └── ...
```

---

## Note Generation Pipeline

```
┌──────────┐    ┌───────────┐    ┌─────────┐    ┌────────────┐    ┌──────────┐
│  Source   │───▶│ Connector │───▶│ Extract │───▶│ AI Agent   │───▶│ Save .md │
│  (URL/   │    │ (detect   │    │ (text,  │    │ (structure,│    │ (file +  │
│   file)  │    │  type)    │    │  slides,│    │  summarize,│    │  SQLite) │
│          │    │           │    │  audio) │    │  format)   │    │          │
└──────────┘    └───────────┘    └─────────┘    └────────────┘    └──────────┘
                                      │                │
                                      │         ┌──────┴──────┐
                                      │         │ Screenshot  │
                                      └────────▶│ Analyzer    │
                                                │ (vision AI) │
                                                └─────────────┘
```

---

## Platform-Specific Notes

### Windows
- Full feature set: scraping, capture, notes, chat, quiz, podcast
- Playwright runs natively with full browser automation
- File system storage in `%APPDATA%/KnowledgeLM/`

### Android
- Reader/consumer mode: view notes, chat, quiz, podcast
- No Playwright (browser automation not available)
- Storage in app-specific directory
- Future: sync with Windows instance via local network or shared storage

# KnowledgeLM — Architecture

> **Last updated to match codebase state as of March 2026**

---

## System Architecture — Current Implementation

KnowledgeLM is a **.NET MAUI Blazor Hybrid** desktop application for AI-powered learning and note-taking. The app uses **Microsoft Semantic Kernel** for AI plugin orchestration and **GitHub Copilot CLI** (`copilot.exe`) as the external LLM interface.

### Architecture Diagram (As Built)

```
┌──────────────────────────────────────────────────────────────────┐
│                    .NET MAUI Blazor Shell                         │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  Home.razor — FluentMultiSplitter (3-pane layout)           │  │
│  │                                                             │  │
│  │  ┌─────────────┐  ┌──────────────┐  ┌───────────────────┐  │  │
│  │  │  Notes       │  │  GithubCLI   │  │  MarkdownView     │  │  │
│  │  │  (TreeView)  │  │  (Chat UI)   │  │  (Monaco Editor)  │  │  │
│  │  │              │  │              │  │                   │  │  │
│  │  │  Course      │  │  User input  │  │  MonacoMarkdown   │  │  │
│  │  │   └─Section  │  │  Agent msgs  │  │  Editor w/        │  │  │
│  │  │      └─Res.  │  │  Tool calls  │  │  live preview     │  │  │
│  │  └──────────────┘  └──────┬───────┘  └───────────────────┘  │  │
│  └───────────────────────────┼──────────────────────────────────┘  │
│                              │                                    │
│  ┌───────────────────────────┴──────────────────────────────────┐  │
│  │              AI Integration Layer                             │  │
│  │                                                               │  │
│  │  ┌───────────────────────┐   ┌─────────────────────────────┐  │  │
│  │  │  Semantic Kernel       │   │  GitHub Copilot CLI         │  │  │
│  │  │  ┌──────────────────┐  │   │  (copilot.exe)              │  │  │
│  │  │  │ "NotesManagement"│  │   │                             │  │  │
│  │  │  │  Plugin           │  │   │  CopilotClient → Session   │  │  │
│  │  │  │  (NotesCRUDSvc)  │  │   │  - SendAsync(query)        │  │  │
│  │  │  │  13 KernelFuncs  │  │   │  - AssistantMessageEvent   │  │  │
│  │  │  └──────────────────┘  │   │  - ToolExecutionStartEvent │  │  │
│  │  └───────────────────────┘   └─────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                              │                                     │
│  ┌───────────────────────────┴──────────────────────────────────┐   │
│  │                    Data Layer                                 │   │
│  │                                                               │   │
│  │  ┌─────────────────────┐   ┌──────────────────────────────┐   │   │
│  │  │  NotesContext        │   │  File System                 │   │   │
│  │  │  (EF Core + SQLite)  │   │  Q:\Dev\Notes\              │   │   │
│  │  │                      │   │  (Markdown + images)         │   │   │
│  │  │  Tables:             │   │                              │   │   │
│  │  │  - Courses           │   │  WebResourceRequested        │   │   │
│  │  │  - Sections          │   │  intercepts /Notes/* and     │   │   │
│  │  │  - Resources         │   │  serves from local disk      │   │   │
│  │  └──────────────────────┘   └──────────────────────────────┘   │   │
│  └────────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐   │
│  │  Infrastructure (Aspire + OpenTelemetry)                       │   │
│  │  - AppHost orchestration    - HTTP resilience                  │   │
│  │  - Service discovery        - Logging / Metrics / Tracing      │   │
│  └────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

### How It Works (Current)

1. **User interacts with 3-pane layout** — Notes tree (left), Copilot chat (center), Markdown editor (right)
2. **User sends a query** to the GithubCLI chat pane
3. **GitHub Copilot CLI** receives the query via `CopilotSession.SendAsync()`, with Semantic Kernel functions exposed as tools
4. **Agent invokes `NotesCRUDService` kernel functions** (e.g., `create_course`, `create_section`, `create_resource`) to manage learning content in the database
5. **Agent can also invoke shell commands** (e.g., Playwright CLI, file system ops) as directed by the system prompt
6. **StateService broadcasts refresh events** so the Notes tree updates reactively after data changes
7. **Markdown files** on disk are served to the Monaco editor via `WebResourceRequested` interception in `MainPage.xaml.cs`

---

## Solution Structure (Actual)

```
KnowledgeLM/
├── KnowledgeLM.slnx
├── docs/
│   ├── architecture.md              # This file
│   ├── milestones.md
│   ├── implementation-plan.md        # Original vision document
│   ├── feasibility.md
│   ├── local-content-loading-plan.md
│   └── markdown-editor-viewer-options.md
│
└── src/
    ├── KnowledgeLM.App/                          # Main MAUI Blazor application
    │   ├── Components/
    │   │   ├── _Imports.razor                     # Global using statements
    │   │   ├── Routes.razor                       # SPA router config
    │   │   ├── Layout/
    │   │   │   ├── MainLayout.razor               # FluentLayout shell
    │   │   │   ├── MainLayout.razor.css
    │   │   │   ├── NavMenu.razor                  # FluentAppBar vertical nav (8 items)
    │   │   │   └── NavMenu.razor.css
    │   │   ├── Pages/
    │   │   │   ├── Home.razor                     # @page "/" — 3-pane FluentMultiSplitter
    │   │   │   └── NotFound.razor
    │   │   └── Widgets/
    │   │       ├── Notes.razor / .razor.cs         # FluentTreeView: Course → Section → Resource
    │   │       ├── GithubCLI.razor / .razor.cs / .razor.js  # Copilot chat interface
    │   │       └── MarkdownView.razor              # MonacoMarkdownEditor wrapper
    │   ├── Data/
    │   │   └── NotesContext.cs                     # EF Core DbContext (Courses, Sections, Resources)
    │   ├── Helpers/
    │   │   ├── AgentPrompts.cs                     # System prompt + Notes folder path
    │   │   └── ManagedStream.cs                    # Stream wrapper for resource disposal
    │   ├── Models/
    │   │   ├── CoPilotMessages.cs                  # Chat message model (Message, EventType)
    │   │   └── Notes/
    │   │       ├── CommonItemProperties.cs         # Abstract base: Name, CreatedAt, LastModified, Location
    │   │       ├── Course.cs                       # Id, Name, Description, Sections[]
    │   │       ├── Section.cs                      # Id, CourseId, Name, Description, Resources[]
    │   │       ├── Resource.cs                     # Id, SectionId, Name, ResourceType
    │   │       ├── ResourceType.cs                 # Enum: Image, Video, Other, Markdown
    │   │       └── DTOs/
    │   │           ├── CourseDto.cs                 # Create/Update/Response DTOs
    │   │           ├── SectionDto.cs
    │   │           ├── ResourceDto.cs
    │   │           └── CourseDetailResponse.cs      # Nested response with sections & resources
    │   ├── Services/
    │   │   ├── NotesCRUDService.cs                 # 370 lines — Semantic Kernel plugin (13 functions)
    │   │   └── StateService.cs                     # Event-based refresh (OnRefreshRequested)
    │   ├── MauiProgram.cs                          # DI registration, DB init, Kernel setup
    │   ├── MainPage.xaml / .xaml.cs                # BlazorWebView host + WebResourceRequested handler
    │   ├── App.xaml / .xaml.cs                     # MAUI Application entry
    │   ├── KnowledgeLM.App.csproj                  # .NET 10 Windows target
    │   └── wwwroot/
    │       ├── index.html                          # Blazor entry point
    │       └── app.css
    │
    ├── KnowledgeLM.AppHost/                       # Aspire orchestration
    │   └── AppHost.cs                              # AddMauiProject + AddWindowsDevice
    │
    └── KnowledgeLM.MauiServiceDefaults/           # Shared service config
        └── Extensions.cs                           # OpenTelemetry, service discovery, HTTP resilience
```

---

## Tech Stack (Actual)

| Layer | Technology | Status |
|-------|-----------|--------|
| App Framework | .NET 10 MAUI Blazor Hybrid | ✅ Implemented |
| UI Components | Microsoft FluentUI Blazor (v4.14) | ✅ Implemented |
| Markdown Editor | FluentUI.Blazor.Monaco.EditorPack (0.2.2-preview) | ✅ Implemented |
| AI Orchestration | Microsoft Semantic Kernel (v1.73) | ✅ Implemented |
| AI Agent | GitHub Copilot CLI SDK (v0.1.24) — external `copilot.exe` process | ✅ Implemented |
| AI Extensions | Microsoft.Extensions.AI (v10.3) | ✅ Implemented |
| Database | EF Core 10 + SQLite | ✅ Implemented |
| Hosting | .NET Aspire AppHost (v13.1.2) | ✅ Implemented |
| Observability | OpenTelemetry (logging, metrics, tracing) | ✅ Implemented |
| HTTP Resilience | Microsoft.Extensions.Http.Resilience | ✅ Implemented |
| Browser Automation | Playwright for .NET | ❌ Not yet implemented |
| PDF Processing | PdfPig / iText7 | ❌ Not yet implemented |
| YouTube Transcripts | YouTube Data API v3 | ❌ Not yet implemented |
| TTS (Podcast) | Azure Cognitive Services / OpenAI TTS | ❌ Not yet implemented |

---

## Core Domain Models (Actual)

```csharp
// Abstract base — shared properties for all entities
public abstract class CommonItemProperties
{
    [Required] [MaxLength(200)]
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }        // Auto-set by NotesContext
    public DateTime LastModified { get; set; }     // Auto-set by NotesContext
    [MaxLength(500)]
    public string? Location { get; set; }
}

// Course — top-level learning container
public class Course : CommonItemProperties
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [MaxLength(2000)]
    public string? Description { get; set; }
    public List<Section> Sections { get; set; } = [];
}

// Section — a module/chapter within a course
public class Section : CommonItemProperties
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int CourseId { get; set; }               // FK → Course
    [MaxLength(2000)]
    public string? Description { get; set; }
    public List<Resource> Resources { get; set; } = [];
}

// Resource — a learning artifact (markdown file, image, video reference)
public class Resource : CommonItemProperties
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int SectionId { get; set; }              // FK → Section
    public ResourceType ResourceType { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResourceType { Image, Video, Other, Markdown }
```

### Database Schema

| Table | PK | FK | Indexes | Cascade Delete |
|-------|----|----|---------|----------------|
| Courses | Id (identity) | — | IX_Courses_Name | — |
| Sections | Id (identity) | CourseId → Courses | IX_Sections_CourseId | Yes |
| Resources | Id (identity) | SectionId → Sections | IX_Resources_SectionId | Yes |

- `ResourceType` stored as string (max 50 chars)
- `CreatedAt` and `LastModified` auto-managed by `NotesContext.SaveChangesAsync()`

---

## AI Integration — Current Implementation

### Semantic Kernel Plugin: NotesCRUDService

`NotesCRUDService` is registered as a Semantic Kernel plugin named `"NotesManagement"`. It exposes 13 kernel functions that the AI agent can invoke:

| Function | Returns | Description |
|----------|---------|-------------|
| `get_all_courses` | `List<CourseResponse>` | All courses ordered by LastModified |
| `get_course_by_id` | `CourseDetailResponse?` | Course with sections and resources (eager loaded) |
| `create_course` | `CourseResponse` | Create new course |
| `update_course` | `bool` | Update course name/description/location |
| `get_sections_by_course_id` | `List<SectionResponse>` | Sections for a course |
| `get_section_by_id` | `SectionDetailResponse?` | Section with resources |
| `create_section` | `SectionResponse` | Create section in a course |
| `update_section` | `bool` | Update section details |
| `get_resources_by_section_id` | `List<ResourceResponse>` | Resources for a section |
| `get_resource_by_id` | `ResourceResponse?` | Single resource |
| `create_resource` | `ResourceResponse` | Create resource in a section |
| `update_resource` | `bool` | Update resource details |

Delete operations exist but are **not** exposed as kernel functions (internal use only).

### GitHub Copilot CLI Integration

The `GithubCLI.razor.cs` component manages the AI chat interface:

```
CopilotClient (copilot.exe)
    └── CopilotSession
            ├── Tools = GetAIFunctions()      // From Semantic Kernel plugins
            ├── SystemMessage = AgentPrompts.SystemPrompt
            ├── Events:
            │   ├── AssistantMessageEvent → UI (🤖 response)
            │   ├── UserMessageEvent → UI (👤 input)
            │   └── ToolExecutionStartEvent → UI (🛠️ tool call)
            └── SendAsync(query)               // User input → agent
```

### System Prompt (AgentPrompts.cs)

The agent is instructed to:
1. Use Playwright CLI for browser navigation (headed, msedge)
2. Create database entries via kernel functions (`create_course`, `create_section`, `create_resource`)
3. Create folder structure under `KnowledgeLM_Notes/` on disk
4. Screenshot generously, use descriptive filenames
5. Handle pagination/scrolling
6. Prefix folders with numeric ordering (`01-intro/`, `02-setup/`)
7. Write `overview.md` summaries

### State Management

```csharp
// StateService — simple event-based refresh
public class StateService
{
    public event Action? OnRefreshRequested;
    public void RequestRefresh() => OnRefreshRequested?.Invoke();
}
```

- `NotesCRUDService.SaveAndNotifyAsync()` calls `StateService.RequestRefresh()` after DB mutations
- `Notes.razor` and `GithubCLI.razor` subscribe to `OnRefreshRequested` to reload data

---

## UI Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `MainLayout.razor` | Layout/ | FluentLayout shell with NavMenu sidebar and body content |
| `NavMenu.razor` | Layout/ | FluentAppBar with 8 vertical navigation items |
| `Home.razor` | Pages/ | `@page "/"` — FluentMultiSplitter with 3 panes |
| `Notes.razor` | Widgets/ | FluentTreeView displaying Course → Section → Resource hierarchy |
| `GithubCLI.razor` | Widgets/ | Chat interface for GitHub Copilot CLI with message history |
| `MarkdownView.razor` | Widgets/ | MonacoMarkdownEditor with Markdig extensions (frontmatter, tables, task lists, etc.) |

---

## DI Registration (MauiProgram.cs)

```csharp
builder.Services
    .AddMauiBlazorWebView()
    .AddMonacoEditorPack()
    .AddFluentUIComponents()
    .AddServiceDefaults()                          // Aspire: OpenTelemetry, resilience
    .AddKernel()                                    // Semantic Kernel
    .AddDbContextFactory<NotesContext>(...)         // EF Core SQLite (IDbContextFactory for Blazor concurrency)
    .AddScoped<NotesCRUDService>()
    .AddScoped<StateService>();

// Plugin registration
kernel.Plugins.AddFromObject(crudService, "NotesManagement");

// Database
SQLite path: {FileSystem.AppDataDirectory}/knowledgelm.db
EnsureCreated() on startup
```

---

## Web Resource Interception (MainPage.xaml.cs)

`MainPage` intercepts `WebResourceRequested` events to serve local files into the Blazor WebView:

- Requests to `/Notes/*` are mapped to `Q:\Dev\{path}` on disk
- Content-type headers set based on file extension (`.html`, `.jpg`, `.png`, `.css`, `.js`)
- Enables markdown files to reference local images that render in the WebView

---

## Hardcoded Configuration

> ⚠️ These values are currently hardcoded and should be moved to app settings:

| Setting | Value | Location |
|---------|-------|----------|
| Copilot CLI path | `C:\Users\manojdixit\AppData\Local\Microsoft\WinGet\Links\copilot.exe` | `GithubCLI.razor.cs` |
| Notes folder | `Q:\Dev\Notes` | `AgentPrompts.cs` |
| Web resource root | `Q:\Dev\` | `MainPage.xaml.cs` |
| Title bar color | `#6264A7` | `MainPage.xaml.cs` |

---

## Future Vision (Not Yet Implemented)

The following capabilities are planned but not yet built. See `docs/implementation-plan.md` for the original design vision.

### Planned Skills

| Skill | Purpose | Status |
|-------|---------|--------|
| Playwright Skill | Browser automation — navigate, screenshot, extract, authenticate | Planned |
| File Processing Skill | PDF text extraction (PdfPig), YouTube transcripts, text files | Planned |
| Screenshot Analysis Skill | Slide change detection (perceptual hashing), vision AI analysis | Planned |
| Learning Interaction Skill | RAG chat, quiz generation, podcast generation | Planned |

### Planned Features

- **Sit-Back Mode** — Agent autonomously navigates courses and generates notes
- **Read-Along Mode** — Real-time note capture while learner browses
- **RAG Chat** — Chat with notes using vector embeddings
- **Quiz Generation** — Auto-generate quizzes from notes
- **Podcast Generation** — TTS-based podcast from notes
- **Android Reader** — Mobile app for consuming notes, chat, quiz (no scraping)

### Planned Model Evolution

The current `Course → Section → Resource` model may evolve to add:
- `Source` entity (URL, file reference, extraction status)
- `CaptureMode` enum (ReadAlong, SitBack)
- Richer `Notebook` concept wrapping courses with metadata

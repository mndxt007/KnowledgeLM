# KnowledgeLM — Milestones

> **Updated to reflect actual codebase state as of March 2026**

---

## Milestone 0: Foundation (✅ DONE)

Core application shell, data model, AI integration, and developer infrastructure.

| Task | Status | Description |
|------|--------|-------------|
| MAUI Blazor shell | ✅ Done | .NET 10 MAUI Blazor Hybrid app with FluentUI components, 3-pane FluentMultiSplitter layout |
| Domain models | ✅ Done | `Course → Section → Resource` hierarchy with `CommonItemProperties` base, DTOs, `ResourceType` enum |
| EF Core + SQLite storage | ✅ Done | `NotesContext` with 3 tables, indexes, cascade deletes, auto-timestamps, `IDbContextFactory` for Blazor concurrency |
| NotesCRUDService | ✅ Done | 370-line service with 13 `[KernelFunction]` methods — full CRUD for courses, sections, resources |
| Semantic Kernel integration | ✅ Done | `NotesCRUDService` registered as `"NotesManagement"` plugin via `AddKernel()` |
| GitHub Copilot CLI integration | ✅ Done | `GithubCLI.razor` — `CopilotClient` + `CopilotSession` with tool exposure, event handling, chat UI |
| Notes tree view | ✅ Done | `Notes.razor` — `FluentTreeView` displaying Course → Section → Resource hierarchy with icons |
| Markdown editor | ✅ Done | `MarkdownView.razor` — `FluentUI.Blazor.Monaco.EditorPack` with Markdig extensions |
| State management | ✅ Done | `StateService` event-based refresh — components subscribe to `OnRefreshRequested` |
| Agent system prompt | ✅ Done | `AgentPrompts.cs` — comprehensive instructions for autonomous note-taking |
| Aspire hosting | ✅ Done | `AppHost` project with MAUI orchestration for local development |
| OpenTelemetry + resilience | ✅ Done | `MauiServiceDefaults` — logging, metrics, tracing, HTTP resilience, service discovery |
| Web resource interception | ✅ Done | `MainPage.xaml.cs` intercepts `/Notes/*` requests → serves from local disk |

---

## Milestone 1: Notes ↔ Editor Linking + Service Extraction

Connect the Notes tree pane to the MarkdownView editor and Copilot chat. Extract GHCP CLI code into a dedicated service.

| Task | Description |
|------|-------------|
| Resource selection event | Add click/selection handler to `Notes.razor` tree items — when a resource is clicked, emit a selected resource event |
| Pass resource to MarkdownView | When a markdown resource is selected in the Notes pane, load its content into the `MarkdownView` editor |
| Pass resource context to GHCP CLI | When a resource is selected, pass its metadata (name, path, type) as context to the Copilot chat session |
| Extract `CopilotService` | Move `CopilotClient`, `CopilotSession`, and related logic from `GithubCLI.razor.cs` into a dedicated `Services/CopilotService.cs` |
| Configurable paths | Move hardcoded paths (Copilot CLI path, Notes folder, web resource root) to app settings / configuration |

---

## Milestone 2: Local Markdown Loading with Image Support

Load, edit, and save markdown files from disk with proper image rendering in the WebView.

| Task | Description |
|------|-------------|
| Markdown file loading | Load `.md` files from the Notes folder into the Monaco editor when a resource is selected |
| Image URL rewriting | Implement Markdig extension to rewrite local file paths (`C:\...\img.jpg`) to virtual paths (`/_localfile/...`) at render time |
| WebResourceRequested handler | Extend `MainPage.xaml.cs` handler to serve `/_localfile/*` requests from local disk (or implement `CreateFileProvider` override) |
| Markdown saving | Save edited markdown back to the original file on disk with dirty-state tracking |
| File ↔ DB sync | When markdown files are saved, update the corresponding `Resource` record in SQLite (LastModified, Location) |

> See `docs/local-content-loading-plan.md` and `docs/markdown-editor-viewer-options.md` for detailed analysis.

---

## Milestone 3: Knowledge Harvesting

AI-powered content extraction and interactive learning features.

| Task | Description |
|------|-------------|
| RAG pipeline | Chunk markdown notes → generate embeddings → vector search for contextual Q&A |
| Chat with notes | UI for asking questions about notes with cited answers from the RAG pipeline |
| Podcast generation | Generate conversational podcast scripts from notes → TTS audio (Azure/OpenAI) |
| Flashcard generation | Auto-generate flashcards from notes with spaced repetition tracking |
| Quiz generation | Interactive quiz UI with scoring based on note content |

---

## Milestone 4: Navigation Menu

Improve app navigation and multi-page support.

| Task | Description |
|------|-------------|
| Navigation structure | Redesign `NavMenu.razor` — organize 8 items into logical groups |
| Route management | Add routes for dedicated pages (Chat, Quiz, Podcast, Settings) |
| Breadcrumb / context | Show current notebook/course/section context in the nav area |
| Responsive layout | Adapt 3-pane layout for different window sizes |

---

## Milestone 5: Copilot Session Resume

Persist and resume Copilot chat sessions across app restarts.

| Task | Description |
|------|-------------|
| Session persistence | Save Copilot session state (conversation history, tool results) to SQLite or local file |
| Session listing | UI to list past sessions with timestamps, summaries, and associated notebooks |
| Session resume | Restore a previous session's context and continue the conversation |
| Session export | Export session transcripts as markdown for reference |

---

## Milestone 6: Browser Automation (Playwright)

Integrate Playwright as a Semantic Kernel skill for autonomous web content extraction.

| Task | Description |
|------|-------------|
| PlaywrightSkill | Register AIFunction tools: NavigateToUrl, TakeScreenshot, ExtractPageContent, ClickElement, ScrollPage, WaitForContent, GetPageLinks, AuthenticateWithSSO, TypeText |
| NoteManagementSkill | Register AIFunction tools: CreateNote, AppendToNote, ReadNote, SaveScreenshot (file system operations) |
| Sit-Back orchestration | Agent prompt engineering for autonomous course navigation + note generation |
| Progress UI | Real-time progress display: current page, screenshots taken, notes generated |

---

## Milestone 7: Advanced Skills & Polish

Build remaining custom skills and polish the experience.

| Task | Description |
|------|-------------|
| FileProcessingSkill | ExtractPdfText (PdfPig), ParseYouTubeTranscript (API), ReadTextFile |
| ScreenshotAnalysisSkill | DetectSlideChange (perceptual hashing), AnalyzeScreenshot (vision model) |
| Read-Along mode | Real-time note capture while learner browses content |
| Error handling & resilience | Robust error handling, retry logic, user feedback across all features |
| UX polish | UI/UX refinement, theming, responsive layout, accessibility |
| Android reader | Android MAUI build: notes viewer, chat, quiz (no Playwright) |

---

## Dependency Graph

```
Milestone 0 (Foundation) ✅ DONE
    ├── MAUI shell, FluentUI, 3-pane layout
    ├── Course/Section/Resource models + EF Core SQLite
    ├── NotesCRUDService (13 Kernel functions)
    ├── GitHub Copilot CLI + Semantic Kernel integration
    ├── Notes tree, Markdown editor, Agent chat UI
    └── Aspire + OpenTelemetry infrastructure

Milestone 1 (Notes ↔ Editor Linking)
    ├── resource-selection ← Notes.razor (M0)
    ├── pass-to-markdownview ← resource-selection + MarkdownView (M0)
    ├── pass-to-ghcp ← resource-selection + GithubCLI (M0)
    ├── extract-copilot-service ← GithubCLI.razor.cs (M0)
    └── configurable-paths ← AgentPrompts + MainPage (M0)

Milestone 2 (Local Markdown Loading)
    ├── markdown-file-loading ← resource-selection (M1)
    ├── image-url-rewriting ← MarkdownView (M0)
    ├── webresource-handler ← MainPage.xaml.cs (M0)
    ├── markdown-saving ← markdown-file-loading
    └── file-db-sync ← markdown-saving + NotesCRUDService (M0)

Milestone 3 (Knowledge Harvesting)
    ├── rag-pipeline ← NotesCRUDService (M0) + markdown-file-loading (M2)
    ├── chat-with-notes ← rag-pipeline
    ├── podcast-gen ← rag-pipeline
    ├── flashcard-gen ← rag-pipeline
    └── quiz-gen ← rag-pipeline

Milestone 4 (Navigation)
    ├── nav-structure ← NavMenu.razor (M0)
    ├── route-management ← Home.razor (M0) + new pages
    ├── breadcrumb ← resource-selection (M1)
    └── responsive-layout ← MainLayout (M0)

Milestone 5 (Session Resume)
    ├── session-persistence ← extract-copilot-service (M1)
    ├── session-listing ← session-persistence
    ├── session-resume ← session-persistence
    └── session-export ← session-persistence

Milestone 6 (Playwright)
    ├── playwright-skill ← Semantic Kernel (M0)
    ├── note-management-skill ← NotesCRUDService (M0)
    ├── sitback-orchestration ← playwright-skill + note-management-skill
    └── progress-ui ← MainLayout (M0)

Milestone 7 (Advanced + Polish)
    ├── file-processing-skill ← Semantic Kernel (M0)
    ├── screenshot-analysis-skill ← Semantic Kernel (M0)
    ├── readalong-mode ← playwright-skill (M6)
    ├── error-handling ← all milestones
    ├── ux-polish ← all milestones
    └── android-reader ← M0 + M2 + M3
```

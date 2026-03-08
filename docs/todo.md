# KnowledgeLM — TODO

> Prioritized for **functionality over cosmetics**. Each item includes context, related tasks, and implementation suggestions.

---

## 1. Link Notes Pane ↔ MarkdownView ↔ GHCP CLI

When a resource is selected in the Notes tree, the MarkdownView should load its content and the GitHub Copilot CLI should receive the resource context so the agent knows what the user is looking at.

**Current state:** `Notes.razor` renders a `FluentTreeView` but tree items have no click/selection handler. `MarkdownView.razor` displays hardcoded `"# Hello World"`. `GithubCLI.razor.cs` has no concept of a "selected resource". All three widgets sit in `Home.razor` as siblings inside a `FluentMultiSplitter` with no shared state.

### Tasks

- **Add `OnSelected` callback to `FluentTreeItem`** in `Notes.razor` — capture the clicked resource's Id, Name, Location, and ResourceType.
- **Extend `StateService`** with a `SelectedResource` property and `OnResourceSelected` event (or create a dedicated `SelectionService`). When a resource is clicked in Notes, publish the selection.
- **Subscribe in `MarkdownView.razor`** — on resource selection, if `ResourceType == Markdown`, read the `.md` file from `resource.Location` and set it as the editor content.
- **Subscribe in `GithubCLI.razor.cs`** — on resource selection, inject resource context (name, path, type) into the next Copilot query or update the system message with current context.
- **Add `[Parameter] EventCallback<ResourceResponse>` to `Notes.razor`** as an alternative to service-based eventing, letting `Home.razor` wire it up.

### Implementation suggestions

- `StateService` already has the `OnRefreshRequested` pattern — replicate it with `OnResourceSelected(ResourceResponse)`. Both `MarkdownView` and `GithubCLI` already inject `StateService`.
- For the GHCP CLI context, prepend the resource info to the user's query: `$"[Context: viewing {resource.Name} at {resource.Location}]\n{userQuery}"` — simplest approach, no SDK changes needed.
- `FluentTreeItem` supports `@onclick` or `OnClick` — wire it on the resource-level items only (not course/section).

---

## 2. Extract GHCP CLI Code into Separate Service

`GithubCLI.razor.cs` (170 lines) mixes UI concerns with Copilot client lifecycle, session management, and tool registration. Extract the AI integration into a dedicated service.

**Current state:** `CopilotClient` creation, `CopilotSession` setup, `GetAIFunctions()`, event subscriptions, and `HandleQuery()` all live inside the component code-behind. Hardcoded CLI path (`C:\Users\manojdixit\...\copilot.exe`) sits in `OnInitialized()`.

### Tasks

- **Create `Services/CopilotService.cs`** — owns `CopilotClient` and `CopilotSession` lifecycle. Exposes `InitializeAsync()`, `SendQueryAsync(string)`, `OnMessage` event, and `IsWorking` state.
- **Move `GetAIFunctions()` into `CopilotService`** — inject `Kernel` there instead of in the component.
- **Move `AgentPrompts.SystemPrompt` usage** into the service — the service configures the session, not the UI.
- **Extract hardcoded config** — CLI path, notes folder path (`AgentPrompts.NotesFolder`), and web resource root (`Q:\Dev`) into `appsettings.json` or `IConfiguration`.
- **Register as `AddSingleton<CopilotService>`** in `MauiProgram.cs` — singleton so the session persists across navigations.
- **Simplify `GithubCLI.razor.cs`** — inject `CopilotService`, subscribe to its message events, render messages. No SDK types in the component.

### Implementation suggestions

- The service should expose an `IObservable<CoPilotMessages>` or simple `event Action<CoPilotMessages>` for the UI to subscribe to. Keep the `ConcurrentBag<CoPilotMessages>` in the service so message history persists if the component re-renders.
- For configuration, use `IOptions<CopilotOptions>` pattern:
  ```csharp
  public class CopilotOptions
  {
      public string CliPath { get; set; }
      public string NotesFolder { get; set; }
  }
  ```
- This is a prerequisite for TODO #6 (Session Resume) — the service will later gain persistence methods.

### Related

- TODO #1 (resource context injection happens in this service)
- TODO #6 (session resume builds on this service)

---

## 3. Markdown Loading from Local Files with Image Support, Editing & Saving

Load `.md` files from disk into the Monaco editor, render local images correctly in the WebView, and save edits back to disk.

**Current state:** `MarkdownView.razor` uses `MonacoMarkdownEditor` from `FluentUI.Blazor.Monaco.EditorPack` with hardcoded content. `MainPage.xaml.cs` already intercepts `WebResourceRequested` for `/Notes/*` paths and serves files from `Q:\Dev\`. Image loading from local paths is blocked by WebView2 security (`file:///` URLs blocked from HTTPS origin).

### Tasks

- **File loading** — when a resource is selected (TODO #1), read the `.md` file from `resource.Location` using `File.ReadAllTextAsync()` and pass to the editor.
- **Image URL rewriting** — build a Markdig extension or regex preprocessor that rewrites local image paths (`C:\...\img.jpg` or relative paths) to virtual paths (`/_localfile/C/path/img.jpg`) before rendering.
- **Extend `WebResourceRequested` handler** — in `MainPage.xaml.cs`, detect `/_localfile/` prefix, reconstruct the real filesystem path, serve the file with correct content-type.
- **Save support** — add a save button or Ctrl+S handler. Write the editor content back to disk via `File.WriteAllTextAsync()`. Undo the image URL rewriting before saving (or only rewrite in the preview, not the source).
- **Dirty-state tracking** — track whether content has changed since last save. Show indicator in the UI.
- **DB sync on save** — update the `Resource.LastModified` in SQLite after saving to keep DB and disk in sync.

### Implementation suggestions

- See `docs/local-content-loading-plan.md` for detailed analysis of Options 1–5 for image loading. **Option 2 (WebResourceRequested + path rewriting)** is recommended — it builds on the existing handler in `MainPage.xaml.cs`.
- The `MonacoMarkdownEditor` component separates source markdown from rendered preview. Rewrite image paths only in the preview pipeline (Markdig rendering), not in the source text — this keeps saved files compatible with other markdown readers.
- For Markdig image rewriting:
  ```csharp
  foreach (var link in document.Descendants<LinkInline>())
      if (link.IsImage && IsLocalPath(link.Url))
          link.Url = ToVirtualPath(link.Url);
  ```
- Consider a `MarkdownFileService` that handles load/save/dirty-state, injected into `MarkdownView.razor`.

### Related

- TODO #1 (resource selection triggers file loading)
- `docs/local-content-loading-plan.md` (detailed options analysis)
- `docs/markdown-editor-viewer-options.md` (editor component evaluation)

---

## 4. Knowledge Harvesting — RAG, Podcasts, Flashcards

Build interactive learning features that transform notes into consumable formats: searchable Q&A (RAG), audio podcasts, and flashcards.

**Current state:** No harvesting features exist. The foundation is in place: markdown notes on disk, `NotesCRUDService` for metadata, Semantic Kernel for AI orchestration, `Microsoft.Extensions.AI` for embeddings.

### Tasks

- **RAG pipeline** — chunk markdown notes → generate embeddings (via `Microsoft.Extensions.AI`) → store in a vector index (SQLite with vector extension, or in-memory HNSW) → retrieve relevant chunks for user queries.
- **Chat with notes UI** — new page/widget for asking questions about notes. Display answers with citations linking back to source notes/sections.
- **Podcast generation** — generate a conversational script from notes using the LLM → convert to audio using TTS (Azure Cognitive Services Speech SDK or OpenAI TTS API). Store as `.mp3` in the media folder.
- **Flashcard generation** — extract key concepts from notes → generate question/answer pairs → display in a flashcard UI with flip animation. Consider spaced repetition (SM-2 algorithm).
- **Quiz generation** — multiple-choice quizzes auto-generated from notes with scoring.

### Implementation suggestions

- Start with **RAG chat** — it's the highest-value feature and validates the embedding pipeline that podcasts and flashcards also need.
- For vector storage, evaluate `Microsoft.Extensions.VectorData` with SQLite or use a simple in-memory approach for the prototype (notes are local, dataset is small).
- Register a `KnowledgeHarvestingSkill` as a Semantic Kernel plugin with functions like `SearchNotes(query)`, `GenerateFlashcards(courseId)`, `GeneratePodcastScript(courseId)`.
- For TTS, `Azure.AI.OpenAI` supports the audio API. Alternatively, use browser-native `SpeechSynthesis` via JS interop for a zero-dependency prototype.
- Flashcard UI: `FluentCard` with CSS flip animation. Track `EaseFactor`, `Interval`, `NextReview` in SQLite for spaced repetition.

### Related

- TODO #3 (markdown loading provides the source content for harvesting)
- Milestone 3 in `docs/milestones.md`

---

## 5. Navigation Menu

Redesign the navigation structure for multi-feature support as the app grows beyond the single Home page.

**Current state:** `NavMenu.razor` has a `FluentAppBar` with 8 vertical items but only the Home route (`/`) is functional. The app is essentially single-page with the 3-pane splitter.

### Tasks

- **Define route structure** — decide on pages: Home (current 3-pane), Chat (dedicated RAG chat), Quiz, Podcast Player, Settings, About.
- **Reorganize `NavMenu.razor`** — group items logically: Learning (Notes, Editor), Interact (Chat, Quiz, Podcast), System (Settings, About).
- **Add routes** — create `.razor` pages for each new route with `@page` directives.
- **Breadcrumb context** — show the currently selected Course → Section → Resource path in the header area so the user always knows their context.
- **Responsive layout** — handle nav collapse for narrow windows. `FluentAppBar` supports `Collapsible`.

### Implementation suggestions

- Use `FluentNavMenu` with `FluentNavGroup` for grouped navigation instead of the flat `FluentAppBar` — it supports hierarchical menu with expand/collapse.
- Keep the 3-pane Home layout as the primary workspace. Dedicated pages (Chat, Quiz) can be simpler single-content layouts.
- For breadcrumbs, subscribe to `StateService.OnResourceSelected` (from TODO #1) and render the path in `MainLayout.razor`.

### Related

- TODO #1 (resource selection feeds breadcrumb context)
- TODO #4 (new pages needed for Chat, Quiz, Podcast)

---

## 6. Copilot Session Resume

Persist Copilot chat sessions so they survive app restarts and can be resumed or reviewed later.

**Current state:** `CopilotSession` is created fresh on each app start in `GithubCLI.razor.cs.OnAfterRenderAsync()`. Message history lives in a `ConcurrentBag<CoPilotMessages>` in the component — lost on navigation or restart.

### Tasks

- **Persist message history** — save `CoPilotMessages` (EventType, Message, Timestamp) to SQLite. Create a `ChatSessions` table (Id, Name, CreatedAt, LastActiveAt) and `ChatMessages` table (SessionId, EventType, Message, Timestamp).
- **Session listing UI** — show past sessions with timestamps and previews (first user message or auto-generated summary). Allow selecting a session to view/resume.
- **Session resume** — when resuming, reload message history into the UI. Re-create the `CopilotSession` and inject conversation history as context (system message with prior conversation summary, or replay key messages).
- **Auto-save** — save messages as they arrive (in the `session.On()` handler), not just on explicit save.
- **Session export** — export a session's messages as a markdown file for reference.

### Implementation suggestions

- This depends on TODO #2 (CopilotService extraction) — the service manages sessions and owns the persistence logic.
- For session resume, the Copilot SDK may not support replaying history natively. Workaround: inject a system message summarizing the prior conversation: `"Previous conversation summary: {summary}"`. Use the LLM itself to generate the summary from saved messages.
- Add `ChatSession` and `ChatMessage` entities to `NotesContext`, or create a separate `ChatContext` to keep concerns separated.
- For the listing UI, a `FluentDataGrid` with columns: Session Name, Created, Last Active, Message Count. Click to resume.
- Consider linking sessions to courses — when a session was started while viewing a specific course, store the `CourseId` for context.

### Related

- TODO #2 (CopilotService must exist first — session lifecycle lives there)
- Milestone 5 in `docs/milestones.md`

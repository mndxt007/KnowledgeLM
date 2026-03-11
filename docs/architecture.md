# KnowledgeLM — Architecture

> **Last updated to match codebase state as of March 2026**

---

## System Overview

KnowledgeLM is a **.NET MAUI Blazor Hybrid** desktop application for AI-powered learning and note-taking. It combines **Microsoft Semantic Kernel** for AI plugin orchestration, **GitHub Copilot SDK** as the LLM interface, **Playwright MCP** for browser automation, and **Azure OpenAI** for vector embeddings and speech synthesis.

### High-Level Architecture

```mermaid
graph TB
    subgraph UI["🖥️ .NET MAUI Blazor Shell"]
        subgraph Pages["Pages"]
            Home["Home.razor<br/><i>3-pane workspace</i>"]
            Harvest["Harvest.razor<br/><i>Study assistant</i>"]
        end

        subgraph HomeWidgets["Home Widgets"]
            Notes["Notes<br/><i>TreeView</i>"]
            GithubCLI["GithubCLI<br/><i>Chat + Harvest</i>"]
            MDView["MarkdownView<br/><i>Monaco Editor</i>"]
        end

        subgraph HarvestWidgets["Harvest Widgets"]
            HarvestChat["HarvestChatPane<br/><i>RAG Q&A</i>"]
            HarvestNotes["HarvestNotesPane<br/><i>Resource tree</i>"]
            Flashcards["FlashcardViewer"]
            PodcastPlayer["PodcastPlayer"]
        end
    end

    subgraph AI["🤖 AI Integration Layer"]
        CopilotSDK["GitHub Copilot SDK<br/><i>CopilotService</i>"]
        SK["Semantic Kernel<br/><i>Plugin host</i>"]
        MCP["Playwright MCP<br/><i>Browser automation</i>"]
        Embeddings["Azure OpenAI<br/><i>Embeddings</i>"]
        Speech["Azure Speech<br/><i>TTS / Podcasts</i>"]
    end

    subgraph Services["⚙️ Service Layer"]
        CRUD["NotesCRUDService<br/><i>13+ KernelFunctions</i>"]
        VectorSvc["VectorStoreService<br/><i>Chunking + search</i>"]
        RAG["RAGSearchPlugin<br/><i>Semantic search</i>"]
        FlashSvc["FlashcardService<br/><i>Q&A generation</i>"]
        PodSvc["PodcastService<br/><i>SSML + audio</i>"]
        StateSvc["StateService<br/><i>Event bus</i>"]
        HarvestState["HarvestStateService<br/><i>Scope tracking</i>"]
        NotifSvc["AppNotificationService"]
    end

    subgraph Data["💾 Data Layer"]
        EF["NotesContext<br/><i>EF Core 10 + SQLite</i>"]
        VecDB["SqliteVec<br/><i>Vector embeddings</i>"]
        FS["File System<br/><i>Markdown + images</i>"]
    end

    subgraph Infra["🔧 Infrastructure"]
        Aspire["Aspire AppHost<br/><i>Orchestration</i>"]
        OTel["OpenTelemetry<br/><i>Logs, metrics, traces</i>"]
        Resilience["HTTP Resilience"]
    end

    Home --> HomeWidgets
    Harvest --> HarvestWidgets
    GithubCLI --> CopilotSDK
    HarvestChat --> CopilotSDK
    CopilotSDK --> SK
    CopilotSDK --> MCP
    SK --> CRUD
    SK --> RAG
    SK --> FlashSvc
    SK --> PodSvc
    CRUD --> EF
    CRUD --> FS
    CRUD --> StateSvc
    VectorSvc --> Embeddings
    VectorSvc --> VecDB
    RAG --> VectorSvc
    PodSvc --> Speech
    FlashSvc --> EF
    PodSvc --> EF
    StateSvc --> Notes
    StateSvc --> MDView
    HarvestState --> HarvestNotes
    HarvestState --> HarvestChat
    MDView --> FS
```

---

## Project Dependency Graph

```mermaid
graph LR
    slnx["KnowledgeLM.slnx"]

    subgraph Solution
        App["KnowledgeLM.App<br/><i>MAUI Blazor Hybrid</i><br/><i>net10.0-windows</i>"]
        AppHost["KnowledgeLM.AppHost<br/><i>.NET Aspire Host</i><br/><i>net10.0</i>"]
        Defaults["KnowledgeLM.MauiServiceDefaults<br/><i>Service Defaults Library</i><br/><i>net10.0-windows</i>"]
        Monaco["KnowledgeLM.MonacoEditor<br/><i>Razor Class Library</i><br/><i>net10.0</i>"]
    end

    App --> Monaco
    App --> Defaults
    AppHost -.->|"ReferenceOutputAssembly=false"| App

    style App fill:#4a9eff,color:#fff
    style AppHost fill:#ff9f43,color:#fff
    style Defaults fill:#26de81,color:#fff
    style Monaco fill:#a55eea,color:#fff
```

---

## Solution Structure

```
KnowledgeLM/
├── KnowledgeLM.slnx
├── docs/
│   ├── architecture.md                    # This file
│   ├── milestones.md
│   ├── implementation-plan.md
│   ├── feasibility.md
│   ├── local-content-loading-plan.md
│   ├── markdown-editor-viewer-options.md
│   └── todo.md
│
└── src/
    ├── KnowledgeLM.App/                              # Main MAUI Blazor application
    │   ├── Components/
    │   │   ├── Pages/
    │   │   │   ├── Home.razor                         # @page "/" — 3-pane workspace
    │   │   │   ├── Harvest.razor                      # @page "/Harvest" — Study assistant
    │   │   │   └── NotFound.razor
    │   │   ├── Layout/
    │   │   │   ├── MainLayout.razor                   # FluentLayout shell
    │   │   │   └── NavMenu.razor                      # FluentAppBar sidebar
    │   │   ├── Widgets/
    │   │   │   ├── Notes.razor(.cs/.js)               # Course → Section → Resource tree
    │   │   │   ├── GithubCLI.razor(.cs/.js)           # Copilot chat + web harvesting
    │   │   │   ├── MarkDownView.razor(.cs)            # Monaco editor wrapper
    │   │   │   ├── HarvestChatPane.razor(.cs)         # RAG Q&A chat
    │   │   │   ├── HarvestNotesPane.razor(.cs)        # Resource tree with vectorization
    │   │   │   ├── FlashcardViewer.razor(.cs)         # Flip card study UI
    │   │   │   └── PodcastPlayer.razor(.cs)           # Audio playback
    │   │   ├── Routes.razor
    │   │   └── _Imports.razor
    │   ├── Data/
    │   │   └── NotesContext.cs                         # EF Core DbContext
    │   ├── Models/
    │   │   ├── Notes/
    │   │   │   ├── CommonItemProperties.cs            # Abstract base entity
    │   │   │   ├── Course.cs / Section.cs / Resource.cs
    │   │   │   ├── ResourceType.cs                    # Enum: Image, Video, Other, Markdown
    │   │   │   └── DTOs/                              # CourseDto, SectionDto, ResourceDto, etc.
    │   │   ├── AI/
    │   │   │   ├── VectorizedResource.cs              # Vectorization tracking
    │   │   │   ├── Flashcard.cs                       # Q&A pairs
    │   │   │   ├── Podcast.cs                         # Audio metadata
    │   │   │   └── NoteChunk.cs                       # Vector store record (1536-dim)
    │   │   └── CoPilotMessages.cs                     # Chat message model
    │   ├── Services/
    │   │   ├── NotesCRUDService.cs                    # CRUD + Semantic Kernel plugin
    │   │   ├── CopilotService.cs                      # GitHub Copilot SDK wrapper
    │   │   ├── VectorStoreService.cs                  # Embedding + semantic search
    │   │   ├── RAGSearchPlugin.cs                     # Semantic Kernel RAG plugin
    │   │   ├── FlashcardService.cs                    # Flashcard generation plugin
    │   │   ├── PodcastService.cs                      # Podcast generation plugin
    │   │   ├── StateService.cs                        # Global event bus
    │   │   ├── HarvestStateService.cs                 # Study scope tracking
    │   │   └── AppNotificationService.cs              # Toast notifications
    │   ├── Helpers/
    │   │   ├── AgentPrompts.cs                        # System prompt + MCP config
    │   │   └── ManagedStream.cs                       # Stream wrapper for WebView
    │   ├── MauiProgram.cs                             # DI, DB init, Kernel setup
    │   ├── MainPage.xaml(.cs)                         # BlazorWebView + WebResourceRequested
    │   ├── App.xaml(.cs)                              # MAUI app entry
    │   ├── appsettings.json                           # Azure OpenAI + Speech config
    │   └── wwwroot/                                   # index.html, CSS
    │
    ├── KnowledgeLM.MonacoEditor/                      # Razor Class Library
    │   ├── Components/
    │   │   ├── MonacoMarkdownEditor.razor(.cs)        # Split editor + preview
    │   │   └── MarkdownToolbar.razor(.cs)             # Formatting actions
    │   ├── Extensions/
    │   │   └── ServiceCollectionExtensions.cs          # DI: AddMonacoEditorPack()
    │   ├── Markdown/
    │   │   ├── MarkdownPipelineFactory.cs             # Markdig pipeline config
    │   │   └── MarkdownOptions.cs
    │   ├── Memento/
    │   │   ├── TextEditorSession.cs                   # Undo/redo session
    │   │   ├── TextOriginator.cs / TextMemento.cs / TextHistory.cs
    │   │   └── IMemento.cs
    │   ├── Monaco/
    │   │   └── MonacoOptions.cs                       # Editor configuration
    │   └── wwwroot/js/
    │       ├── monacoMarkdownEditor.js                # Editor lifecycle + interop
    │       ├── monacoMarkdownToolbar.js               # Toolbar JS actions
    │       ├── monaco-editor-pack.js                  # CDN loader (v0.52.2)
    │       ├── fluentUIDesignTokens.js                # Fluent UI token extraction
    │       └── cssClassHarvester.js                   # CSS IntelliSense
    │
    ├── KnowledgeLM.AppHost/                           # .NET Aspire orchestration
    │   └── AppHost.cs                                 # AddMauiProject + AddWindowsDevice
    │
    └── KnowledgeLM.MauiServiceDefaults/               # Shared service config
        └── Extensions.cs                              # OpenTelemetry, resilience, discovery
```

---

## Tech Stack

| Layer | Technology | Status |
|-------|-----------|--------|
| App Framework | .NET 10 MAUI Blazor Hybrid | ✅ Implemented |
| UI Components | Microsoft FluentUI Blazor (v4.14) | ✅ Implemented |
| Markdown Editor | Monaco Editor v0.52.2 (CDN) + Markdig | ✅ Implemented |
| AI Orchestration | Microsoft Semantic Kernel (v1.73) | ✅ Implemented |
| AI Agent | GitHub Copilot SDK (v0.1.30) | ✅ Implemented |
| AI Extensions | Microsoft.Extensions.AI (v10.3) | ✅ Implemented |
| Vector Search | SqliteVec + Azure OpenAI Embeddings (text-embedding-ada-002) | ✅ Implemented |
| Speech Synthesis | Azure Cognitive Services Speech (v1.48.2) — SSML | ✅ Implemented |
| Browser Automation | Playwright MCP Server (@playwright/mcp, MS Edge) | ✅ Implemented |
| Audio Playback | Plugin.Maui.Audio (v4.0) | ✅ Implemented |
| Database | EF Core 10 + SQLite | ✅ Implemented |
| Hosting | .NET Aspire AppHost (v13.1.2) | ✅ Implemented |
| Observability | OpenTelemetry (logging, metrics, tracing) | ✅ Implemented |
| HTTP Resilience | Microsoft.Extensions.Http.Resilience | ✅ Implemented |
| HTML Sanitization | HtmlSanitizer (v9.0) | ✅ Implemented |

---

## Data Model

### Entity Relationship Diagram

```mermaid
erDiagram
    Course ||--o{ Section : "has many"
    Section ||--o{ Resource : "has many"
    Resource ||--o| VectorizedResource : "tracking"
    Resource ||--o{ Flashcard : "has many"
    Resource ||--o{ Podcast : "has many"

    Course {
        int Id PK
        string Name "required, 200 chars"
        string Description "2000 chars"
        string Location "500 chars"
        datetime CreatedAt "auto"
        datetime LastModified "auto"
    }

    Section {
        int Id PK
        int CourseId FK
        string Name "required, 200 chars"
        string Description "2000 chars"
        string Location "500 chars"
        datetime CreatedAt "auto"
        datetime LastModified "auto"
    }

    Resource {
        int Id PK
        int SectionId FK
        string Name "required, 200 chars"
        string Location "500 chars"
        ResourceType ResourceType "Image|Video|Other|Markdown"
        datetime CreatedAt "auto"
        datetime LastModified "auto"
    }

    VectorizedResource {
        int Id PK
        int ResourceId FK "unique"
        datetime VectorizedAt
        int ChunkCount
        string Status "Pending|Completed"
    }

    Flashcard {
        int Id PK
        int ResourceId FK
        string Question "required"
        string Answer "required"
        datetime CreatedAt
    }

    Podcast {
        int Id PK
        int ResourceId FK
        string Title "500 chars"
        string AudioFilePath "1000 chars"
        string SsmlContent
        long DurationTicks
        datetime CreatedAt
    }

    NoteChunk {
        string Id PK "vector store key"
        int ResourceId "indexed"
        int SectionId "indexed"
        int CourseId "indexed"
        string Text "content"
        string SourceName
        string SourceLink
        float_array Embedding "1536 dims"
    }
```

### Database Layout

| Database | Engine | Purpose |
|----------|--------|---------|
| `knowledgelm.db` | SQLite (EF Core) | Core entities: Courses, Sections, Resources, VectorizedResources, Flashcards, Podcasts |
| `knowledgelm_vectors.db` | SQLite + SqliteVec | Vector embeddings: `note_chunks` collection (1536-dim, Azure OpenAI) |

---

## AI Agent Architecture

### Copilot Session & Tool Flow

```mermaid
flowchart LR
    User["👤 User Input"] --> Session["CopilotSession"]

    subgraph CopilotSDK["GitHub Copilot SDK"]
        Session --> Events{Events}
        Events -->|AssistantMessageEvent| Response["🤖 Response"]
        Events -->|ToolExecutionStartEvent| ToolExec["🛠️ Tool Execution"]
        Events -->|UserMessageEvent| Echo["👤 Echo"]
    end

    subgraph Tools["Registered Tools (AIFunctions)"]
        direction TB
        NM["NotesManagement Plugin"]
        FC["Flashcards Plugin"]
        PC["Podcasts Plugin"]
        RAGp["RAGSearch Plugin"]
        PW["Playwright MCP Server"]
    end

    ToolExec --> Tools
    Session -->|"SystemMessage"| Prompt["AgentPrompts.SystemPrompt"]
    Session -->|"McpServers"| PW

    style CopilotSDK fill:#2d3436,color:#fff
    style Tools fill:#636e72,color:#fff
```

### Semantic Kernel Plugins

```mermaid
graph TB
    subgraph Kernel["Semantic Kernel"]
        subgraph NM["NotesManagement Plugin"]
            gc["get_all_courses"]
            gcid["get_course_by_id"]
            cc["create_course"]
            uc["update_course"]
            dc["delete_course"]
            gs["get_sections_by_course_id"]
            gsid["get_section_by_id"]
            cs["create_section"]
            us["update_section"]
            gr["get_resources_by_section_id"]
            grid["get_resource_by_id"]
            cr["create_resource"]
            ur["update_resource"]
            gnf["get_notes_folder"]
        end

        subgraph FC["Flashcards Plugin"]
            gf["get_flashcards"]
            sf["save_flashcards"]
        end

        subgraph PC["Podcasts Plugin"]
            gpa["generate_podcast_audio"]
            sp["save_podcast"]
        end

        subgraph RAGp["RAGSearch Plugin"]
            gcc["get_current_context"]
            sn["search_notes"]
        end
    end

    NM --> DB[(SQLite)]
    NM --> FS[("File System")]
    FC --> DB
    PC --> AzSpeech["Azure Speech"]
    PC --> DB
    RAGp --> VecSvc["VectorStoreService"]
    VecSvc --> VecDB[(SqliteVec)]
    VecSvc --> AzOAI["Azure OpenAI<br/>Embeddings"]
```

### MCP Server Configuration

```mermaid
flowchart LR
    CopilotSession -->|"McpServers config"| MCP["Playwright MCP Server"]
    MCP -->|"npx @playwright/mcp@latest"| Edge["Microsoft Edge<br/>(headed)"]

    subgraph BrowserTools["Available Browser Tools"]
        nav["browser_navigate"]
        snap["browser_snapshot"]
        screen["browser_screenshot"]
        click["browser_click"]
        typ["browser_type"]
        scroll["browser_scroll_down/up"]
        wait["browser_wait"]
        close["browser_close"]
    end

    MCP --> BrowserTools
    BrowserTools --> Edge
```

---

## Application Workflows

### Web Harvesting (Home Page)

```mermaid
sequenceDiagram
    actor User
    participant CLI as GithubCLI.razor
    participant Copilot as CopilotService
    participant SK as Semantic Kernel
    participant CRUD as NotesCRUDService
    participant PW as Playwright MCP
    participant DB as SQLite
    participant FS as File System
    participant State as StateService
    participant Tree as Notes.razor

    User->>CLI: Enter URL / instruction
    CLI->>Copilot: CreateSessionAsync(tools, systemPrompt, mcpServers)
    Copilot-->>CLI: Session created

    CLI->>Copilot: SendAsync(query)
    Copilot->>PW: browser_navigate(url)
    PW-->>Copilot: Page loaded

    Copilot->>PW: browser_snapshot()
    PW-->>Copilot: Page structure (accessibility tree)

    Copilot->>SK: create_course(name, description)
    SK->>CRUD: CreateCourseAsync()
    CRUD->>DB: INSERT Course
    CRUD->>State: RequestRefresh()
    State-->>Tree: OnRefreshRequested

    loop For each section
        Copilot->>SK: create_section(courseId, name)
        SK->>CRUD: CreateSectionAsync()
        CRUD->>DB: INSERT Section

        Copilot->>PW: browser_click(section link)
        Copilot->>PW: browser_screenshot()
        PW-->>FS: Save screenshot

        Copilot->>FS: mkdir + write notes.md

        Copilot->>SK: create_resource(sectionId, name, type)
        SK->>CRUD: CreateResourceAsync()
        CRUD->>DB: INSERT Resource
        CRUD->>State: RequestRefresh()
    end

    Copilot->>PW: browser_close()
    Copilot-->>CLI: AssistantMessageEvent (summary)
    CLI-->>User: Display response
```

### Study Assistant (Harvest Page)

```mermaid
sequenceDiagram
    actor User
    participant HNP as HarvestNotesPane
    participant HState as HarvestStateService
    participant VecSvc as VectorStoreService
    participant AzOAI as Azure OpenAI
    participant VecDB as SqliteVec DB
    participant HCP as HarvestChatPane
    participant Copilot as CopilotService
    participant RAG as RAGSearchPlugin

    User->>HNP: Select resource
    HNP->>HState: Set scope (course/section/resource IDs)
    HState-->>HCP: OnScopeChanged

    alt Resource not vectorized
        HNP->>VecSvc: VectorizeResourceAsync()
        VecSvc->>VecSvc: Split text into chunks
        VecSvc->>AzOAI: Generate embeddings (1536-dim)
        AzOAI-->>VecSvc: Embeddings
        VecSvc->>VecDB: Store NoteChunk records
    end

    User->>HCP: Ask question
    HCP->>Copilot: CreateSession(RAG tools)
    HCP->>Copilot: SendAsync(question)

    Copilot->>RAG: get_current_context()
    RAG-->>Copilot: Scope (course/section/resource IDs)

    Copilot->>RAG: search_notes(query, resourceId)
    RAG->>VecSvc: SearchAsync(query, resourceId)
    VecSvc->>AzOAI: Embed query
    VecSvc->>VecDB: Vector similarity search
    VecDB-->>VecSvc: Top-N chunks with scores
    VecSvc-->>RAG: Ranked results with citations
    RAG-->>Copilot: Context chunks

    Copilot-->>HCP: Answer with citations

    opt Generate flashcards
        Copilot->>RAG: save_flashcards(resourceId, cards[])
    end

    opt Generate podcast
        Copilot->>RAG: generate_podcast_audio(ssml)
    end

    HCP-->>User: Display answer + artifacts
```

### Flashcard & Podcast Generation

```mermaid
flowchart LR
    subgraph Input
        Resource["Selected Resource<br/>(markdown content)"]
    end

    subgraph Agent["Copilot Agent"]
        Analyze["Analyze content"]
        GenFC["Generate Q&A pairs"]
        GenSSML["Generate SSML script"]
    end

    subgraph FlashcardFlow["Flashcard Pipeline"]
        FCService["FlashcardService"]
        FCDb[(SQLite)]
        FCView["FlashcardViewer<br/>(flip cards)"]
    end

    subgraph PodcastFlow["Podcast Pipeline"]
        PodService["PodcastService"]
        AzSpeech["Azure Speech<br/>(SSML → WAV)"]
        PodDb[(SQLite)]
        PodPlay["PodcastPlayer<br/>(audio control)"]
    end

    Resource --> Analyze
    Analyze --> GenFC --> FCService --> FCDb --> FCView
    Analyze --> GenSSML --> PodService --> AzSpeech --> PodDb --> PodPlay
```

---

## State Management

```mermaid
flowchart TB
    subgraph Singletons["Singleton Services"]
        SS["StateService<br/><i>OnRefreshRequested<br/>OnResourceSelected</i>"]
        HS["HarvestStateService<br/><i>OnScopeChanged<br/>Flashcard/Podcast availability</i>"]
        NS["AppNotificationService<br/><i>Toast publishing</i>"]
    end

    subgraph Publishers["Event Publishers"]
        CRUD["NotesCRUDService<br/><i>SaveAndNotifyAsync()</i>"]
        HNP["HarvestNotesPane<br/><i>OnResourceSelected()</i>"]
    end

    subgraph Subscribers["Event Subscribers"]
        NotesTree["Notes.razor<br/><i>Reload tree</i>"]
        MDView["MarkDownView.razor<br/><i>Refresh content</i>"]
        HChat["HarvestChatPane<br/><i>Update scope</i>"]
    end

    CRUD -->|"RequestRefresh()"| SS
    HNP -->|"SetScope()"| HS
    SS -->|"OnRefreshRequested"| NotesTree
    SS -->|"OnResourceSelected"| MDView
    HS -->|"OnScopeChanged"| HChat
```

---

## Monaco Editor Architecture

```mermaid
flowchart TB
    subgraph RCL["KnowledgeLM.MonacoEditor (Razor Class Library)"]
        subgraph Components
            Editor["MonacoMarkdownEditor.razor"]
            Toolbar["MarkdownToolbar.razor"]
        end

        subgraph Memento["Memento Pattern (Undo/Redo)"]
            Session["TextEditorSession"]
            Originator["TextOriginator"]
            Mem["TextMemento"]
            History["TextHistory"]
        end

        subgraph Markdown["Markdown Pipeline"]
            Factory["MarkdownPipelineFactory"]
            Sanitizer["HtmlSanitizer"]
        end

        subgraph JS["JavaScript Interop (wwwroot/js/)"]
            EditorJS["monacoMarkdownEditor.js"]
            ToolbarJS["monacoMarkdownToolbar.js"]
            CDN["monaco-editor-pack.js<br/><i>CDN loader v0.52.2</i>"]
            Tokens["fluentUIDesignTokens.js"]
            CSS["cssClassHarvester.js"]
        end
    end

    Editor -->|"IJSRuntime"| EditorJS
    Toolbar -->|"IJSRuntime"| ToolbarJS
    EditorJS --> CDN
    Editor --> Session --> Originator --> Mem
    Session --> History
    Editor -->|"Render preview"| Factory
    Factory --> Sanitizer

    EditorJS -->|"[JSInvokable]<br/>OnContentChunk"| Editor
```

---

## DI Registration (MauiProgram.cs)

```mermaid
flowchart TB
    subgraph Builder["MauiProgram.CreateMauiApp()"]
        direction TB
        MAUI["UseMauiApp → AddMauiBlazorWebView"]
        Fluent["AddFluentUIComponents"]
        MonacoReg["AddMonacoEditorPack"]
        Audio["AddSingleton(AudioManager)"]
        Aspire["AddServiceDefaults → OpenTelemetry + Resilience"]

        subgraph KernelReg["Semantic Kernel"]
            K["AddKernel()"]
            P1["Plugin: NotesManagement"]
            P2["Plugin: Flashcards"]
            P3["Plugin: Podcasts"]
            P4["Plugin: RAGSearch"]
        end

        subgraph AIReg["AI Services"]
            Embed["AddAzureOpenAIEmbeddingGenerator"]
        end

        subgraph DataReg["Data"]
            DbCtx["AddDbContextFactory〈NotesContext〉<br/><i>SQLite: knowledgelm.db</i>"]
            EnsureDB["EnsureCreated + CreateAITables"]
        end

        subgraph SvcReg["Application Services"]
            Scoped["Scoped: CopilotService, NotesCRUDService,<br/>VectorStoreService, RAGSearchPlugin,<br/>FlashcardService, PodcastService"]
            Singleton["Singleton: StateService,<br/>HarvestStateService,<br/>AppNotificationService"]
        end
    end

    MAUI --> Fluent --> MonacoReg --> Audio --> Aspire --> KernelReg --> AIReg --> DataReg --> SvcReg
    K --> P1 & P2 & P3 & P4
```

---

## UI Component Map

```mermaid
graph TB
    subgraph App["MainLayout.razor"]
        Nav["NavMenu.razor<br/><i>FluentAppBar sidebar</i>"]

        subgraph HomePage["Home.razor — @page '/'"]
            direction LR
            HP1["Notes.razor<br/><i>FluentTreeView</i><br/>Course → Section → Resource"]
            HP2["GithubCLI.razor<br/><i>Chat interface</i><br/>Web harvesting agent"]
            HP3["MarkDownView.razor<br/><i>MonacoMarkdownEditor</i><br/>+ breadcrumbs"]
        end

        subgraph HarvestPage["Harvest.razor — @page '/Harvest'"]
            direction LR
            HV1["HarvestNotesPane.razor<br/><i>Resource tree</i><br/>Vectorization status"]
            HV2["HarvestChatPane.razor<br/><i>RAG Q&A</i><br/>Reasoning display"]
            HV3["FlashcardViewer.razor<br/><i>Flip card UI</i><br/>Q&A navigation"]
            HV4["PodcastPlayer.razor<br/><i>Audio player</i><br/>Play/pause/progress"]
        end
    end

    Nav --> HomePage
    Nav --> HarvestPage
```

---

## Web Resource Interception

`MainPage.xaml.cs` intercepts `WebResourceRequested` events to serve local files into the Blazor WebView:

```mermaid
sequenceDiagram
    participant WebView as BlazorWebView
    participant Handler as MainPage.xaml.cs
    participant Disk as Local File System

    WebView->>Handler: WebResourceRequested(/Notes/course/image.png)
    Handler->>Handler: Map URL path to disk path
    Handler->>Disk: Read file (Q:\Dev\{path})
    Disk-->>Handler: File bytes
    Handler->>Handler: Set Content-Type header
    Handler-->>WebView: WebResourceResponse (stream + headers)
```

---

## Design Patterns

| Pattern | Implementation | Purpose |
|---------|---------------|---------|
| **Memento** | `TextEditorSession` / `TextHistory` / `TextMemento` | Undo/redo in Monaco editor |
| **Factory** | `IDbContextFactory<NotesContext>` | Concurrency-safe EF Core contexts in Blazor |
| **Observer** | `StateService` / `HarvestStateService` events | Reactive UI updates across components |
| **Plugin** | `[KernelFunction]` attributes on services | Expose C# methods as AI-callable tools |
| **MCP** | Playwright MCP Server (JSON-RPC) | Extensible browser automation for AI agent |
| **Repository** | `NotesCRUDService` | Abstract data access behind kernel functions |
| **Memento + Command** | `MonacoMarkdownToolbar` JS actions | Editor formatting with undo support |

---

## Configuration

### appsettings.json

```json
{
  "AzureOpenAI": {
    "Endpoint": "",
    "ApiKey": "",
    "EmbeddingDeployment": "text-embedding-3-small"
  },
  "AzureSpeech": {
    "Key": "",
    "Region": ""
  }
}
```

### Hardcoded Values

> ⚠️ These values are currently hardcoded and should be moved to app settings:

| Setting | Value | Location |
|---------|-------|----------|
| Copilot CLI path | `C:\Users\manojdixit\AppData\Local\Microsoft\WinGet\Links\copilot.exe` | `GithubCLI.razor.cs` |
| Notes folder | `D:\Dev\LearnLoom` | `AgentPrompts.cs` |
| Web resource root | `Q:\Dev\` | `MainPage.xaml.cs` |
| Title bar color | `#6264A7` | `MainPage.xaml.cs` |

---

## Future Vision (Not Yet Implemented)

See `docs/implementation-plan.md` for the original design vision.

### Planned Features

- **Sit-Back Mode** — Agent autonomously navigates courses and generates notes
- **Read-Along Mode** — Real-time note capture while learner browses
- **Quiz Generation** — Auto-generate quizzes from notes
- **Android Reader** — Mobile app for consuming notes, chat, quiz (no scraping)

### Planned Capabilities

| Capability | Purpose | Status |
|------------|---------|--------|
| PDF Processing | PdfPig/iText7 text extraction | ❌ Planned |
| YouTube Transcripts | YouTube Data API v3 | ❌ Planned |
| Screenshot Analysis | Perceptual hashing for slide change detection | ❌ Planned |
| Authentication | Enterprise SSO for LinkedIn Learning, Pluralsight, etc. | ❌ Planned |

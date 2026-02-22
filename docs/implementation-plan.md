# KnowledgeLM — Implementation Plan

## App Name

**KnowledgeLM** — Plays on "LM" (Language Model) while conveying knowledge-first focus. Distinctive from NotebookLM, memorable, and positions the product clearly.

Other considered names: Cortex, ScribeMind, RecallForge, SynapseNotes.

---

## Problem Statement

Learners struggle to balance note-taking with content consumption. Existing tools (NotebookLM) only work with public/uploaded sources and don't support enterprise learning platforms (LinkedIn Learning, Pluralsight, SharePoint, Microsoft Learn). KnowledgeLM will be an AI-powered learning companion that automatically generates structured Markdown notes from any content source — public or private — while the learner focuses on learning.

---

## Architecture Overview — Agent-First Design

The **Copilot SDK agent is the orchestrator**. Playwright is registered as a **skill/tool** the agent invokes. Custom code only fills gaps Playwright can't cover (PDF parsing, YouTube API, perceptual hashing).

```
┌────────────────────────────────────────────────────────┐
│                  .NET MAUI Blazor Shell                 │
│  ┌───────────┐  ┌──────────┐  ┌─────────────────────┐  │
│  │ Notebook  │  │ Markdown │  │ Chat/Quiz/Podcast   │  │
│  │ Manager   │  │ Editor   │  │ UI                  │  │
│  └─────┬─────┘  └────┬─────┘  └────────┬────────────┘  │
│        └──────────────┴─────────────────┘              │
│                        │                               │
│  ┌─────────────────────┴─────────────────────────────┐  │
│  │        Copilot SDK Agent (Orchestrator)            │  │
│  │  Reasons, plans, invokes skills autonomously       │  │
│  │                                                    │  │
│  │  Skills/Tools:                                     │  │
│  │  🌐 Playwright Skill — navigate, screenshot,       │  │
│  │     extract, click, scroll, authenticate           │  │
│  │  📝 Note Management — create/append/read .md       │  │
│  │  📄 File Processing — PDF, YouTube, text (custom)  │  │
│  │  🔍 Screenshot Analysis — slide change detection   │  │
│  │  💬 Learning — chat, quiz, podcast generation      │  │
│  └────────────────────────────────────────────────────┘  │
│                        │                                │
│  ┌─────────────────────┴─────────────────────────────┐  │
│  │  Storage: SQLite (metadata) + File System (.md)    │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

> **Principle:** Playwright is a tool the agent uses, not a layer the app calls. The agent decides *when* to navigate, *what* to screenshot, *how* to structure notes.

---

## Project Structure

```
KnowledgeLM/
├── KnowledgeLM.sln
├── src/
│   ├── KnowledgeLM.App/                  # .NET MAUI Blazor app
│   │   ├── Components/                    # Blazor UI components
│   │   │   ├── NotebookManager/           # Notebook CRUD, source management
│   │   │   ├── MarkdownEditor/            # Obsidian-like MD editor
│   │   │   ├── Chat/                      # Chat with sources UI
│   │   │   ├── Quiz/                      # Quiz generation & interaction
│   │   │   └── Shared/                    # Layout, nav, common components
│   │   ├── Platforms/                     # MAUI platform-specific code
│   │   ├── MauiProgram.cs                 # DI + agent + skill registration
│   │   └── wwwroot/                       # Static assets
│   │
│   ├── KnowledgeLM.Core/                 # Core business logic (class library)
│   │   ├── Models/                        # Notebook, Note, Source, etc.
│   │   ├── Services/                      # Orchestration, storage service
│   │   └── Interfaces/                    # Abstractions
│   │
│   ├── KnowledgeLM.Skills/               # Agent skills (registered as AIFunction tools)
│   │   ├── PlaywrightSkill.cs             # 🌐 Browser: navigate, screenshot, extract, auth
│   │   ├── NoteManagementSkill.cs         # 📝 CRUD for .md notes + media
│   │   ├── FileProcessingSkill.cs         # 📄 PDF, YouTube, text (custom code)
│   │   ├── ScreenshotAnalysisSkill.cs     # 🔍 Slide change detection, vision
│   │   └── LearningInteractionSkill.cs    # 💬 Chat, quiz, podcast
│   │
│   └── KnowledgeLM.Storage/              # Storage layer (class library)
│       ├── SqliteDbContext.cs
│       ├── FileSystemNoteStore.cs
│       └── MediaStore.cs
│
├── tests/
│   ├── KnowledgeLM.Core.Tests/
│   └── KnowledgeLM.Skills.Tests/
│
└── docs/
    ├── implementation-plan.md
    ├── architecture.md
    ├── feasibility.md
    └── milestones.md
```

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| App Framework | .NET 9 MAUI Blazor Hybrid |
| UI Components | Blazor (Razor components, CSS isolation) |
| Markdown | Markdig (parsing) + CodeMirror/Monaco (editor) |
| AI/Agent | GitHub Copilot CLI SDK (orchestrator) → Microsoft.Extensions.AI |
| Agent Skills | Playwright Skill, Note Skill, File Skill, Screenshot Skill, Learning Skill |
| Browser Automation | Playwright for .NET (registered as agent skill) |
| Storage | SQLite (via EF Core) + File System (.md files + media) |
| PDF Processing | PdfPig or iText7 (custom FileProcessingSkill) |
| YouTube | YouTube Data API v3 (custom FileProcessingSkill) |
| SharePoint | Microsoft Graph SDK (custom skill) |
| TTS (Podcast) | Azure Cognitive Services Speech / OpenAI TTS |
| Testing | xUnit + Playwright Test + Moq |

---

## Note Generation Pipeline (Agent-Driven)

The agent orchestrates the entire pipeline — no hardcoded connector logic:

```
User adds source → Agent reasons about source type
  → Agent invokes PlaywrightSkill (navigate, auth, extract, screenshot)
  → Agent invokes FileProcessingSkill (PDF/YouTube/text — when Playwright can't help)
  → Agent invokes ScreenshotAnalysisSkill (detect slide changes, skip duplicates)
  → Agent synthesizes structured Markdown notes using its own LLM reasoning
  → Agent invokes NoteManagementSkill (save .md + embedded screenshot references)
```

The agent handles LinkedIn Learning, Pluralsight, Microsoft Learn, SharePoint, and any web content **through the same PlaywrightSkill** — it just reasons differently about how to navigate each platform.

### Screenshot & Frame Capture Strategy
1. **Timed capture**: Screenshot every N seconds during video playback
2. **Change detection**: Compare consecutive frames using perceptual hashing (pHash) — only save when content visually changes (new slide/topic)
3. **AI-assisted**: Send screenshot to vision model to determine if it's a meaningful capture (slide vs. speaker face)

### Authentication Strategy (Private Sources)
- SSO/Corporate credentials via Playwright's persistent browser context (cookies/session reuse)
- Store auth state in encrypted local storage
- Support manual login flow where user authenticates in the browser, tool captures the session

---

## Android Considerations

- Playwright won't run on Android directly
- Android app scope for prototype: **notes reader, chat, quiz, podcast player**
- Scraping/capture happens on Windows, notes sync via shared file system or local network
- Future: companion service architecture for cross-device scraping

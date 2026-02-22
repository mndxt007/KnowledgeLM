# KnowledgeLM — Milestones

## Milestone 1: Foundation & Skeleton

Set up the solution structure, MAUI Blazor app shell, core models, and local storage.

| Task | Description |
|------|-------------|
| Create .NET solution | Solution with projects: App, Core, Skills, Storage. Add project references. |
| Define domain models | `Notebook`, `Note`, `Source`, `SourceType`, `CaptureMode` in `KnowledgeLM.Core/Models/` |
| Implement storage layer | SQLite context + file system note store in `KnowledgeLM.Storage/` |
| MAUI Blazor app shell | Navigation shell with sidebar: Notebooks, Editor, Chat |
| Integrate Markdown editor | Blazor Markdown editor component (Markdig + CodeMirror/Monaco) |

---

## Milestone 2: Agent + Playwright Skill (Core Engine)

Wire up the Copilot SDK agent and register Playwright as its primary browser skill.

| Task | Description |
|------|-------------|
| Configure Copilot SDK agent | Set up agent with system prompt, DI registration in MauiProgram.cs |
| Build PlaywrightSkill | Register AIFunction tools: NavigateToUrl, TakeScreenshot, ExtractPageContent, ClickElement, ScrollPage, WaitForContent, GetPageLinks, TypeText, AuthenticateWithSSO |
| Build NoteManagementSkill | Register AIFunction tools: CreateNote, AppendToNote, ReadNote, SaveScreenshot |
| Agent chat UI | Basic chat interface to interact with the agent and trigger tasks |

---

## Milestone 3: Custom Skills (Gaps Playwright Can't Fill)

Build custom skills for capabilities that require specialized code.

| Task | Description |
|------|-------------|
| FileProcessingSkill | ExtractPdfText (PdfPig), ParseYouTubeTranscript (API), ReadTextFile |
| ScreenshotAnalysisSkill | DetectSlideChange (perceptual hashing), AnalyzeScreenshot (vision model) |
| SharePoint skill | Microsoft Graph SDK integration for SharePoint content extraction |

---

## Milestone 4: Sit-Back & Relax Mode

The agent autonomously navigates courses and generates notes.

| Task | Description |
|------|-------------|
| Sit-Back orchestration | Agent prompt engineering for autonomous course navigation + note generation |
| Progress UI | Real-time progress display: current page, screenshots taken, notes generated |
| Source management UI | Add sources to notebooks, trigger Sit-Back mode, view extraction status |

---

## Milestone 5: Read-Along Mode

Real-time note generation while the learner consumes content.

| Task | Description |
|------|-------------|
| Read-Along capture | Agent monitors browser side-panel/overlay for user's current position |
| Real-time note streaming | Agent streams notes as content is consumed, appending to active `.md` file |
| Capture controls | UI: Start/Pause/Stop capture, manual "capture this" button |

---

## Milestone 6: Chat, Quiz & Podcast

Interactive learning features built on generated notes.

| Task | Description |
|------|-------------|
| LearningInteractionSkill | ChatWithNotes, GenerateQuiz, GeneratePodcast AIFunction tools |
| Chat UI | RAG-based chat: embed notes → vector search → contextual answers with citations |
| Quiz UI | Interactive quiz interface with scoring |
| Podcast generation | Conversational podcast script from notes → TTS audio |

---

## Milestone 7: Polish & Android

Refine UX and extend to Android.

| Task | Description |
|------|-------------|
| Android reader app | Android MAUI build: notes viewer, chat, quiz (no Playwright) |
| UX polish | UI/UX refinement, theming, responsive layout |
| Error handling & resilience | Robust error handling, retry logic, user feedback |

---

## Dependency Graph

```
Milestone 1 (Foundation)
    ├── setup-solution
    │   ├── core-models → storage-layer
    │   ├── app-shell → markdown-editor
    │
Milestone 2 (Agent + Playwright Skill)  ← Core Engine
    │   ├── copilot-agent-setup ← setup-solution
    │   ├── playwright-skill ← copilot-agent-setup
    │   ├── note-management-skill ← copilot-agent-setup + storage-layer
    │   └── agent-chat-ui ← app-shell + copilot-agent-setup
    │
Milestone 3 (Custom Skills)
    │   ├── file-processing-skill ← copilot-agent-setup
    │   ├── screenshot-analysis-skill ← copilot-agent-setup
    │   └── sharepoint-skill ← copilot-agent-setup
    │
Milestone 4 (Sit-Back Mode)
    │   ├── sitback-orchestration ← playwright-skill + note-management-skill
    │   ├── progress-ui ← app-shell
    │   └── source-management-ui ← app-shell + copilot-agent-setup
    │
Milestone 5 (Read-Along)
    │   ├── readalong-capture ← playwright-skill
    │   ├── realtime-notes ← note-management-skill + readalong-capture
    │   └── capture-controls ← app-shell
    │
Milestone 6 (Chat/Quiz/Podcast)
    │   ├── learning-skill ← copilot-agent-setup + storage-layer
    │   ├── chat-ui ← app-shell + learning-skill
    │   ├── quiz-ui ← app-shell + learning-skill
    │   └── podcast-gen ← learning-skill
    │
Milestone 7 (Polish)
    │   ├── android-reader ← app-shell + markdown-editor
    │   ├── ux-polish ← app-shell
    │   └── error-handling ← all skills
```

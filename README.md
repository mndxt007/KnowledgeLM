# KnowledgeLM

> AI-powered learning companion that automatically generates structured Markdown notes from any content source — public or private — so learners can focus on learning and use notes for recall and knowledge retention.

## Vision

Like how a table of contents helps reach a topic, notes help recall knowledge. Imagine revising before a cert exam — with an agent that takes great-quality notes, the learner can fully focus on the content and later use the notes for retention.

## Key Features

### 📚 Enterprise/Private Knowledge Scraper
- Retains NotebookLM-style sources: YouTube, PDF, TXT attachments
- Adds private sources: LinkedIn Learning, Pluralsight, SharePoint, Microsoft Learn
- Auto-screenshots slides/video frames and creates structured notes

### 🎯 Two Capture Modes
- **Read-Along Mode**: Notes generated in real-time while the learner consumes content
- **Sit-Back & Relax Mode**: Agent navigates courses and creates notes autonomously

### 💬 Notebooks + Chat + Learning
- Chat with sources (RAG-based)
- Generate podcasts, quizzes from notes
- NotebookLM-style interactive learning

### ✏️ Markdown Editor
- All notes stored as `.md` files
- Obsidian-like editing experience
- Full control over note structure

## Tech Stack

| Layer | Technology |
|-------|-----------|
| App Framework | .NET MAUI Blazor Hybrid (Windows + Android) |
| AI/Agent | GitHub Copilot CLI SDK (orchestrator) |
| Browser Skill | Playwright for .NET (registered as agent AIFunction tool) |
| Storage | Local SQLite + File System (offline-first) |
| Markdown | Markdig + CodeMirror/Monaco editor |

### Agent-First Architecture
The Copilot SDK agent is the orchestrator. Playwright is a **skill/tool** the agent invokes for browser automation, screenshots, and content extraction. Custom code is only written for capabilities Playwright can't provide (PDF parsing, YouTube API, perceptual hashing).

## Getting Started

See [docs/](docs/) for implementation plan and architecture.

## License

Private — Personal Use

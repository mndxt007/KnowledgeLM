---
marp: true
theme: uncover
paginate: true
backgroundColor: "#0d1117"
color: "#e6edf3"
style: |
  @import url('https://fonts.googleapis.com/css2?family=JetBrains+Mono:ital,wght@0,100..800;1,100..800&display=swap');

  :root {
    --color-brand: #58a6ff;
    --color-accent: #7ee787;
    --color-warn: #f0883e;
    --color-subtle: #8b949e;
    --color-bg: #0d1117;
    --color-card: #161b22;
    --color-border: #30363d;
  }

  * {
    font-family: 'JetBrains Mono', monospace !important;
  }

  section {
    font-size: 22px;
    line-height: 1.5;
    padding: 40px 60px;
  }

  h1 {
    color: var(--color-brand);
    font-size: 42px;
    font-weight: 800;
    margin-bottom: 10px;
  }

  h2 {
    color: var(--color-accent);
    font-size: 32px;
    font-weight: 700;
    margin-bottom: 8px;
  }

  h3 {
    color: var(--color-warn);
    font-size: 24px;
    font-weight: 600;
  }

  strong {
    color: var(--color-brand);
  }

  em {
    color: var(--color-subtle);
    font-style: normal;
  }

  code {
    background: var(--color-card);
    color: var(--color-accent);
    padding: 2px 8px;
    border-radius: 4px;
    font-size: 0.85em;
  }

  a {
    color: var(--color-brand);
    text-decoration: none;
  }

  table {
    width: 100%;
    border-collapse: collapse;
    font-size: 18px;
    margin-top: 10px;
  }

  th {
    background: var(--color-card);
    color: var(--color-brand);
    padding: 10px 14px;
    text-align: left;
    border-bottom: 2px solid var(--color-border);
  }

  td {
    padding: 8px 14px;
    border-bottom: 1px solid var(--color-border);
  }

  tr:hover td {
    background: rgba(88, 166, 255, 0.05);
  }

  ul, ol {
    margin-top: 8px;
  }

  li {
    margin-bottom: 6px;
  }

  blockquote {
    border-left: 4px solid var(--color-brand);
    padding: 8px 20px;
    background: var(--color-card);
    border-radius: 0 8px 8px 0;
    margin: 16px 0;
    font-size: 20px;
  }

  .columns {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 30px;
    align-items: start;
  }

  .card {
    background: var(--color-card);
    border: 1px solid var(--color-border);
    border-radius: 12px;
    padding: 20px;
  }

  .stat {
    font-size: 36px;
    font-weight: 800;
    color: var(--color-warn);
  }

  .stat-label {
    font-size: 16px;
    color: var(--color-subtle);
  }

  .tag {
    display: inline-block;
    background: var(--color-card);
    border: 1px solid var(--color-border);
    color: var(--color-brand);
    padding: 4px 12px;
    border-radius: 20px;
    font-size: 14px;
    margin: 2px;
  }

  .mermaid {
    font-size: 14px;
  }

  footer {
    color: var(--color-subtle);
    font-size: 14px;
  }

  section::after {
    color: var(--color-subtle);
    font-size: 14px;
  }
---

<!-- _paginate: false -->
<!-- _class: lead -->

# 🎓 LearnLoom

## AI-Agent-Powered Knowledge Management

*Capture · Organize · Harvest — from any content source*

<br/>

<div style="display: flex; gap: 12px; justify-content: center; flex-wrap: wrap;">
<span class="tag">GitHub Copilot SDK</span>
<span class="tag">Semantic Kernel</span>
<span class="tag">Playwright MCP</span>
<span class="tag">.NET MAUI Blazor</span>
<span class="tag">Azure OpenAI</span>
</div>

<br/>

*March 2026*

---

# The Problem

## Knowledge consumed ≠ Knowledge retained

Learners and professionals constantly consume content across **fragmented platforms** — but balancing note-taking with active learning is a losing battle.

<div class="columns">
<div>

### 😫 Pain Points

- **70–90%** of knowledge forgotten within a week
- Note-taking **competes** with comprehension
- **1.8 hrs/day** lost searching for information
- **4–6** fragmented learning platforms
- No tool captures from **enterprise** sources

</div>
<div>

### 🚫 Existing Tools Fall Short

- NotebookLM → **public sources only**
- No SSO/enterprise platform access
- Manual upload required
- No structured note generation
- No active learning features

</div>
</div>

---

# The Solution

## LearnLoom — Full-Lifecycle Knowledge Management

> An **AI agent** that captures, organizes, and helps you retain knowledge — so you can focus entirely on learning.

<div class="columns">
<div>

### 📚 Capture
AI agent navigates, screenshots, extracts, and generates structured Markdown notes **autonomously**

### 📝 Organize
Obsidian-like editor with full control. Notes as `.md` files with screenshots and metadata

</div>
<div>

### 🧠 Harvest
Chat with notes via **RAG**, generate **flashcards** for self-assessment, create **AI podcasts** for on-the-go revision

### 🌐 Any Source
LinkedIn Learning · Pluralsight · Microsoft Learn · SharePoint · YouTube · PDFs · Web

</div>
</div>

---

# Capture

## AI Agent Does the Note-Taking

<div class="columns">
<div>

### 🎬 Read-Along Mode
Notes generated **in real-time** while you consume content
- You browse at your pace
- Agent captures alongside you
- Screenshots at every key moment

### 🛋️ Sit-Back & Relax Mode
Agent navigates courses **autonomously**
- Provide a URL, walk away
- Full course captured end-to-end
- Structured folder hierarchy created

</div>
<div>

![w:560](pitch_resources/agent_capture.svg)

</div>
</div>

---

# Organize

## Obsidian-Like Markdown Editor

<div class="columns">
<div>

### ✨ Features

- **Monaco Editor** with split live preview
- Full **Markdown toolbar** — bold, italic, lists, tables, code blocks
- **Course → Section → Resource** hierarchy
- **TreeView** navigation with context menus
- Screenshots and media embedded inline
- **Undo/Redo** with Memento pattern
- Local-first — all files on disk

</div>
<div>

![w:560](pitch_resources/agent_organize.svg)

</div>
</div>

---

# Harvest

## Turn Notes into Active Learning

<div class="columns">
<div>

### 💬 RAG-Powered Q&A
Ask questions about your notes — get answers with **source citations** from your own content

### 🃏 AI Flashcards
Auto-generated **Q&A cards** for spaced repetition and self-assessment

### 🎙️ AI Podcasts
**Text-to-Speech** podcast episodes generated from your notes for on-the-go revision

</div>
<div>

![w:400](pitch_resources/agent_harvest.svg)

</div>
</div>

---

# Agent-First Architecture

## The AI Agent Is the Brain

![w:600](pitch_resources/agent_first_architecture.svg)

> The agent **reasons, plans, and invokes** registered skills. Custom code only exists for capabilities the agent + Playwright can't provide.

---

# Architecture Deep Dive

## System Component Diagram

![w:800](pitch_resources/architecture_deep_dive.svg)

---

# Tech Stack

<div class="columns">
<div>

| Layer | Technology |
|-------|-----------|
| **App Framework** | .NET MAUI Blazor Hybrid |
| **UI Components** | Fluent UI Blazor v4 |
| **AI Orchestrator** | GitHub Copilot SDK |
| **AI Plugins** | Semantic Kernel v1.73 |
| **Browser Automation** | Playwright MCP Server |
| **Vector Search** | SqliteVec + Azure OpenAI |

</div>
<div>

| Layer | Technology |
|-------|-----------|
| **Markdown Editor** | Monaco Editor + Markdig |
| **Speech / TTS** | Azure Cognitive Services |
| **Database** | EF Core 10 + SQLite |
| **Hosting** | .NET Aspire |
| **Observability** | OpenTelemetry |
| **Target Platforms** | Windows + Android |

</div>
</div>

<br/>

<div style="text-align: center;">

*All AI capabilities are provided through the* **Copilot SDK agent** *— no direct LLM API calls in application code*

</div>

---

# Impact

## Solving Real Problems at Scale

<div class="columns">
<div>

<div class="card" style="text-align: center; padding: 12px;">
<div class="stat" style="font-size: 28px;">70–90%</div>
<div class="stat-label">knowledge forgotten in a week</div>
<div style="color: var(--color-accent); font-size: 16px;">→ Structured notes + spaced retrieval</div>
</div>

<div class="card" style="text-align: center; padding: 12px; margin-top: 10px;">
<div class="stat" style="font-size: 28px;">1.8 hrs</div>
<div class="stat-label">per day lost searching</div>
<div style="color: var(--color-accent); font-size: 16px;">→ Centralized notebooks + semantic search</div>
</div>

<div class="card" style="text-align: center; padding: 12px; margin-top: 10px;">
<div class="stat" style="font-size: 28px;">$31.5B</div>
<div class="stat-label">annual enterprise knowledge loss</div>
<div style="color: var(--color-accent); font-size: 16px;">→ Persistent knowledgeBase and Seach</div>
</div>

</div>
<div>

<div class="card" style="text-align: center; padding: 12px;">
<div class="stat" style="font-size: 28px;">Zero</div>
<div class="stat-label">cognitive load on learner</div>
<div style="color: var(--color-accent); font-size: 16px;">→ Agent captures notes autonomously</div>
</div>

<div class="card" style="text-align: center; padding: 12px; margin-top: 10px;">
<div class="stat" style="font-size: 28px;">4–6</div>
<div class="stat-label">fragmented platforms unified</div>
<div style="color: var(--color-accent); font-size: 16px;">→ Single tool for all content sources</div>
</div>

<div class="card" style="text-align: center; padding: 12px; margin-top: 10px;">
<div class="stat" style="font-size: 28px;">Active</div>
<div class="stat-label">learning, not passive consumption</div>
<div style="color: var(--color-accent); font-size: 16px;">→ Quizzes, podcasts, conversational review</div>
</div>

</div>
</div>

---

<!-- _class: lead -->

# Demo Time! 🚀

<br/>

<div style="text-align: center; font-size: 24px;">

**Capture** → Point the agent at a Microsoft Learn module

**Organize** → Browse generated notes in the editor

**Harvest** → Ask questions, generate flashcards, listen to podcast

</div>

<br/>

<div style="display: flex; gap: 40px; justify-content: center;">
<div style="text-align: center;">
<div style="font-size: 48px;">📚</div>
<div>Capture</div>
</div>
<div style="text-align: center; font-size: 48px; color: var(--color-subtle);">→</div>
<div style="text-align: center;">
<div style="font-size: 48px;">📝</div>
<div>Organize</div>
</div>
<div style="text-align: center; font-size: 48px; color: var(--color-subtle);">→</div>
<div style="text-align: center;">
<div style="font-size: 48px;">🧠</div>
<div>Harvest</div>
</div>
</div>

---

<!-- _paginate: false -->
<!-- _class: lead -->

# Thank You

## LearnLoom — Learn More, Note Less

<br/>

<div style="display: flex; gap: 12px; justify-content: center; flex-wrap: wrap;">
<span class="tag">.NET MAUI Blazor</span>
<span class="tag">GitHub Copilot SDK</span>
<span class="tag">Semantic Kernel</span>
<span class="tag">Playwright MCP</span>
<span class="tag">Azure OpenAI</span>
<span class="tag">Azure Speech</span>
<span class="tag">SqliteVec</span>
<span class="tag">.NET Aspire</span>
</div>

<br/>

*🎓 AI agent captures the notes. You capture the knowledge.*

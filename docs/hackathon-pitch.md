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

*Hackathon Demo · March 2026*

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

```mermaid
sequenceDiagram
    actor User
    participant Agent as Copilot Agent
    participant Browser as Playwright
    participant DB as Notes DB

    User->>Agent: Add source URL
    Agent->>Browser: Navigate & authenticate
    Agent->>Browser: Snapshot page structure
    Agent->>DB: Create course + sections
    loop Each section
        Agent->>Browser: Navigate to section
        Agent->>Browser: Screenshot slides
        Agent->>Agent: Generate markdown
        Agent->>DB: Save resources
    end
    Agent-->>User: ✅ Notes ready!
```

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

```mermaid
graph TB
    subgraph Editor["Monaco Markdown Editor"]
        Split["Split Pane View"]
        TB2["Formatting Toolbar"]
        Preview["Live HTML Preview"]
    end

    subgraph Tree["Notes TreeView"]
        C["📁 Course"]
        S["  📂 Section"]
        R["    📄 Resource.md"]
        I["    🖼️ Screenshot.png"]
    end

    subgraph Storage["Local Storage"]
        SQLite["SQLite DB<br/>(metadata)"]
        FS["File System<br/>(.md + images)"]
    end

    Tree --> Editor
    Editor --> Storage
```

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

```mermaid
flowchart TB
    Notes["📝 Your Notes"]

    Notes --> Vec["Vectorize<br/>(Azure OpenAI<br/>Embeddings)"]
    Vec --> VecDB["SqliteVec<br/>Vector Store"]

    Notes --> FC["Generate<br/>Flashcards"]
    FC --> FCView["🃏 Flip Cards<br/>Study UI"]

    Notes --> Pod["Generate<br/>SSML Script"]
    Pod --> TTS["Azure Speech<br/>TTS Engine"]
    TTS --> Audio["🎙️ Podcast<br/>Player"]

    VecDB --> RAG["Semantic<br/>Search"]
    RAG --> Chat["💬 Q&A Chat<br/>with Citations"]
```

</div>
</div>

---

# Agent-First Architecture

## The AI Agent Is the Brain

```mermaid
graph TB
    User["👤 User"] --> UI["🖥️ MAUI Blazor UI"]

    UI --> CopilotSDK["🤖 GitHub Copilot SDK<br/><i>Orchestrator</i>"]

    CopilotSDK --> SK["Semantic Kernel<br/><i>Plugin Host</i>"]

    SK --> NM["📚 NotesManagement<br/>14 kernel functions<br/><i>CRUD operations</i>"]
    SK --> RAG2["🔍 RAGSearch<br/>Vector similarity<br/><i>Semantic search</i>"]
    SK --> FC2["🃏 Flashcards<br/>Generate & save<br/><i>Study cards</i>"]
    SK --> POD2["🎙️ Podcasts<br/>SSML + TTS<br/><i>Audio generation</i>"]

    CopilotSDK --> MCP["🌐 Playwright MCP<br/><i>Browser Automation</i>"]
    MCP --> Edge["Microsoft Edge"]

    NM --> DB["💾 SQLite + File System"]
    RAG2 --> VDB["🧮 SqliteVec + Azure OpenAI"]
    POD2 --> AZ["☁️ Azure Speech Service"]
```

> The agent **reasons, plans, and invokes** registered skills. Custom code only exists for capabilities the agent + Playwright can't provide.

---

# Architecture Deep Dive

## System Component Diagram

```mermaid
graph LR
    subgraph UI["UI Layer"]
        Home["Home Page<br/><i>3-pane workspace</i>"]
        Harvest["Harvest Page<br/><i>Study assistant</i>"]
    end

    subgraph Agent["AI Layer"]
        Copilot["Copilot SDK"]
        Kernel["Semantic Kernel"]
        PW["Playwright MCP"]
        Embed["Azure OpenAI<br/>Embeddings"]
        Speech["Azure Speech"]
    end

    subgraph Services["Service Layer"]
        CRUD["NotesCRUD"]
        Vector["VectorStore"]
        RAG3["RAGSearch"]
        Flash["Flashcard"]
        Pod["Podcast"]
        State["StateService"]
    end

    subgraph Data["Data Layer"]
        EF["EF Core<br/>SQLite"]
        SVec["SqliteVec"]
        FS2["File System<br/>.md + images"]
    end

    Home --> Copilot
    Harvest --> Copilot
    Copilot --> Kernel --> Services
    Copilot --> PW
    Vector --> Embed
    Pod --> Speech
    CRUD --> EF & FS2
    Vector --> SVec
    Flash --> EF
    Pod --> EF
    CRUD --> State --> UI
```

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

<div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 20px; margin-top: 20px;">

<div class="card" style="text-align: center;">
<div class="stat">70–90%</div>
<div class="stat-label">knowledge forgotten in a week</div>
<div style="color: var(--color-accent); margin-top: 8px;">→ Structured notes + spaced retrieval via quizzes and RAG Q&A</div>
</div>

<div class="card" style="text-align: center;">
<div class="stat">1.8 hrs</div>
<div class="stat-label">per day lost searching</div>
<div style="color: var(--color-accent); margin-top: 8px;">→ Centralized notebooks with semantic search</div>
</div>

<div class="card" style="text-align: center;">
<div class="stat">$31.5B</div>
<div class="stat-label">annual enterprise knowledge loss</div>
<div style="color: var(--color-accent); margin-top: 8px;">→ Persistent knowledge base that survives transitions</div>
</div>

</div>

<div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 20px; margin-top: 16px;">

<div class="card" style="text-align: center;">
<div class="stat">Zero</div>
<div class="stat-label">cognitive load on learner</div>
<div style="color: var(--color-accent); margin-top: 8px;">→ Agent captures notes autonomously</div>
</div>

<div class="card" style="text-align: center;">
<div class="stat">4–6</div>
<div class="stat-label">fragmented platforms unified</div>
<div style="color: var(--color-accent); margin-top: 8px;">→ Single tool for all content sources</div>
</div>

<div class="card" style="text-align: center;">
<div class="stat">Active</div>
<div class="stat-label">learning, not passive consumption</div>
<div style="color: var(--color-accent); margin-top: 8px;">→ Quizzes, podcasts, and conversational review</div>
</div>

</div>

---

# Why LearnLoom Matters

## AI-Forward Engineering

<div class="columns">
<div>

### 🏗️ Architecture Philosophy

The AI agent **isn't a feature bolted on** — it's the **core orchestrator**.

- Agent **reasons and plans** the capture workflow
- Agent **invokes tools** (Playwright, CRUD, Search)
- Agent **decides** what to capture and how to structure
- Custom code only where agent + Playwright can't reach

### 🎯 Built With AI, For AI-Assisted Learning

Every interaction — from capture to quiz to podcast — flows through the intelligent agent.

</div>
<div>

```mermaid
graph TB
    subgraph Traditional["Traditional Approach"]
        T1["Hard-coded scraper"]
        T2["Manual note templates"]
        T3["Static quiz builder"]
        T4["Separate tools"]
    end

    subgraph LearnLoom["LearnLoom Approach"]
        A["🤖 Copilot Agent"]
        A --> S1["Adaptive navigation"]
        A --> S2["Intelligent note structure"]
        A --> S3["Context-aware Q&A"]
        A --> S4["Unified experience"]
    end

    style Traditional fill:#1a1a2e,stroke:#30363d
    style LearnLoom fill:#0d2137,stroke:#58a6ff
```

</div>
</div>

---

# Data Flow

## End-to-End Knowledge Pipeline

```mermaid
flowchart LR
    subgraph Capture["📥 CAPTURE"]
        URL["Source URL"]
        URL --> PW2["Playwright MCP<br/>Navigate + Auth"]
        PW2 --> SS["Screenshots"]
        PW2 --> Text["Text Extraction"]
    end

    subgraph Organize["📝 ORGANIZE"]
        SS --> MD2["Markdown Notes"]
        Text --> MD2
        MD2 --> DB2["SQLite<br/>(Course/Section/Resource)"]
        MD2 --> FS3["Disk<br/>(.md + .png)"]
    end

    subgraph Harvest["🧠 HARVEST"]
        MD2 --> Chunk["Chunk + Embed"]
        Chunk --> VecDB2["Vector DB"]
        VecDB2 --> Search["Semantic Search"]
        Search --> QA2["💬 Q&A Chat"]
        MD2 --> Cards["🃏 Flashcards"]
        MD2 --> Podcast2["🎙️ Podcast"]
    end

    style Capture fill:#1a2332,stroke:#58a6ff
    style Organize fill:#1a2a1a,stroke:#7ee787
    style Harvest fill:#2a1a1a,stroke:#f0883e
```

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

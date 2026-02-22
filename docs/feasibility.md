# KnowledgeLM — Feasibility Analysis

## ✅ Feasible (High Confidence)

| Component | Why |
|-----------|-----|
| .NET MAUI Blazor hybrid app | Mature framework, good Windows support, improving Android support. Blazor components reusable across platforms. |
| Playwright browser automation | Excellent .NET SDK, headless/headed modes, screenshot support, multi-browser. Works well on Windows. |
| Local SQLite + file system | Battle-tested for offline-first apps. MAUI has native SQLite support. |
| Markdown editor in Blazor | Multiple open-source Blazor Markdown editors exist (e.g., Markdig for parsing, Monaco/CodeMirror for editing). |
| YouTube/PDF/TXT source ingestion | Well-established: YouTube transcript APIs, PDF parsing libraries (PdfPig, iText), straightforward text processing. |
| AI note generation via Copilot SDK | SDK is stable and supports function tooling, streaming, multi-turn conversations. |
| Chat with sources (RAG) | Standard RAG pattern: chunk notes → embed → vector search → LLM generation. Microsoft.Extensions.AI supports embeddings. |

---

## ⚠️ Feasible with Caveats

| Component | Risk | Mitigation |
|-----------|------|------------|
| LinkedIn Learning scraping | ToS restrictions; DOM structure changes frequently; SSO auth complexity | Use corporate-authorized access only; build resilient selectors; abstract connector interface for easy updates |
| Pluralsight scraping | Similar ToS concerns; video-heavy content | Same as above; focus on transcript/slide extraction over video frames |
| SharePoint content extraction | Graph API is better than scraping; complex auth flows | Use Microsoft Graph SDK instead of Playwright for SharePoint |
| MS Learn multi-page navigation | Content structure varies (modules, units, exercises) | Build a flexible page walker; use AI to detect content boundaries |
| Auto-screenshot of video frames | Frame detection (when slides change) requires CV or heuristics | Start with timed intervals; later add perceptual hashing for change detection |
| Podcast generation | Requires TTS (Text-to-Speech) service | Use Azure Cognitive Services Speech SDK or OpenAI TTS API |
| Android + Playwright | Playwright doesn't run natively on Android | Playwright runs as a background service on a companion PC, or use a hosted scraping service; Android app consumes results |

---

## ❌ Significant Challenges

| Component | Challenge | Recommendation |
|-----------|-----------|----------------|
| Playwright on Android | Chromium engine can't be bundled in mobile apps easily | For prototype: Windows-only scraping, Android as a notes reader/chat client. Later: build a companion service. |
| Real-time "Read-Along" mode | Requires detecting learner's viewport/progress in real-time while they browse | Start with manual trigger (user clicks "capture this section"); evolve to browser extension or side-panel approach. |

---

## Risk Register

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Platform ToS violations from scraping | Project viability | Medium | Personal use only; respect robots.txt; rate limiting; user-authenticated sessions |
| Playwright Android incompatibility | Reduced mobile feature set | High | Windows-only scraping; Android as reader client |
| LLM hallucination in notes | Note quality | Medium | Ground notes in extracted text; show source references; user can edit |
| Frequent DOM changes on learning platforms | Connector breakage | High | Selector abstraction layer; AI-assisted selector recovery; community-maintained selectors |
| Copilot SDK API changes | Build breakage | Low | Pin SDK versions; abstract behind interfaces |

---

## Legal & Ethical Considerations

### Terms of Service
- Scraping LinkedIn Learning and Pluralsight may violate ToS
- This tool should only be used with content the user has legitimate access to, for personal/educational use

### Copyright
- Generated notes are derivative works
- Keep them as personal study aids, not redistributable content

### Recommendation
Frame this as a "personal learning assistant" that works with content the user is already authorized to access.

<div align="center">

<br/>

# PPT Semantic Editor

### AI-powered PowerPoint editing at the semantic level.
### Upload → Understand → Rewrite → Export.

<br/>

[![Live Demo](https://img.shields.io/badge/🚀%20Live%20Demo-ppt--editor--9liw.onrender.com-22c55e?style=for-the-badge)](https://ppt-editor-9liw.onrender.com/)
&nbsp;
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
&nbsp;
[![React 19](https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://react.dev/)
&nbsp;
[![Docker](https://img.shields.io/badge/Docker-multi--stage-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
&nbsp;
[![Groq](https://img.shields.io/badge/LLM-Llama%203.3%2070B-F55036?style=for-the-badge)](https://groq.com/)

<br/>

</div>

---

## The Problem

Most AI tools that edit PowerPoint files treat slides as blobs of text. They don't understand that a 14-word bullet and a 3-word callout have different structural roles. They don't know which elements are in a graph, a layout placeholder, or a content region. When they rewrite text that's too long for its text box, the layout breaks silently — and you only notice after downloading.

**PPT Semantic Editor solves this properly.**

It parses the `.pptx` OpenXML, builds a typed Abstract Syntax Tree, classifies every element by its semantic role, detects graphs and architecture diagrams, then hands the LLM a structured, word-count-aware representation — not raw XML or plain text. Edits are applied to the semantic model and rendered back to OpenXML. The LLM never touches XML.

---

## Live Demo

> **[https://ppt-editor-9liw.onrender.com/](https://ppt-editor-9liw.onrender.com/)**
>
> Free-tier cold start: first request may take ~30 seconds. Every subsequent request is instant.

---

## What It Does

Upload a `.pptx`. Describe what you want in plain English. Download a rewritten presentation.

Under the hood, every stage of the pipeline is inspectable in the UI:

| Stage | What you can see and do |
|---|---|
| **Upload** | Drag-and-drop; pipeline runs automatically; stage-by-stage status |
| **OpenXML View** | Raw shape metadata, text runs, layout info per slide |
| **Semantic Tree** | Typed element tree — titles, body text, callouts, chart data, placeholders |
| **Graph View** | Visual graph of inter-slide relationships and detected diagram structures |
| **AI Analysis** | LLM reads the semantic tree and returns a structured content analysis |
| **Edit Plan** | Type a plain-English instruction; AI returns a per-element action plan with confidence scores |
| **Diff View** | Side-by-side before/after of every changed text element |
| **Apply & Validate** | Execute approved actions; structural validation runs automatically |
| **Version History** | Every apply step is snapshotted; roll back to any version in one click |
| **Render & Download** | Semantic model is written back to a fully valid `.pptx` |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         React 19 SPA                            │
│   Upload · OpenXML · SemanticTree · Agent · Diff · Renderer     │
└────────────────────────────┬────────────────────────────────────┘
                             │ REST (same origin)
┌────────────────────────────▼────────────────────────────────────┐
│                    ASP.NET Core 8 Web API                       │
│                                                                 │
│  ┌─────────────┐   ┌──────────────┐   ┌─────────────────────┐  │
│  │   Parser    │──▶│  Semantic    │──▶│       Agent         │  │
│  │             │   │              │   │                     │  │
│  │ OpenXML →   │   │ AST Builder  │   │ Groq API            │  │
│  │ ParsedInfo  │   │ SlideClassif │   │ llama-3.3-70b       │  │
│  │ MediaExtract│   │ GraphDetect  │   │ EditPlanAgent       │  │
│  └─────────────┘   │ TreeBuilder  │   │ AnalysisAgent       │  │
│                    └──────────────┘   └──────────┬──────────┘  │
│                                                   │             │
│  ┌─────────────────────────────┐   ┌─────────────▼──────────┐  │
│  │         Renderer            │◀──│    ActionExecutor       │  │
│  │                             │   │    ValidationEngine     │  │
│  │  SemanticModel → .pptx      │   │    VersionHistory       │  │
│  └─────────────────────────────┘   └────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Why not just send the XML to the LLM?

Three reasons:

1. **Token cost.** A 30-slide deck can be 500KB of XML. The semantic tree strips it to the meaningful nodes — a fraction of the size.
2. **Safety.** LLMs make XML mistakes. A single malformed tag breaks the entire `.pptx`. Keeping the LLM in the semantic layer means it only touches structured data it can't accidentally corrupt.
3. **Constraint enforcement.** The LLM is given each element's exact word-count ceiling. If it exceeds it, the text box overflows the slide. The prompt engineering enforces this as a hard constraint, and the validation engine catches violations before rendering.

---

## Engineering Highlights

**Multi-stage Docker build** — Node 20 builds the Vite bundle, the .NET SDK publishes the API, and the slim `aspnet:8.0` runtime image contains only the compiled output. Total image size is a fraction of what a naive build produces.

**Single-origin serving** — In production, the .NET API serves the compiled React app from `wwwroot` using `UseStaticFiles` + `MapFallbackToFile`. No separate static host, no CORS configuration needed in production, no proxying.

**Configurable everything** — Storage path, CORS origins, Groq model, and API key are all environment-variable driven. The app runs correctly with zero config changes between local dev, Docker, and Render.

**Per-IP rate limiting** — The three Groq-calling endpoints are protected by ASP.NET Core's built-in fixed-window rate limiter (15 req/min). This required zero external dependencies — it ships with .NET 8.

**Session-scoped pipeline state** — Every upload gets a `sessionId`. All pipeline stages (parsing, AST, semantic, analysis, edit plan, render) are tracked per session. The UI polls stage status and renders a live progress view.

**Prompt engineering that actually works** — The edit plan prompt enforces: topic changes vs. style rewrites are fundamentally different operations; word count is a hard ceiling per element; no whitespace padding; all elements on a targeted slide must be rewritten, not just the first one.

---

## Tech Stack

| | Technology | Why |
|---|---|---|
| **Frontend** | React 19, TypeScript 6, Vite 8 | Latest stable ecosystem; fast HMR in dev |
| **Styling** | Tailwind CSS 4 | Utility-first; no runtime overhead |
| **State** | Zustand | Minimal boilerplate for session-scoped global state |
| **Routing** | React Router 7 | File-based routing for 12 feature pages |
| **Backend** | ASP.NET Core 8 | High-performance, strongly-typed, great OpenXML support |
| **OpenXML** | DocumentFormat.OpenXml | Official Microsoft SDK — full fidelity |
| **LLM** | Groq / Llama 3.3 70B | Fast inference; structured JSON output; generous free tier |
| **Container** | Docker multi-stage | Reproducible builds; minimal runtime image |
| **Hosting** | Render | Git-push deploys; automatic PORT injection |
| **Rate Limiting** | ASP.NET Core built-in | No extra package; fixed-window per-IP |
| **Tests** | xUnit | Pipeline unit tests independent of API and LLM |

---

## Project Structure

```
ppt_editor/
├── Dockerfile                          # 3-stage: Node → .NET SDK → aspnet runtime
├── .dockerignore
├── frontend/
│   └── src/
│       ├── pages/                      # 12 feature pages
│       ├── components/                 # Layout, graph visualization
│       ├── services/api.ts             # Typed REST client
│       ├── store/                      # Zustand state
│       └── types/                      # Shared TypeScript interfaces
└── backend/
    ├── PptSemanticEditor.sln
    ├── src/
    │   ├── PptSemanticEditor.Api       # Web API, controllers, DI config
    │   ├── PptSemanticEditor.Core      # Domain models, interfaces
    │   ├── PptSemanticEditor.Parser    # OpenXML → ParsedInfo
    │   ├── PptSemanticEditor.Semantic  # AST, classifier, graph detector
    │   ├── PptSemanticEditor.Agent     # Groq integration, edit plan agent
    │   └── PptSemanticEditor.Renderer  # SemanticModel → .pptx
    └── tests/
        └── PptSemanticEditor.Tests     # xUnit pipeline tests
```

---

## Getting Started

### Docker (recommended)

```bash
docker build -t ppt-editor .

docker run -p 8080:8080 \
  -e PORT=8080 \
  -e Groq__ApiKey=your_groq_api_key \
  ppt-editor
```

Open **http://localhost:8080**

### Local development

```bash
# Backend
cd backend
dotnet run --project src/PptSemanticEditor.Api

# Frontend (separate terminal)
cd frontend
npm install && npm run dev
```

Add your Groq key to `backend/src/PptSemanticEditor.Api/appsettings.Development.json`:
```json
{ "Groq": { "ApiKey": "your-key-here" } }
```

### Run tests

```bash
cd backend && dotnet test tests/PptSemanticEditor.Tests
```

---

## Configuration

| Variable | Description | Default |
|---|---|---|
| `PORT` | Port Kestrel binds to (auto-injected by Render) | `5000` |
| `Groq__ApiKey` | Groq API key | *(required)* |
| `STORAGE_PATH` | Storage directory for uploads and renders | `App_Data/uploads` |
| `AllowedOrigins__0` | Additional CORS origin (split-host deploys only) | — |

No secrets are hardcoded anywhere in the repository.

---

## API Reference

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/upload` | Upload `.pptx`; full pipeline runs automatically |
| `GET` | `/api/upload/{id}/status` | Live pipeline stage status |
| `GET` | `/api/openxml/{id}` | Raw OpenXML parse result |
| `GET` | `/api/semantic/{id}/tree` | Semantic element tree |
| `GET/PUT` | `/api/semantic/{id}/json` | Get or update the semantic model |
| `POST` | `/api/agent/{id}/analyze` | LLM content analysis |
| `POST` | `/api/agent/{id}/edit-plan` | Generate per-element edit plan from a prompt |
| `POST` | `/api/agent/{id}/apply-edits` | Apply and validate edit actions |
| `POST` | `/api/renderer/{id}/render` | Render edited model to `.pptx` |
| `GET` | `/api/renderer/{id}/download` | Download the rendered file |
| `GET` | `/api/validation/{id}/history` | Full version history |
| `POST` | `/api/validation/{id}/rollback/{v}` | Roll back to version `v` |
| `GET` | `/api/pipeline/{id}/diff` | Before/after element diff |
| `GET` | `/api/health` | Health check |

---

## Roadmap

- [ ] Persistent storage (S3 / Render Disk) for multi-session file durability
- [ ] Streaming LLM responses for real-time edit feedback in the UI
- [ ] Image and chart element editing
- [ ] Export to PDF
- [ ] Auth layer for multi-user deployments

---

## License

MIT — do whatever you want with it.

---

<div align="center">

Built end-to-end as a personal project to explore what *actually good* AI-assisted document editing looks like when you do it at the right abstraction level.

**[Try it live →](https://ppt-editor-9liw.onrender.com/)**

</div>

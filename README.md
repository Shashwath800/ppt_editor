<div align="center">

# 🎨 PPT Semantic Editor

**AI-powered PowerPoint editing — understand, rewrite, and export presentations with LLM intelligence.**

[![Live Demo](https://img.shields.io/badge/Live%20Demo-ppt--editor--9liw.onrender.com-brightgreen?style=for-the-badge&logo=render)](https://ppt-editor-9liw.onrender.com/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge&logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6.0-3178C6?style=for-the-badge&logo=typescript)](https://www.typescriptlang.org/)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?style=for-the-badge&logo=docker)](https://www.docker.com/)
[![Groq](https://img.shields.io/badge/LLM-Groq%20%2F%20Llama%203.3-F55036?style=for-the-badge)](https://groq.com/)

</div>

---

## ✨ What is this?

PPT Semantic Editor is a full-stack application that lets you upload a `.pptx` file, understand its structure at a semantic level, and apply AI-generated edits — all through a clean web interface. Instead of manipulating XML directly, the app builds a structured **semantic tree** of your presentation and lets an LLM reason over it.

**The result:** you describe what you want in plain English, and the app rewrites your slides — respecting word counts, structure, and formatting — then renders a new, downloadable `.pptx`.

---

## 🚀 Live Demo

> **[https://ppt-editor-9liw.onrender.com/](https://ppt-editor-9liw.onrender.com/)**

> ⚠️ Running on Render's free tier — first load may take ~30 seconds to cold-start.

---

## 🧠 How It Works

```
Upload .pptx
     │
     ▼
OpenXML Parser          ← Extracts raw slide shapes, text runs, layout info
     │
     ▼
AST Builder             ← Builds a typed Abstract Syntax Tree of the presentation
     │
     ▼
Semantic Tree Builder   ← Classifies slides, detects graphs/architecture diagrams,
     │                     resolves relationships between elements
     ▼
LLM Agent (Groq)        ← Analyzes content, generates structured edit plans
     │                     with per-element rewrite actions
     ▼
Action Executor         ← Applies approved edits to the semantic model
     │
     ▼
Validation Engine       ← Checks structural consistency of the modified tree
     │
     ▼
OpenXML Renderer        ← Writes the edited semantic model back to a valid .pptx
     │
     ▼
Download modified .pptx
```

---

## 🎯 Features

| Feature | Description |
|---|---|
| **Upload & Parse** | Drag-and-drop a `.pptx`; the full pipeline runs automatically on upload |
| **OpenXML Viewer** | Inspect raw slide-level XML structure and shape metadata |
| **Semantic Tree** | Browse the typed semantic model — slides, elements, classifications |
| **Graph Viewer** | Visualize inter-slide relationships and detected diagram structures |
| **AI Analysis** | One-click LLM analysis of your presentation's content and structure |
| **Edit Plan** | Describe a change in plain English; the AI generates a structured action plan per element |
| **Diff Viewer** | Side-by-side before/after diff of every text element changed |
| **Apply & Validate** | Execute the edit plan and validate structural integrity in one step |
| **Version History** | Snapshot every apply step; roll back to any previous version |
| **Render & Download** | Export the modified presentation as a fully valid `.pptx` |
| **Rate Limited** | Groq-calling endpoints are rate-limited to 15 req/min per IP |

---

## 🏗️ Architecture

### Backend — .NET 8 Web API

```
backend/
├── src/
│   ├── PptSemanticEditor.Api        # ASP.NET Core 8 Web API (entry point)
│   ├── PptSemanticEditor.Core       # Shared models & interfaces
│   ├── PptSemanticEditor.Parser     # OpenXML parsing + media extraction
│   ├── PptSemanticEditor.Semantic   # AST builder, semantic tree, graph detection
│   ├── PptSemanticEditor.Agent      # LLM integration (Groq), edit plan generation
│   └── PptSemanticEditor.Renderer   # Semantic model → .pptx writer
└── tests/
    └── PptSemanticEditor.Tests      # xUnit pipeline tests
```

### Frontend — React 19 + Vite + TypeScript

```
frontend/src/
├── pages/       # 12 feature pages (Upload, Analysis, EditPlan, Diff, Renderer, ...)
├── components/  # Shared layout + graph visualization components
├── services/    # Typed API client (api.ts)
├── store/       # Zustand global state
└── types/       # Shared TypeScript interfaces
```

### Key design decisions

- **Single-origin deploy** — The API serves the compiled React app from `wwwroot`. No CORS issues in production; no separate static hosting needed.
- **Session-based state** — Each uploaded file gets a `sessionId`; all pipeline stages are tracked per session in an in-memory store.
- **Semantic, not XML** — Edits are made to the semantic model and rendered back to OpenXML, keeping the LLM far away from raw XML.
- **LLM prompt engineering** — The edit plan prompt enforces hard constraints: word count ceilings, no whitespace padding, full rewrites for topic changes vs. style changes.

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 19, TypeScript 6, Vite 8, Tailwind CSS 4, Zustand, React Router 7 |
| Backend | ASP.NET Core 8, C# 12 |
| AI / LLM | Groq API — `llama-3.3-70b-versatile` |
| OpenXML | DocumentFormat.OpenXml (Microsoft) |
| Containerisation | Docker (3-stage multi-stage build) |
| Hosting | Render (free tier) |
| Rate Limiting | ASP.NET Core built-in `AddRateLimiter` (fixed window, 15 req/min) |

---

## 🐳 Run with Docker

```bash
# Build
docker build -t ppt-editor .

# Run
docker run -p 8080:8080 \
  -e PORT=8080 \
  -e Groq__ApiKey=<your-groq-api-key> \
  ppt-editor
```

Open **http://localhost:8080** — the React app is served at `/`, API at `/api/*`.

---

## 💻 Run Locally (dev mode)

**Prerequisites:** .NET 8 SDK, Node.js 20+

```bash
# Terminal 1 — Backend
cd backend
dotnet run --project src/PptSemanticEditor.Api

# Terminal 2 — Frontend
cd frontend
npm install
npm run dev
```

Frontend: http://localhost:5173 · Backend: http://localhost:5000

Set your Groq API key in `backend/src/PptSemanticEditor.Api/appsettings.Development.json`:
```json
{
  "Groq": {
    "ApiKey": "your-key-here"
  }
}
```

---

## ⚙️ Configuration

All configuration is environment-variable driven — no secrets in source control.

| Env Var | Description | Default |
|---|---|---|
| `PORT` | Port Kestrel listens on (injected by Render automatically) | `5000` |
| `Groq__ApiKey` | Your Groq API key | *(required)* |
| `STORAGE_PATH` | Where uploaded / rendered `.pptx` files are stored | `App_Data/uploads` |
| `AllowedOrigins__0` | Extra CORS origins (for split-host deploys) | *(none)* |

For Render deployment, set these as **Environment Variables** in the dashboard. Never commit a real API key.

---

## 🧪 Tests

```bash
cd backend
dotnet test tests/PptSemanticEditor.Tests
```

The xUnit test suite covers the parsing and semantic pipeline stages independently of the API and LLM.

---

## 📁 API Reference

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/upload` | Upload a `.pptx`; runs full parse pipeline |
| `GET` | `/api/upload/{sessionId}/status` | Get pipeline stage status |
| `GET` | `/api/openxml/{sessionId}` | Raw OpenXML parse result |
| `GET` | `/api/semantic/{sessionId}/tree` | Semantic tree |
| `GET` | `/api/semantic/{sessionId}/json` | Semantic JSON |
| `PUT` | `/api/semantic/{sessionId}/json` | Update semantic JSON manually |
| `POST` | `/api/agent/{sessionId}/analyze` | Run LLM analysis |
| `POST` | `/api/agent/{sessionId}/edit-plan` | Generate AI edit plan from a prompt |
| `POST` | `/api/agent/{sessionId}/apply-edits` | Apply and validate edit actions |
| `POST` | `/api/renderer/{sessionId}/render` | Render edited semantic model to `.pptx` |
| `GET` | `/api/renderer/{sessionId}/download` | Download the rendered `.pptx` |
| `GET` | `/api/validation/{sessionId}/history` | Version history |
| `POST` | `/api/validation/{sessionId}/rollback/{version}` | Roll back to a previous version |
| `GET` | `/api/pipeline/{sessionId}/diff` | Before/after diff |
| `GET` | `/api/health` | Health check |

---

## 🗺️ Roadmap

- [ ] Persistent storage (Render Disk / S3) for uploaded files
- [ ] Multi-user sessions with authentication
- [ ] Streaming LLM responses for real-time edit feedback
- [ ] Image and chart element editing
- [ ] Export to PDF

---

## 📄 License

MIT

---

<div align="center">
Built with ❤️ using .NET 8, React 19, and Groq's Llama 3.3
</div>

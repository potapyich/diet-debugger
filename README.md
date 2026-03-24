# Diet Debugger

AI-powered nutrition analysis app. Photograph meals, get instant calorie/macro breakdowns, and understand behavioral patterns across days, weeks, and months.

**Stop counting calories. Start understanding your nutrition.**

## Stack

- **Backend:** ASP.NET Core 10 (C#) + PostgreSQL
- **Frontend:** React + Vite + TypeScript, served via nginx
- **AI:** pluggable provider (OpenAI or Claude) for vision and text analysis

## Local Setup

### Prerequisites

- Docker and Docker Compose
- An OpenAI or Anthropic API key

### 1. Configure environment

```bash
cp .env.example .env
```

Edit `.env`:

```env
DB_PASSWORD=choose-a-strong-password
JWT_SECRET=a-random-string-of-at-least-32-characters
AI_MODEL_PROVIDER=openai          # or "claude"
AI_API_KEY=sk-...                 # your API key
```

### 2. Start all services

```bash
docker compose up -d
```

This starts:
- `db` — PostgreSQL 16 on a named volume
- `api` — ASP.NET Core API on port 5000 (internal), migrations run on startup
- `client` — nginx serving the React app on port **3000**

### 3. Open the app

```
http://localhost:3000
```

The client proxies all `/api/*` requests to the backend automatically.

### Health check

```bash
curl http://localhost:3000/api/health
# {"status":"ok","db":"connected"}
```

### Stopping

```bash
docker compose down          # stop containers, keep volumes
docker compose down -v       # stop containers and delete all data
```

## Development

### Backend (hot reload)

```bash
cd src/DietDebugger.Api
dotnet watch run
```

Requires a local PostgreSQL instance or set `DB_CONNECTION_STRING` in `appsettings.Development.json`.

### Frontend (hot reload)

```bash
cd client
pnpm install
pnpm dev
```

Vite dev server runs on `http://localhost:5173` and proxies `/api` to `http://localhost:5000`.

### Running tests

```bash
dotnet test
```

## Environment Variables

| Variable | Required | Description |
|---|---|---|
| `DB_PASSWORD` | yes | PostgreSQL password |
| `JWT_SECRET` | yes | JWT signing secret (min 32 chars) |
| `AI_MODEL_PROVIDER` | yes | `openai` or `claude` |
| `AI_API_KEY` | yes | API key for the chosen provider |
| `FILE_STORAGE_PATH` | no | Temp upload directory (default: `/data/uploads`) |

# Plan: Diet Debugger

## Architecture Overview

```
┌─────────────────────────────────────────┐
│           React + Vite SPA              │
│  Day | Week | Analytics | Profile       │
└──────────────┬──────────────────────────┘
               │ REST API (JSON)
┌──────────────▼──────────────────────────┐
│         ASP.NET Core API                │
│  ┌─────────────┐  ┌──────────────────┐  │
│  │ Controllers │  │  Background Jobs │  │
│  └──────┬──────┘  └────────┬─────────┘  │
│         │                  │            │
│  ┌──────▼──────────────────▼─────────┐  │
│  │           AI Agent Pipeline       │  │
│  │  Vision | Feedback | Summary |    │  │
│  │  Weekly | Monthly | PatternBreak  │  │
│  └──────────────────────────────────┘  │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│            PostgreSQL                   │
└─────────────────────────────────────────┘
```

**Key principles:**
- AI agents behind interfaces — model is swappable
- Level 1 agents run synchronously in-request (<2s)
- Level 2 agents run async — triggered lazy on screen open or by scheduler
- Photos are temporary; only structured data is persisted

---

## Work Blocks

### Block 1: Foundation
**What:** Runnable skeleton — API, DB, auth, user model
**Why first:** Everything else depends on this. No features without a working backend and user context.
**Includes:**
- Solution structure (API project, Domain, Infrastructure, Application layers)
- PostgreSQL + EF Core setup, initial migrations
- User entity + profile fields
- Email/password auth with JWT (register, login, refresh)
- Health check endpoint
- React + Vite scaffold, routing, auth context, protected routes
**Dependencies:** none

---

### Block 2: Meal Logging Core
**What:** Full meal logging loop — photo/text input → AI analysis → user confirmation → save
**Why second:** Core value prop. Everything downstream (analytics, patterns) requires meal data.
**Includes:**
- `Meal` entity + CRUD API
- `FoodVisionAgent` interface + implementation (photo → structured data; text → structured data)
- Async processing flow: upload → poll for result (or SSE)
- Confidence score handling — "we're not sure" UX branch
- User correction / edit before save
- `MealFeedbackAgent` (Level 1 rule-based) — 1–2 instant insights post-save
- Frontend: "+" bottom sheet, camera/gallery/text flows, result screen, edit screen
**Dependencies:** Block 1

---

### Block 3: Day View
**What:** Full Day tab — today's meals, running totals, goal comparison, day insights
**Why third:** First thing users see after logging; necessary for basic retention loop.
**Includes:**
- Day state screen (calories consumed vs target, macro rings, meal list)
- `DailySummaryAgent` (Level 2 async) — triggered on first open of Day analytics
- Insight invalidation on meal edit/delete → regenerate on next open
- Toast notification system (top-right, macOS-style) — "insights ready" trigger
- Profile-incomplete nudge on UI (persistent banner if no profile)
**Dependencies:** Block 2

---

### Block 4: Profile & Goals
**What:** User profile, goals, preferences
**Why fourth:** Unlocks personalized analysis in all Level 2 agents. Can be skipped by user but needed for full value.
**Includes:**
- Profile API (weight, height, age, sex, goal, diet type, feedback tone, language)
- Goal API (daily calorie target, optional macro targets)
- Profile screen (frontend)
- Anonymous mode handling — profile-gated vs general analysis branching in agents
**Dependencies:** Block 1 (profile fields exist from Block 1, this completes the UX)

---

### Block 5: Weekly Analytics & Calendar
**What:** Week tab with patterns, calendar grid, weekly report
**Why fifth:** Second-most important analytics layer; needs several days of meal data to be useful.
**Includes:**
- `WeeklyPatternAgent` (Level 2 async) — triggered on first open of Week tab
- `WeeklyReport` entity + API
- Week tab frontend (summary stats, pattern insights, calendar grid)
- Calendar grid: day-level color status (green / yellow / red) based on DailySummary
- Lazy generation + toast notification when ready
**Dependencies:** Block 3 (DailySummary must exist for calendar coloring)

---

### Block 6: Pattern Breaker
**What:** Behavioral pattern detection — alerts and positive reinforcement
**Why sixth:** Needs meal + daily history to detect patterns meaningfully.
**Includes:**
- `PatternEvent` entity + API
- `PatternBreakerAgent` — runs after each meal save and daily summary generation
- Built-in pattern library (9 patterns from PRD)
- User-reported habits input (profile section)
- Alert UI: toast notifications + Pattern Breaker feed in Analytics tab
**Dependencies:** Block 3, Block 4

---

### Block 7: Monthly Strategy & Analytics Tab
**What:** Analytics & Recommendations tab — monthly strategy, Pattern Breaker feed, forecast
**Why seventh:** Requires a full week+ of data; makes sense after weekly layer is stable.
**Includes:**
- `MonthlyStrategyAgent` (Level 2 async)
- Analytics & Recommendations tab frontend
- Forecast section ("at current pace: -X kg/month")
- Pattern Breaker history feed
**Dependencies:** Block 5, Block 6

---

### Block 8: Polish & Production Readiness
**What:** Error handling, empty states, loading states, edge cases, deployment
**Why last:** Polish on top of working features.
**Includes:**
- All loading / error / empty states across all screens
- No-profile fallback messaging throughout
- Low-confidence meal flow edge cases
- API error handling + user-facing error messages
- Input validation (frontend + backend)
- Storage abstraction (local → interface ready for cloud swap)
- Environment config, deployment pipeline
**Dependencies:** All previous blocks

---

## Sequence Rationale

1. **Foundation first** — auth and DB are the backbone; nothing is buildable without them
2. **Meal logging before analytics** — analytics are meaningless without data; logging is the core habit loop
3. **Day view before profile** — users should see value immediately; profile deepens value but isn't a gate
4. **Weekly before Pattern Breaker** — patterns need aggregated history (DailySummary) to fire correctly
5. **Monthly last** — lowest frequency, needs the most data, highest LLM cost — validate cheaper layers first
6. **Polish last** — avoid polishing half-built features

---

## Risks & Assumptions

| Risk | Mitigation |
|------|-----------|
| AI food recognition accuracy is insufficient for user trust | Make correction UX frictionless; low-confidence branch is a first-class flow |
| LLM response time for Level 2 agents too slow for UX | Async + lazy generation + toast notification decouples wait from navigation |
| Vision API costs too high at scale | Abstract behind interface; can switch models or add caching layer |
| Users don't fill in profile → personalization never activates | Persistent UI nudge; show clear diff between general vs personalized insights |
| Pattern Breaker fires too often → alert fatigue | Rate-limit alerts per pattern; require N occurrences before first trigger |
| React SPA vs camera access on mobile browser | Use native file input with `capture="camera"` attribute; works in mobile browsers |

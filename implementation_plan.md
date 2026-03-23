# Implementation Plan: Diet Debugger

## Block 1: Foundation

### 1.1 Solution structure
**What:** Create the .NET solution with layered architecture: `DietDebugger.Api`, `DietDebugger.Application`, `DietDebugger.Domain`, `DietDebugger.Infrastructure`. Set up project references, shared NuGet packages (EF Core, MediatR, FluentValidation).
**Acceptance:** `dotnet build` passes; project reference graph is correct (Api → Application → Domain; Infrastructure → Application)
**Notes:** Keep Domain free of EF Core dependencies. Application layer owns interfaces; Infrastructure implements them.

### 1.2 PostgreSQL + EF Core setup
**What:** Add EF Core with Npgsql provider to Infrastructure. Configure `AppDbContext`. Create and apply initial migration (empty — just verifies connection).
**Acceptance:** `dotnet ef migrations add Initial` succeeds; `dotnet ef database update` creates the DB; health check endpoint returns 200 with DB connectivity confirmed.
**Notes:** Use `dotnet-ef` tool locally. Connection string via environment variable / `appsettings.Development.json`.

### 1.3 User entity + JWT auth
**What:** Create `User` entity (Id, Email, PasswordHash, CreatedAt). Implement register (`POST /auth/register`), login (`POST /auth/login`), and token refresh (`POST /auth/refresh`) endpoints with JWT access + refresh token pair.
**Acceptance:** Register → Login returns JWT; protected endpoint returns 401 without token, 200 with valid token; refresh endpoint issues new access token.
**Notes:** Use `BCrypt.Net` for password hashing. Store refresh tokens in DB with expiry.

### 1.4 React + Vite scaffold
**What:** Create React + Vite project (`/client`). Set up: React Router v6 (routes: `/`, `/day`, `/week`, `/analytics`, `/profile`, `/auth`), Axios with JWT interceptor (auto-attach token, auto-refresh on 401), protected route wrapper, global toast context (stub).
**Acceptance:** App starts; unauthenticated user is redirected to `/auth`; authenticated user reaches `/day`; toast context renders without errors.
**Notes:** Use `pnpm`. No UI library decision needed yet — plain CSS modules acceptable for scaffold.

---

## Block 2: Meal Logging Core

### 2.1 Meal entity + migration
**What:** Create `Meal` entity: `Id`, `UserId`, `LoggedAt`, `Calories`, `ProteinG`, `FatG`, `CarbsG`, `Ingredients` (JSON), `PortionEstimate`, `ConfidenceScore`, `Source` (enum: Photo/Text/Manual), `Notes`. Migration + `DbContext` config.
**Acceptance:** Migration applies cleanly; EF can insert and query a `Meal` record.

### 2.2 FoodVisionAgent interface + implementation
**What:** Define `IFoodVisionAgent` interface in Application layer: `Task<FoodVisionResult> AnalyzeAsync(FoodVisionInput input)` where input is either image bytes or free text. Implement against a real vision model (e.g., OpenAI GPT-4o or Claude). Return: ingredients list, calories, macros, portion estimate, confidence (0–1).
**Acceptance:** Integration test: send a real food photo → get structured result with calories > 0 and confidence score.
**Notes:** Wrap API call with timeout (10s). Log prompt + response for debugging. Model selected via config — not hardcoded.

### 2.3 Async meal analysis API
**What:** Implement `POST /meals/analyze` — accepts image upload or text body, enqueues analysis job, returns `jobId`. Implement `GET /meals/analyze/{jobId}` — returns status (`pending` / `ready` / `failed`) and result when ready. Store job state in DB (`AnalysisJob` table).
**Acceptance:** Upload photo → poll until `ready` → get structured result; poll after failure → get `failed` with error message.
**Notes:** Use a simple in-process background worker (IHostedService queue) for MVP; no Hangfire needed yet.

### 2.4 Meal CRUD API
**What:** `POST /meals` (save confirmed meal), `GET /meals/{id}`, `PUT /meals/{id}` (edit), `DELETE /meals/{id}` (hard delete), `GET /meals?date=YYYY-MM-DD` (list for a day). All scoped to authenticated user.
**Acceptance:** Full CRUD works; user cannot access another user's meals (403); date filter returns correct subset.

### 2.5 MealFeedbackAgent (Level 1)
**What:** Implement rule-based `MealFeedbackAgent` in Application layer. Runs synchronously after meal save. Input: saved meal + today's running totals + user profile (if exists). Output: status (`ok`/`warning`/`issue`) + 1–2 insight strings. Rules: daily fat % threshold, protein threshold, calorie overage, etc.
**Acceptance:** Unit tests cover all rules; saving a high-fat meal returns `warning` + insight text; saving a balanced meal returns `ok`.
**Notes:** No LLM call here. If profile is absent, use generic thresholds (2000 kcal/day reference).

### 2.6 Frontend: add meal flow
**What:** "+" FAB on Day tab → bottom sheet with three options: Take Photo (native file input `capture="camera"`), Choose from Gallery (file input), Enter Text (text area). On submit: call `POST /meals/analyze`, show spinner, poll `GET /meals/analyze/{jobId}` every 1.5s until ready or failed.
**Acceptance:** Full flow works on mobile browser (iOS Safari + Android Chrome). Spinner shown during processing. Error message shown on failure.

### 2.7 Frontend: meal result + edit screen
**What:** After analysis completes: show result screen with ingredients list (editable), calories, macros, confidence badge. If confidence < 0.7: show yellow banner "We're not sure — please check or retake the photo". User can edit any field. Save button calls `POST /meals`. After save: show MealFeedback card (status + insight). Back button cancels without saving.
**Acceptance:** Low-confidence result shows banner; edits are reflected in saved meal; feedback card appears post-save.

---

## Block 3: Day View

### 3.1 DailySummary entity + API
**What:** Create `DailySummary` entity: `UserId`, `Date`, `TotalCalories`, `TotalProteinG`, `TotalFatG`, `TotalCarbsG`, `Insights` (JSON array), `GeneratedAt`, `IsStale` (bool). `GET /daily-summary?date=` returns summary or 404 if not yet generated. `POST /daily-summary/invalidate?date=` marks it stale (called on meal edit/delete).
**Acceptance:** API returns 404 for a day with no summary; invalidation sets `IsStale = true`.

### 3.2 DailySummaryAgent
**What:** Implement `DailySummaryAgent` (Level 2, LLM). Input: list of meals for the day + user profile/goals. Output: insights array (2–4 strings). Triggered by `GET /daily-summary` when summary is missing or stale — generate and persist, then return.
**Acceptance:** Integration test: save 3 meals → call GET daily-summary → insights array has 2–4 non-empty strings; second call returns cached result (no new LLM call — check via log/counter).
**Notes:** Use a simple mutex/lock per (userId, date) to avoid parallel generation on concurrent requests.

### 3.3 Insight invalidation on meal edit/delete
**What:** In meal `PUT` and `DELETE` handlers: after successful operation, call `DailySummaryService.InvalidateAsync(userId, date)`. Frontend: after successful meal edit/delete, mark local day summary as stale.
**Acceptance:** Edit a meal → GET daily-summary returns stale flag or triggers regeneration; old insights are not shown after edit.

### 3.4 Toast notification system
**What:** Implement global toast component in React (top-right, auto-dismiss 4s, macOS-style). Events: `InsightsReady`, `PatternAlert`, `StreakAlert`. Toast context exposes `showToast(type, message)`. Wire up: after DailySummary loads for the first time today → show "Today's insights are ready".
**Acceptance:** `showToast('InsightsReady', '...')` renders toast in top-right; dismisses after 4s; multiple toasts stack.

### 3.5 Frontend: Day tab
**What:** Day tab shows: calories ring (consumed / target), macro bars (P/F/C), meal list sorted by time (each card: name, calories, macros, edit/delete actions), DailySummary insights section (skeleton loader while generating), "+" FAB. If no profile: persistent yellow banner "Fill in your profile for personalized insights" with link to Profile tab.
**Acceptance:** Renders correctly with 0 meals (empty state); renders with meals; insights section shows skeleton then content; profile nudge appears when profile incomplete.

---

## Block 4: Profile & Goals

### 4.1 Profile API
**What:** Add profile fields to `User`: `WeightKg`, `HeightCm`, `Age`, `Sex`, `DietType`, `FeedbackTone`, `PreferredLanguage`, `ProfileCompletedAt`. `GET /profile`, `PUT /profile`. `ProfileCompletedAt` is set when all required fields are filled.
**Acceptance:** PUT with full profile sets `ProfileCompletedAt`; GET returns updated fields; partial PUT (missing fields) does not set `ProfileCompletedAt`.

### 4.2 Goals API
**What:** Create `UserGoal` entity: `UserId`, `GoalType` (enum: Cut/Maintain/Bulk), `DailyCalorieTarget`, `ProteinTargetG`(nullable), `FatTargetG` (nullable), `CarbsTargetG` (nullable), `ActiveSince`. `GET /goals`, `PUT /goals`.
**Acceptance:** PUT creates or updates goal; GET returns current goal; Day tab calorie ring uses target from goal if present.

### 4.3 Frontend: Profile tab
**What:** Profile tab with form: weight, height, age, sex (select), goal type (radio), diet type (select), feedback tone (radio: neutral/direct/harsh), language (select). Save button. Completion progress indicator ("Profile 3/7 fields complete"). Link to Goals sub-section.
**Acceptance:** Form saves to API; completion indicator updates; language change persists to localStorage and affects AI response language header.

### 4.4 Anonymous mode branching in agents
**What:** In `DailySummaryAgent`, `WeeklyPatternAgent`, `MealFeedbackAgent`: check if user has `ProfileCompletedAt`. If null: use generic analysis (no personalized targets, generic thresholds). If set: use profile + goals for personalized analysis. The difference should be visible in insight text.
**Acceptance:** Unit test: same meals, with and without profile → different insight text; no null reference errors in either path.

---

## Block 5: Weekly Analytics & Calendar

### 5.1 WeeklyReport entity + API
**What:** Create `WeeklyReport` entity: `UserId`, `WeekStartDate` (Monday), `Patterns` (JSON), `CalendarDayStatuses` (JSON: date → `green`/`yellow`/`red`), `GeneratedAt`. `GET /weekly-report?weekStart=` — returns report or triggers generation (same lazy pattern as DailySummary). `POST /weekly-report/invalidate?weekStart=`.
**Acceptance:** GET returns 404 before any meals logged that week; returns report after generation; invalidation resets stale flag.

### 5.2 WeeklyPatternAgent
**What:** Implement `WeeklyPatternAgent` (Level 2, LLM). Input: 7 DailySummaries + user profile/goals. Output: patterns array (strings) + `CalendarDayStatuses` map. Calendar status logic: green = within 10% of target; yellow = 10–25% over; red = >25% over (or >25% under if goal is bulk). No profile → green/yellow/red based on absolute thresholds.
**Acceptance:** Integration test: 7 days of meals → patterns array non-empty; calendar statuses map has entry for each day with data.

### 5.3 Frontend: Week tab
**What:** Week tab: weekly calorie/macro summary bars, patterns insights list (lazy load with skeleton), calendar grid (7 columns × N weeks, each cell colored by status). Tapping a day cell navigates to that day's Day tab view. Toast fires when weekly report is first generated.
**Acceptance:** Calendar renders with correct colors matching API data; tapping a day navigates correctly; skeleton shown while generating.

---

## Block 6: Pattern Breaker

### 6.1 PatternEvent entity + API
**What:** Create `PatternEvent`: `UserId`, `PatternKey` (string enum), `Type` (Positive/Warning), `TriggeredAt`, `Acknowledged` (bool), `Message`. `GET /pattern-events?limit=20` (recent, unacknowledged first). `POST /pattern-events/{id}/acknowledge`.
**Acceptance:** GET returns events sorted by triggered_at desc; acknowledge sets flag; acknowledged events appear last.

### 6.2 PatternBreakerAgent + built-in patterns
**What:** Implement `PatternBreakerAgent`. Runs after each meal save and after DailySummary generation. Evaluates 9 built-in patterns against recent history (last 30 days of DailySummaries + meals). Creates `PatternEvent` records when conditions are met. Rate-limit: same `PatternKey` cannot fire more than once per 3 days per user.
**Patterns to implement:** weekend overeating, late-night calories, post-streak crash, Friday risk, breakfast-skip compensation (warnings); consecutive deficit streak, first full week, protein streak, no-late-eating habit (positives).
**Acceptance:** Unit tests for each pattern: construct history that should trigger pattern → event created; construct history that should not → no event; rate-limit test: fire twice within 3 days → second event not created.

### 6.3 User habits input
**What:** Add `UserHabits` table: `UserId`, `HabitDescription` (free text), `CreatedAt`. `POST /habits`, `GET /habits`, `DELETE /habits/{id}`. In Profile tab: "My habits" section with list + "Add habit" text field. Habits are passed as context to `DailySummaryAgent` and `PatternBreakerAgent` prompts.
**Acceptance:** Habit saved via API; appears in profile tab list; habit text appears in LLM prompt context (verify via log).

### 6.4 Frontend: Pattern Breaker alerts + feed
**What:** Toast notifications for new `PatternEvent` records (check on app focus / after meal save). Analytics tab includes "Pattern Breaker" section: list of recent pattern events (icon by type, message, date, acknowledge button).
**Acceptance:** New warning event → toast appears within 5s of meal save; positive event → different icon/color toast; feed shows all recent events; acknowledge button works.

---

## Block 7: Monthly Strategy & Analytics Tab

### 7.1 MonthlyStrategyAgent
**What:** Implement `MonthlyStrategyAgent` (Level 2, LLM). Input: last 4 WeeklyReports + user profile/goals. Output: strategic assessment (3–5 insight strings) + forecast object (`estimatedWeightChangePer30Days`, `deficitConsistencyPercent`, `mainBlocker`). Triggered lazy on first open of Analytics tab for current month.
**Acceptance:** Integration test: 4 weeks of meal data → assessment non-empty; forecast object has valid numeric fields.

### 7.2 Forecast calculation
**What:** Implement `ForecastService`: from average daily calorie delta vs target over last 30 days, calculate estimated weight change (7700 kcal ≈ 1kg). Returns `ForecastResult` used by both `MonthlyStrategyAgent` (as context) and Analytics tab directly.
**Acceptance:** Unit test: 500 kcal/day deficit for 30 days → ~1.9kg estimated loss; surplus → positive number.
**Notes:** This is pure arithmetic — no LLM needed.

### 7.3 Frontend: Analytics & Recommendations tab
**What:** Analytics tab sections: (1) Monthly Strategy — lazy-loaded insights with skeleton + "generated at" timestamp; (2) Forecast card — "At current pace: ±X kg/month"; (3) Pattern Breaker feed (from Block 6). Toast on first monthly report generation.
**Acceptance:** All three sections render; forecast card shows correct sign (+/-); skeleton appears while generating; toast fires once per month per user.

---

## Block 8: Polish & Production Readiness

### 8.1 Loading, error, and empty states
**What:** Audit every screen for missing states. Implement: skeleton loaders for all async data, error banners with retry for failed API calls, empty states with CTA for: no meals today, no weekly data, no pattern events, profile incomplete (full-screen prompt on first launch).
**Acceptance:** Simulate network error → error banner shown with retry button; simulate no data → empty state with CTA shown; no blank white screens anywhere.

### 8.2 Input validation (frontend + backend)
**What:** Frontend: form validation on Profile and meal text input (required fields, numeric ranges for weight/height/age). Backend: FluentValidation on all POST/PUT endpoints. Return structured `400` errors with field-level messages.
**Acceptance:** Submit empty profile form → inline field errors shown; send invalid payload to API → 400 with `errors` object; valid payload passes.

### 8.3 Storage abstraction
**What:** Define `IFileStorage` interface in Application: `Task<string> SaveTemporaryAsync(byte[] data, string ext)`, `Task DeleteAsync(string key)`. Implement `LocalFileStorage` for dev. Ensure all photo handling uses the interface — no direct `System.IO` calls outside Infrastructure.
**Acceptance:** Swap `LocalFileStorage` for a stub implementation → all photo-dependent tests still pass.

### 8.4 Deployment configuration
**What:** Add `docker-compose.yml` with services: `api` (ASP.NET Core), `db` (PostgreSQL), `client` (Nginx serving built React app). Add `Dockerfile` for API and client. Environment variable config for: DB connection string, JWT secret, AI model API key, storage path. Add README with local setup instructions.
**Acceptance:** `docker compose up` → app accessible at `localhost:3000`; login works; photo upload works.

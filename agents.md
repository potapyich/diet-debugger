# Diet Debugger — AI Agent Pipeline

## Overview

The AI layer is split into two levels to balance speed and cost:

- **Level 1** — fast, synchronous, runs per meal (rule-based + lightweight model)
- **Level 2** — async LLM analysis, runs on schedules or on-demand (day / week / month)

All agents communicate through defined interfaces, making the underlying model swappable.

---

## Agent Inventory

### 1. FoodVisionAgent

**Trigger:** user uploads a meal photo
**Input:** image
**Output:**
```json
{
  "ingredients": ["chicken breast", "rice", "broccoli"],
  "calories": 520,
  "protein_g": 48,
  "fat_g": 8,
  "carbs_g": 58,
  "portion_estimate": "medium",
  "confidence": 0.87
}
```
**Notes:**
- Uses a vision model (GPT-4o / Gemini Vision / Claude — configurable via interface)
- Result is editable by user before saving
- Low confidence (<0.7) surfaces a correction prompt

---

### 2. MealFeedbackAgent (Level 1)

**Trigger:** meal is saved
**Input:** meal macros + user profile + today's running totals
**Output:** status + 1–2 insights

**Status values:** `ok` | `warning` | `issue`

**Example insights:**
- "60% of your daily fat in one meal"
- "Low protein — you'll likely feel hungry soon"
- "Over daily calorie target by 200 kcal"

**Implementation:** rule-based logic, no LLM call needed for basic cases; optionally a fast LLM call for richer language

---

### 3. DailySummaryAgent (Level 2)

**Trigger:** end of day (scheduled) or manual "analyze my day"
**Input:** all meals of the day + user profile + goals
**Output:** daily totals, goal comparison, 2–4 insights

**Example insights:**
- "Calories on target, but protein 30% below goal"
- "Most calories consumed after 8pm"
- "Good deficit today — 3rd day in a row"

---

### 4. WeeklyPatternAgent (Level 2)

**Trigger:** weekly schedule (e.g., Sunday evening)
**Input:** 7 daily summaries + user profile
**Output:** detected behavioral patterns + recommendations

**Example patterns:**
- "Weekdays: solid deficit. Weekends: +800 kcal avg — erasing progress"
- "Protein consistently low on days without meat"
- "Calorie distribution improving week-over-week"

---

### 5. MonthlyStrategyAgent (Level 2)

**Trigger:** monthly schedule
**Input:** 4 weekly reports + weight log (if available) + goals
**Output:** strategic assessment

**Example output:**
- "Average deficit too small (~100 kcal/day) to see scale movement"
- "Inconsistency is the main blocker — 8/30 days over target"
- "At current pace: estimated -0.5kg/month"

---

### 6. PatternBreakerAgent

**Trigger:** runs after each meal save and daily summary
**Input:** recent meal/day history + known user patterns
**Output:** alerts and positive reinforcements

**Alert types:**
- `streak_positive` — "5th day in deficit — your best streak yet"
- `pattern_warning` — "Friday evening snacking pattern detected — heads up"
- `near_relapse` — "Today looks like your typical weekend blowout pattern starting"

**Implementation:** pattern matching over recent history; LLM used for natural language generation only

---

## Interface Contract

All agents implement:

```csharp
public interface IAgent<TInput, TOutput>
{
    Task<TOutput> RunAsync(TInput input, CancellationToken ct = default);
}
```

AI model clients are injected — never instantiated directly inside agent logic.

---

## Execution Notes

- Level 1 agents run synchronously in the request pipeline (target: <2s)
- Level 2 agents run as background jobs (Hangfire / hosted service)
- Agent outputs are stored in the DB and served from cache — not recomputed on each view
- Prompt templates are externalized (not hardcoded) for easy iteration

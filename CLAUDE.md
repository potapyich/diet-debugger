# Diet Debugger — Claude Context

## Project

AI-powered nutrition analysis app. Users photograph meals; the app identifies food, estimates calories/macros, and explains *why* their diet is or isn't working — surfacing behavioral patterns across meal, day, week, and month.

Slogan: "Stop counting calories. Start understanding your nutrition."

## Stack

- **Backend:** ASP.NET Core (C#)
- **Database:** PostgreSQL
- **Storage:** local-first, abstracted behind an interface for future cloud migration
- **AI:** all AI integrations behind interfaces (vision model for food recognition, LLM for analysis/feedback)

## Architecture Notes

- AI components are pluggable via interfaces — don't hardcode model calls directly in business logic
- Photos are temporary; only structured data (calories, macros, ingredients) is persisted
- Two-level AI pipeline:
  - Level 1: fast rule-based checks (per-meal instant feedback)
  - Level 2: LLM analysis (daily summary, weekly patterns, monthly strategy)
- Feedback tone is user-configurable (neutral / direct / harsh)
- Multilingual: AI responds in user's chosen language (default: English)

## Key Domain Concepts

- `Meal` — single food log entry with timestamp, calories, macros (P/F/C), ingredients
- `DailySummary` — end-of-day totals + insights
- `WeeklyReport` — behavioral patterns across the week
- `Pattern Breaker` — system that detects streaks (positive) and warns of impending bad patterns (negative)
- User goals: cut / maintain / bulk; optional daily calorie + macro targets

## MVP Scope

Must-have: photo → analysis, instant meal feedback, daily view, weekly view, user profile.
Out of scope for MVP: Apple Health, social features, gamification, real-time tracking.

## Docs

- `prd.md` — English PRD
- `prd_ru.md` — Russian PRD (source of truth for product decisions)
- `agents.md` — AI agent pipeline design

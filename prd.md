# Product Requirements Document: Diet Debugger

## Overview

Diet Debugger is an AI-powered web application (native mobile in v2) that analyzes a user's diet from food photos and explains *why* they are not achieving their goal — rather than simply counting calories. Users photograph meals; the app identifies food, estimates calories/macros, surfaces insights, and detects behavioral patterns across meal → day → week → month.

## Problem Statement

Users trying to lose weight or improve nutrition face a common frustration:
- They don't understand why they're not losing weight despite "eating okay"
- They don't want to manually count every calorie
- They can't see their own behavioral patterns
- Existing tools give them numbers but no explanation

## Goals

1. Let users log meals by photo with minimal friction
2. Provide instant, actionable feedback after each meal
3. Surface daily, weekly, and monthly patterns in eating behavior
4. Explain *why* the diet is or isn't working — not just report numbers
5. Help users catch and break bad patterns before they derail progress (Pattern Breaker)
6. Support multiple languages; AI responds in the user's chosen language

## Non-Goals

- Apple Health / fitness tracker integration (v2+)
- Social features (v2+)
- Native mobile apps (v2+)
- Social login (v2+)
- Freemium / paywall (v2–v3; deep analysis will eventually be paid)
- Pixel-perfect calorie accuracy
- Complex gamification
- Real-time / live tracking

## User Stories

### Core meal flow
- As a user, I open the app and see today's day-state screen
- I tap "+" and choose: take a photo, pick from gallery, or enter text
- The app processes the meal asynchronously (loader shown); when ready I see calories, macros, ingredients, and a confidence indicator
- If confidence is low, I see "we're not sure — check or retake" with the best guess shown for editing
- I confirm or edit the result, then save
- I can return to any saved meal and edit it; insights recalculate automatically after edits
- After saving I immediately see 1–2 meal-level insights (MealFeedbackAgent)

### Analytics
- At any time I can open the Day tab and see running totals vs. goal, meal list, and day insights
- When I first open the Week or Analytics tab, insights are generated on-demand (lazy); a toast notification (top-right, macOS-style) appears when ready
- The Week view includes a calendar grid with color-coded days (green / yellow / red)
- Monthly and strategic insights available in the Analytics & Recommendations tab

### Pattern Breaker
- The app tracks built-in behavioral patterns; alerts appear as toast notifications or in-feed cards
- Users can report their own habits; the app learns from history over time

### Profile & onboarding
- I can start using the app immediately without a profile (general food analysis only)
- The UI persistently recommends filling in profile for personalized insights
- Profile fields: weight, height, age, sex, goal (cut / maintain / bulk), diet type, feedback tone (neutral / direct / harsh), preferred language

### Auth
- MVP: email + password only
- v2: social login (Google, Apple)

## Technical Requirements

**Frontend:** React + Vite SPA (web-first MVP)
**Backend:** ASP.NET Core REST API
**Database:** PostgreSQL
**Storage:** local-first, abstracted behind an interface for future cloud migration
**Auth:** email/password (JWT); social login in v2

**AI pipeline:**
- All AI integrations behind interfaces (swappable model)
- Level 1 — MealFeedbackAgent: synchronous rule-based, runs in request pipeline (<2s target)
- Level 2 — DailySummaryAgent, WeeklyPatternAgent, MonthlyStrategyAgent: async, triggered on first screen open (lazy) or by background scheduler
- FoodVisionAgent: async; result returned via polling or server-sent event
- Manual text entry: free text → FoodVisionAgent (text mode) → user validation flow (same as photo flow)

**Photos:** temporary only; structured data (calories, macros, ingredients) persisted permanently

**Localization:** default English; user-selectable language; AI responds in selected language

**Data model (core entities):**
- `Meal`: timestamp, calories, macros (P/F/C), ingredients, portion estimate, confidence score, source (photo/text/manual)
- `DailySummary`: totals, goal comparison, insights, generated_at
- `WeeklyReport`: patterns, calendar day statuses, generated_at
- `User`: profile, goals, known habits
- `PatternEvent`: type (positive/warning), pattern_key, triggered_at, acknowledged

## Navigation

Four main tabs:
1. **Day** — today's meal list + running totals + day insights
2. **Week** — weekly summary + patterns + calendar grid (color-coded days)
3. **Analytics & Recommendations** — monthly strategy + Pattern Breaker feed
4. **Profile** — user settings, goals, feedback tone, language

## Pattern Breaker — Built-in Patterns

**Negative (warnings):**
- Weekend overeating (Sat/Sun consistently above target)
- Most calories consumed after 19:00
- Post-streak crash (binge after several "good" days)
- Friday evening — historically high-risk time
- Skipping breakfast → evening compensation

**Positive (reinforcement):**
- N consecutive days in deficit (streak)
- First full week hitting target
- Protein on track 3+ days in a row
- No eating after X — new habit forming

Additional patterns: user-reported + learned from history over time.

## UX Details

**Main screen (Day tab):**
- Day state at a glance: calories consumed / target, macro rings
- Meal list below
- Floating "+" button → bottom sheet: Take Photo | Choose from Gallery | Enter Text
- Toast notifications (top-right, macOS-style) for: insights ready, Pattern Breaker alerts

**Meal analysis flow:**
1. Upload photo or submit text
2. Async processing → spinner / progress indicator
3. Result screen: ingredients list, calories, macros, confidence badge
4. If low confidence: "We're not sure — please check or retake the photo"
5. User edits if needed → Save
6. Instant meal-level insight shown post-save

**Editing meals:** any saved meal is editable; daily insights invalidated and regenerated on next open after edit.

**Calendar (Week tab):** monthly grid, each day colored: green (on target) / yellow (close) / red (over).

## Monetization

- MVP: fully free
- v2–v3: deep LLM analysis (weekly patterns, monthly strategy, recommendations) behind paywall

## Metrics

- DAU, meals logged per day
- Retention (D7, D30)
- Insights engagement rate
- Pattern Breaker alert acknowledgment rate
- Deficit consistency rate

## Future (v2+)

- Native iOS / Android apps (push notifications for Pattern Breaker)
- Apple Health integration
- Social login
- Activity tracking
- Weight log
- AI coach
- Freemium paywall

## Slogan

Stop counting calories. Start understanding your nutrition.

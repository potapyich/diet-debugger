# Step 02 — Interview Setup

## Goal

Configure the interview mode before starting the interview.

## Instructions

### 1. Present options to the user

Ask:

> Before we start the interview, let's configure it.
>
> **Question limit:** How many questions should I ask?
> - Unlimited (I'll tell you when I'm done)
> - Fixed number: ___
>
> **Mode:**
> - **Chunked / 5 questions (recommended):** I pause every 5 questions, tell you my current
>   confidence level, flag any critical gaps, and offer to continue or stop.
> - **Continuous:** I ask all questions without pausing, then give a summary.
>
> What do you prefer?

### 2. Record configuration

Wait for the user's response. Parse their preferences:
- `interview_mode`: `"chunked_5"` or `"continuous"`
- `interview_limit`: number or `null` (unlimited)

### 3. Update state

```json
{
  "step": "interview",
  "interview_mode": "<chunked_5 or continuous>",
  "interview_limit": <number or null>,
  "interview_questions_asked": 0,
  "completed": ["init", "interview_setup"],
  "pending": ["interview", "plan", "impl_plan", "tasks", "execute"]
}
```

Write to `.pipeline/state.json`.

### 4. Transition

Confirm:
> Got it. Starting interview now.

Then immediately begin Step 03 (interview) — load `${CLAUDE_SKILL_DIR}/steps/03_interview.md`.

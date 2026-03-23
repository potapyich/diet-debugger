# Step 01 — Init: Input & PRD Generation

## Goal

Gather the user's project description and generate a structured PRD.

## Instructions

### 1. Detect input language

Check what language the user has been writing in this session.
Set `lang` in state accordingly: `"ru"` or `"en"`.

### 2. Request project description

If no project description has been given yet, ask the user:

> Describe your project in free form — dump everything you know: what it is, why it exists,
> how it works, constraints, tech stack. No need to be structured, I'll organize it.

Wait for the user's response before proceeding.

### 3. Check for existing CLAUDE.md

Use the Read tool to check if `CLAUDE.md` exists in the current project.
- If found: read it and note the tech stack and architectural decisions.
- If not found: note this; offer at the end of this step to create one or defer.

### 4. Generate PRD

**If input was in Russian:**
1. Generate `_prd_ru.md` — structured PRD in Russian, based on the user's description.
2. Generate `prd.md` — English translation/adaptation of `_prd_ru.md`.

**If input was in English:**
1. Generate `prd.md` directly.

PRD structure:
```markdown
# Product Requirements Document: <Project Name>

## Overview
<1-2 sentence summary>

## Problem Statement
<what problem this solves and for whom>

## Goals
<numbered list of success criteria>

## Non-Goals
<explicit exclusions>

## User Stories
<key use cases>

## Technical Requirements
<stack, integrations, constraints>

## Open Questions
<unresolved items — will be addressed in interview>
```

### 5. Present and confirm

Show the generated PRD to the user. Ask:
> Does this capture your project correctly? Any corrections before we move to interview setup?

Apply any corrections, then proceed.

### 6. Update state

```json
{
  "step": "interview_setup",
  "lang": "<detected lang>",
  "completed": ["init"],
  "pending": ["interview_setup", "interview", "plan", "impl_plan", "tasks", "execute"]
}
```

Write to `.pipeline/state.json`.

### 7. Offer CLAUDE.md creation (if missing)

If `CLAUDE.md` was not found:
> No CLAUDE.md found. Want me to create one now with the stack and context from your project
> description? You can edit it later.

If yes: generate a minimal `CLAUDE.md` with tech stack and project context.

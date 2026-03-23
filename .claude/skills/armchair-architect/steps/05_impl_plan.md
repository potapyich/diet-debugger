# Step 05 — Implementation Plan (Tactics)

## Goal

Break each block from `plan.md` into concrete, scoped steps. Generate `implementation_plan.md`
for user approval. This is where "too coarse", "not needed", and "missing a step" are caught.

## Instructions

### 1. Read plan.md

Use the Read tool to read `plan.md`.

### 2. Generate implementation_plan.md

For each block in `plan.md`, produce a list of concrete steps:

```markdown
# Implementation Plan: <Project Name>

## Block 1: <Name>

### 1.1 <Step Name>
**What:** Precise description of what to build/change
**Acceptance:** How we know this is done (observable outcome)
**Notes:** Any non-obvious constraints or considerations

### 1.2 <Step Name>
...

## Block 2: <Name>
...
```

**Step sizing guidelines:**
- Each step should be completable in one focused session
- Steps should produce a verifiable artifact (file, passing test, working endpoint)
- If a step feels vague, break it down further
- If two steps are always done together, merge them

### 3. If lang = "ru", also generate _implementation_plan_ru.md

### 4. Present for approval

Show `implementation_plan.md` to the user. Ask:

> Review the implementation plan:
> - Any steps too coarse-grained (need splitting)?
> - Any steps that shouldn't be there?
> - Any missing steps?
> - Any wrong order within a block?
>
> Approve to generate execution tasks, or tell me what to change.

### 5. Apply corrections and re-confirm if needed

Repeat until explicit approval.

### 6. Update state

```json
{
  "step": "tasks",
  "completed": ["init", "interview_setup", "interview", "plan", "impl_plan"],
  "pending": ["tasks", "execute"]
}
```

### 7. Confirm

> Implementation plan approved. Run `/pipeline` to generate execution tasks.

# Step 06 — Generate Execution Tasks

## Goal

Convert the approved `implementation_plan.md` into `implementation_plan.json` — the task
file that ralph will execute.

## Instructions

### 1. Read implementation_plan.md

Use the Read tool to read `implementation_plan.md`.

### 2. Generate implementation_plan.json

Rules:
- Each step from `implementation_plan.md` becomes one task object
- Tasks must fit within one context window (if a step is too large, note it for splitting)
- Verification steps must be runnable commands — not descriptions
- `passes` is always `false` initially (ralph marks it true after verification)

Schema:
```json
[
  {
    "id": "1.1",
    "category": "<backend|frontend|database|infra|test|config>",
    "description": "Precise description of what to implement",
    "context": "Any non-obvious background the executor needs",
    "verification": [
      "npm test -- --grep 'unit test name'",
      "curl -s http://localhost:3000/endpoint | jq '.status'"
    ],
    "passes": false
  }
]
```

**Verification guidelines:**
- Must be shell commands that exit 0 on success, non-0 on failure
- Prefer running specific tests over `npm test` (faster feedback)
- For UI tasks: include a check that the file exists and contains expected content
- At minimum one verification per task

### 3. Write the file

Write the generated JSON to `implementation_plan.json`.

### 4. Show summary

Display:
> Generated `implementation_plan.json` with <N> tasks across <M> categories.
>
> Task breakdown:
> - <category>: <count> tasks
> ...
>
> Run `/pipeline` to start execution.

### 5. Update state

```json
{
  "step": "execute",
  "completed": ["init", "interview_setup", "interview", "plan", "impl_plan", "tasks"],
  "pending": ["execute"]
}
```

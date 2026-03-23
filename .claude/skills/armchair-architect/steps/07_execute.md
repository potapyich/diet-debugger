# Step 07 — Execute

## Goal

Run the implementation plan using ralph (or the configured executor).

## Instructions

### 1. Check impl configuration

Read `impl.execute` from state. Default: `ralph`.
Load the corresponding impl file: `${CLAUDE_SKILL_DIR}/impl/execute/<impl>.md`

### 2. Pre-flight check

Verify `implementation_plan.json` exists:

```bash
cat implementation_plan.json | jq 'length'
```

If missing, tell the user:
> `implementation_plan.json` not found. Run `/pipeline` from Step 06 to generate it first.

Show current task status (passed vs pending):
```bash
cat implementation_plan.json | jq '[.[] | {id, passes}]'
```

### 3. Recommend supervised start

> Before running the full plan, I recommend a supervised run of 3 tasks to verify
> ralph picks tasks correctly and tests pass.
>
> **Supervised run:**
> ```bash
> ralph 3
> ```
>
> **Full run:**
> ```bash
> ralph 10
> ```
>
> How many tasks should ralph run?

Wait for user's choice, then follow the impl file instructions.

### 4. Context handoff reminder

If you notice you're approaching context limits (~40-50%), warn the user:

> Approaching context limit. Before starting a new session:
> 1. `progress.md` has the execution log
> 2. `implementation_plan.json` has task status (`passes: true/false`)
> 3. In the new session, run `/pipeline` — it will resume from `execute` step
>
> ralph will continue from the first `passes: false` task.

### 5. On task failure or stuck task

If ralph reports a task as stuck or failing after 2 attempts, propose a split:

> Task `<id>` appears stuck. Suggested split:
> - `<id>a`: <subtask description>
> - `<id>b`: <subtask description>
> - `<id>c`: <subtask description>
>
> Approve split? The original task will be replaced in `implementation_plan.json`.

Wait for user approval before modifying `implementation_plan.json`.

### 6. Completion

When all tasks have `passes: true`:

> All tasks complete.
>
> Summary:
> - Tasks completed: <N>
> - Verification commands run: <N>
>
> Run `git log --oneline` to review commits, or check `progress.md` for full execution log.

Update state:
```json
{
  "step": "done",
  "completed": ["init", "interview_setup", "interview", "plan", "impl_plan", "tasks", "execute"],
  "pending": []
}
```

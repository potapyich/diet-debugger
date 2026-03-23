# Impl: Embedded Executor (default)

## Overview

Autonomous task executor built into the pipeline. No external dependencies.
Reads `implementation_plan.json`, implements tasks one by one, verifies each, marks done.

---

## Execution Loop

### 1. Load the task list

```bash
cat implementation_plan.json
```

Find the first task where `"passes": false`. That is the current task.
If all tasks have `"passes": true` — execution is complete, go to Completion.

### 2. Announce the task

Tell the user:
> **Task <id>:** <description>

### 3. Implement the task

Read relevant files, understand the context, write the code.

Guidelines:
- Make the smallest change that satisfies the task description
- Do not refactor unrelated code
- Do not add features not in the task description
- If the task depends on a previous task's output, verify that output exists first

### 4. Run verification

For each command in the task's `verification` array, run it with the Bash tool.

A task passes when **all** verification commands exit with code 0.

If a command fails:
- Read the error output carefully
- Attempt a fix
- Re-run the failed command
- Allow up to **2 fix attempts** before declaring the task stuck

### 5. Mark task result

**On pass:** Update `implementation_plan.json` — set `"passes": true` for this task id.

```bash
# Read current json, update the task, write back
cat implementation_plan.json | jq '
  map(if .id == "<task_id>" then .passes = true else . end)
' > /tmp/plan_update.json && mv /tmp/plan_update.json implementation_plan.json
```

Announce:
> Task <id> passed verification.

**On stuck (2 failed fix attempts):** Do NOT mark as passed. Go to Stuck Task handling below.

### 6. Loop

Go back to step 1 — pick the next `passes: false` task.

---

## Stuck Task Handling

When a task fails after 2 fix attempts:

1. Show the user:
```
Task <id> is stuck.

What I tried:
- <attempt 1 summary>
- <attempt 2 summary>

Last error:
<error output>

Suggested split:
  <id>a: <subtask>
  <id>b: <subtask>
  <id>c: <subtask>  (if applicable)

Options:
A) Approve split — I'll update implementation_plan.json and continue
B) Skip this task — mark as known issue, continue with next
C) Stop — investigate manually, then run /pipeline to resume
```

2. Wait for user choice. Do NOT modify `implementation_plan.json` until approved.

3. On split approval:
   - Replace the stuck task entry with the subtask entries (all `passes: false`)
   - Write updated `implementation_plan.json`
   - Resume from the first subtask

---

## Context Budget Awareness

After each completed task, check approximately how much context has been used.
At ~40-50% estimated usage, warn the user:

> Approaching context limit after task <id>.
> Completed so far: <N> tasks. Remaining: <M> tasks.
>
> Recommended: start a new Claude Code session.
> State is saved — run `/pipeline` to resume from the execute step.
> ralph will continue from the first `passes: false` task.

---

## Completion

When all tasks have `"passes": true`:

```bash
cat implementation_plan.json | jq '[.[] | select(.passes == true)] | length'
```

Announce:
> All <N> tasks complete.
>
> Run `git log --oneline -10` to review what was built.

Update pipeline state — set `step` to `"done"`.

---

## Switching to Ralph

If you prefer to use the ralph-loop plugin instead of this executor:

```
/pipeline use execute ralph
```

Then see `impl/execute/ralph.md` for how to invoke it.

<!--
## Detection approach (Option C — not implemented, kept for reference)

To auto-detect ralph and use it when available:

  # Check if ralph skill files are present at known locations
  RALPH_GLOBAL=~/.claude/skills/ralph-loop/SKILL.md
  RALPH_LOCAL=.claude/skills/ralph-loop/SKILL.md

  if [ -f "$RALPH_GLOBAL" ] || [ -f "$RALPH_LOCAL" ]; then
    echo "ralph"
  else
    echo "default"
  fi

If detected: delegate to impl/execute/ralph.md
If not: proceed with this embedded executor

Downside: ralph-loop may live in a non-standard path depending on install method.
Upside: zero-config for users who already have ralph.
-->

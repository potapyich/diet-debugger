# Impl: Execute via Ralph Loop

## What This Is

Delegates execution to the [ralph-loop](https://github.com/anthropics/ralph) Claude Code plugin.
Use this if you have ralph installed and prefer it over the built-in executor.

To switch to this executor:
```
/pipeline use execute ralph
```

To switch back to the built-in executor:
```
/pipeline use execute default
```

---

## Prerequisites

ralph-loop plugin must be installed. If not installed, switch back to the default executor:
```
/pipeline use execute default
```

---

## How to Invoke

Tell the user to run ralph in a new prompt:

> Run `/ralph-loop` to start autonomous execution.
> ralph will read `implementation_plan.json` and execute tasks from the first `passes: false` entry.
>
> Suggested: start with a supervised run of 3 tasks to verify things are working:
> - In the ralph prompt, specify: run 3 tasks

Then monitor ralph's output and report results back here when done.

---

## Resuming After Ralph

When ralph completes its run, run `/pipeline` to sync state:
- Pipeline will read `implementation_plan.json` and check for remaining `passes: false` tasks
- If tasks remain, it will prompt you to continue
- If all tasks pass, pipeline moves to completion

<!--
## Note on programmatic invocation

Claude Code does not support invoking one skill from another programmatically.
Ralph cannot be called via Bash tool since it is a skill (prompt), not a CLI binary.
The delegation above is intentionally manual — user runs /ralph-loop in a separate prompt.

If ralph ever exposes a CLI entry point, this could be changed to:
  Use the Bash tool: `ralph <N>`
-->

---
name: armchair-architect
description: Feature development pipeline. Guides a project from idea through PRD, planning, and automated execution. Usage: /armchair-architect [status|reset|use <component> <impl>]
---

Current pipeline state:
```json
!`cat .pipeline/state.json 2>/dev/null || echo '{"step":"init","completed":[],"pending":["init","interview_setup","interview","plan","impl_plan","tasks","execute"],"impl":{"interview":"ask_user_question","execute":"default"}}'`
```

Arguments: "$ARGUMENTS"

---

## Routing

Read the state above and the arguments, then act accordingly:

### If arguments = "status"
Display the current pipeline state in a readable format:
- Current step
- Completed steps
- Pending steps
- Active impl configuration
- Language setting

Do not proceed further.

### If arguments = "reset"
Ask the user to confirm: "Reset pipeline state? All progress will be lost. [y/n]"
If confirmed: use the Bash tool to run `rm -f .pipeline/state.json` and confirm deletion.
If declined: do nothing.

Do not proceed further.

### If arguments starts with "use "
Parse: `use <component> <impl>` (e.g., `use execute ralphex`)
Read current state, update `impl.<component>` to `<impl>`, write back to `.pipeline/state.json`.
Confirm: "Switched <component> to <impl>."

Do not proceed further.

### If arguments = "" or "run" (default — run or continue)
Determine the current step from state. Load and execute the corresponding step file:

| Step | File |
|---|---|
| init | `${CLAUDE_SKILL_DIR}/steps/01_init.md` |
| interview_setup | `${CLAUDE_SKILL_DIR}/steps/02_interview_setup.md` |
| interview | `${CLAUDE_SKILL_DIR}/steps/03_interview.md` |
| plan | `${CLAUDE_SKILL_DIR}/steps/04_plan.md` |
| impl_plan | `${CLAUDE_SKILL_DIR}/steps/05_impl_plan.md` |
| tasks | `${CLAUDE_SKILL_DIR}/steps/06_tasks.md` |
| execute | `${CLAUDE_SKILL_DIR}/steps/07_execute.md` |

Read the step file using the Read tool, then follow its instructions exactly.

After completing a step, update `.pipeline/state.json`:
- Move the completed step from `pending` to `completed`
- Set `step` to the next pending step

Use the Bash tool to write state updates:
```bash
# Example — always read current state first, then write merged result
cat .pipeline/state.json
```

If `.pipeline/` directory does not exist, create it first:
```bash
mkdir -p .pipeline
```

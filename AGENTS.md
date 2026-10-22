# AGENTS.md Configuration

## Routing preferences

```toml
[orchestrator]
model = "gpt-6.1-sol"
reasoning_effort = "high"

[subagent_routing]
default_subagent_model = "gpt-6-luna"
default_subagent_reasoning_effort = "high"
auto_delegate = true

[routing_policy]
# Force Sol to handle complex planning and route mechanical steps to Luna
classify_task = true
sol_threshold = "architecture, complex logic, risk mitigation"
luna_threshold = "exact mechanical work, tests, formatting, refactoring"
```

Use these as workspace instructions when the requested models and delegation tools are available. This Markdown file does not itself change the app's active model or register configuration keys.

### Delegate routine execution to Luna

Apply this standing preference in every chat working in this repository, without waiting for the user to repeat it:

- Delegate routine execution to a cheaper `gpt-6-luna` subagent. This includes gameplay recordings, video encoding, uploads, file transfers, straightforward code changes, formatting, refactoring, and routine validation.
- Use Luna with `max` reasoning effort for code work and recordings/uploads; use the configured `high` default for simpler mechanical tasks. Luna does not support `ultra`.
- The main Sol agent handles architecture, complex planning, choreography, coordination, and final verification. Verify the resulting artifact or behavior, and confirm uploads reached their requested destination, before reporting completion.
- For a task such as recording a gameplay walkthrough and uploading it, give Luna the complete execution task, then independently inspect the video and upload confirmation. Do not perform the entire routine task on Sol merely because the tools are available there.
- If Luna or delegation is unavailable, explain that briefly and continue the authorized work with the available agent.

## Global standing practices

### Background bash tasks: always checkpoint

Any long-running background bash job (rips, remuxes/splits, file transfers, or anything else expected to take more than a couple minutes) must log periodic progress checkpoints — never just block silently until process-exit.

- Checkpoint roughly every 2 minutes (adjust for expected job length).
- Each checkpoint logs elapsed time plus a concrete progress signal — total bytes written to the expected output file(s)/dir(s) is the most reliable one for rips, remuxes, and transfers.
- Compare against the previous checkpoint's value; explicitly flag (e.g. `[WARN] no growth since last checkpoint`) when it hasn't moved — that's the real stall signal, not just "process still running."
- If there's a reasonable estimate for total expected duration, also flag when elapsed time exceeds it by a wide margin (roughly 1.5–2x) as abnormally slow and worth checking manually.
- This applies even to a job already running that can't have logging retrofitted into it — spin up a separate lightweight watchdog process that inspects the same output on the same interval instead of leaving it unmonitored.

**Why:** A bare "is the process still running" check has two failure modes — it can match the wrong process name and report false-completion instantly, or the process can be alive but genuinely stalled, which an existence-check alone can't distinguish. Either way, progress is invisible until something eventually notices. Checkpointed, byte-level progress logging catches both.

### Git commits: user only

Never create Git commits, including local commits, or amend existing commits. The user reviews changes and performs all commits personally. You may inspect Git state, prepare changes, and stage changes when requested, but leave committing to the user.

### Readability

Preserve the user's readable formatting. Expand switch cases instead of compressing them together. Simplify unnecessary code without compressing readable statements merely to reduce line count.

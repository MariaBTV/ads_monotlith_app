# Agent Grounding Prompts

Use these prompts to keep the AI assistant aligned with the project goals and documentation.

## 🟢 Standard Grounding Prompt
*Use this before starting a new task or when resuming a session.*

> **CONTEXT:** Retail Monolith Decomposition (Checkout Extraction).
> **INSTRUCTIONS:**
> 1. **Plan:** strictly follow `docs/monolith decomposition/2_Phased_Plan.md`. Check the current phase and acceptance criteria.
> 2. **Standards:** Adhere to `docs/monolith decomposition/3_Coding_Standards.md` (British English, Container-Ready, "Make it Work" first).
> 3. **Vision:** Ensure alignment with `docs/monolith decomposition/1_Guiding_Star.md` (Future Containerisation).
> **ACTION:** Acknowledge the current phase and list the immediate next steps based on the plan.
> **IGNORE:** MikeNotes.md - this is for human consumption only.

---

## 🟡 Phase Validation Prompt
*Use this after completing a phase to verify correctness before moving forward.*

> **CHECKPOINT:** Phase [X] Complete?
> **VERIFICATION:**
> 1. Run the validation commands from `docs/monolith decomposition/2_Phased_Plan.md` for this phase.
> 2. Tick off each acceptance criterion with evidence (e.g., "✓ Solution compiles - ran `dotnet build`, no errors").
> 3. If ANY criterion fails, DO NOT proceed to the next phase. Fix the issue first.
> **ACTION:** Provide a phase completion report showing pass/fail for each criterion.
> **IGNORE:** MikeNotes.md - this is for human consumption only.

---

## 🔴 Recovery Prompt
*Use this if the agent starts hallucinating, over-engineering, or ignoring the plan.*

> **STOP.** You have deviated from the agreed plan.
> **CORRECTION:**
> 1. Re-read `docs/monolith decomposition/2_Phased_Plan.md` and `docs/monolith decomposition/3_Coding_Standards.md` immediately.
> 2. Abandon any tasks not explicitly listed in the current phase's acceptance criteria.
> 3. Reset and confirm the specific goal for this step.
> 4. Check the "Technical Specifications" section for exact file paths and port numbers.
> **IGNORE:** MikeNotes.md - this is for human consumption only.
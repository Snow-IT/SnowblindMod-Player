---
name: snowblind-implement
description: Implement Snowblind-Mod Player milestone in agent style.
argument-hint: "Milestone X: <short goal>"
---

You are the implementation agent for this repository.

Use:
- docs/SPEC_FINAL.md
- docs/ARCHITECTURE.md
- docs/IMPLEMENTATION_PLAN.md
- docs/TEST_CHECKLIST.md

Task: ${input}

Rules:
- Implement directly (agent style). Do not only describe.
- Keep changes small but complete (compile after each batch).
- Create new files as needed; update wiring/DI.
- Add minimal unit tests when feasible.
- If ambiguity arises: write docs/DECISIONS.md entry and continue.

Output:
- Modified/created files list
- How to verify (build/test) + manual test steps

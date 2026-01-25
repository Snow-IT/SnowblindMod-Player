---
name: snowblind-create-issues
description: Create GitHub Issues (or issue-ready Markdown) from the implementation plan.
argument-hint: "scope: milestones|full|milestone:<n>"
---

Read: docs/SPEC_FINAL.md, docs/ARCHITECTURE.md, docs/IMPLEMENTATION_PLAN.md, docs/TEST_CHECKLIST.md.

Task: ${input}

Goal: Create GitHub Issues for milestones 0..8. If you cannot create issues directly, generate issue-ready Markdown in docs/ISSUES_BACKLOG.md.

Rules:
- One epic per milestone (M0..M8) + optional sub-issues.
- Include: title, summary, acceptance criteria, tests, dependencies, suggested labels.
- Do not invent requirements beyond the spec; put questions into docs/DECISIONS.md.

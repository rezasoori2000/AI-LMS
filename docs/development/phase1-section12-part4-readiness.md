# Phase 1 — Section 12, Part 4 Readiness

## Goal

Refine tutor interaction behavior with a small explicit intent model so the
tutor can explain simply, go deeper, give examples, and clarify confusion while
remaining lesson-grounded.

---

## Completed in Part 4

- [x] Added explicit tutor interaction intents
- [x] Added explanation depth control
- [x] Added response shaping policy for each intent
- [x] Added suggested follow-up prompts
- [x] Wired intent-based retrieval/context shaping into tutor flow scaffold
- [x] Added request/response metadata fields for future frontend/backend use
- [x] Documented supported intents and deferred pedagogy boundaries

---

## Why this is enough now

- Keeps tutor interaction explicit instead of vague
- Improves private-teacher-like feel without broad conversation design
- Preserves retrieval grounding and safety boundaries
- Remains compatible with current tutor backend skeleton and UI

---

## Guardrails preserved

- No open-domain chatbot behavior
- No full adaptive tutoring engine
- No grading or intervention workflows
- No broad freeform conversation model

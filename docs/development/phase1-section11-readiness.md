# Phase 1 — Section 11 Readiness: AI Tutor MVP Boundaries and Architecture

## Summary

Section 11, Part 1 establishes the **architecture foundations** for the AI tutor feature. No live LLM calls are made in Part 1. The work defines boundaries, interfaces, context assembly contracts, and stub endpoints so that Part 2 can implement the actual tutor logic without touching these architectural decisions again.

**Status: Part 1 complete — architecture defined, stubs returning HTTP 501**

---

## Part 1 — Architecture Foundations

### What Was Built

| Layer | File | Purpose |
|-------|------|---------|
| Application | `LMS.Application/AiTutor/TutorDtos.cs` | Request/response record types |
| Application | `LMS.Application/AiTutor/TutorContextSnapshot.cs` | Context assembly contract |
| Application | `LMS.Application/AiTutor/ITutorService.cs` | Service interface with data boundary docs |
| Application | `LMS.Application/AiTutor/TutorBoundaryNotes.cs` | Architecture decisions as code comments |
| API | `LMS.Api/Controllers/Student/TutorController.cs` | Stub controller (returns 501) |
| AI Service | `ai-service/app/models/tutor.py` | Pydantic models (service-to-service contract) |
| AI Service | `ai-service/app/api/v1/tutor.py` | Route stub (returns 501) |
| AI Service | `ai-service/app/api/router.py` | Wired tutor router |

**No new tests added in Part 1.** Tests will be added in Part 2 when there is real behaviour to assert.

**Build validation**: `dotnet build LMS.sln` — 0 errors, 0 warnings  
**Test regression**: `dotnet test LMS.sln` — 168/168 passed

---

## MVP Scope Decision

The following table records what is included in the AI tutor MVP and what is deferred. These decisions are also encoded in `TutorBoundaryNotes.cs`.

### Included (Section 11 Parts 2+)

| Feature | Rationale |
|---------|-----------|
| Ask free-text questions about the current lesson | Core tutoring loop |
| Request a hint for a specific question | Scaffolded learning without answer-giving |
| Request a simpler re-explanation of lesson content | Differentiation for struggling students |
| Multi-turn conversation within a session | Needed for useful tutoring dialogue |
| Session start / end lifecycle | Maps to `AiConversation.StartedAt` / `EndedAt` |
| Conversation history persisted per session | Enables coherent multi-turn responses |

### Deferred

| Feature | Deferred To | Reason |
|---------|-------------|--------|
| LearnerProfile / TopicMasterySnapshot context | Phase 3 | Those domain entities do not exist yet |
| Cross-session learner memory | Phase 3 | Requires LearnerProfile |
| Streaming responses | Phase 3 nice-to-have | Infrastructure complexity; MVP doesn't need it |
| Vector-search / RAG pipeline | Phase 3 | Significant infrastructure; not needed for lesson-grounded MVP |
| ShortAnswer AI grading | Phase 3 | Grading is owned by `IStudentService`; complex to do correctly |
| AI-generated questions | Out of scope | Not requested; would require separate moderation pipeline |
| Autonomous academic record mutation | Never | `IStudentService` owns `LessonProgress`, `QuestionAnswerRecord`, `Enrollment` |
| Parent or teacher notifications from tutor | Never | Portal messaging is a separate domain feature |
| Emotional support / counseling | Never | Outside safe MVP scope; liability concerns |
| Open-domain chat beyond the lesson | Never | Would undermine the lesson-grounded learning model |

---

## Architecture

### Call Pattern

```
Student browser
    │  POST /api/student/tutor/sessions               (start session)
    │  POST /api/student/tutor/sessions/{id}/ask      (send message)
    │  POST /api/student/tutor/sessions/{id}/end      (end session)
    ▼
Backend API  [Authorize(Roles = "Student")]
    │  TutorController  →  ITutorService (Part 2)
    │  ├─ Auth: valid Student JWT required
    │  ├─ Ownership: enrollment check → StudentAccessDeniedException (403)
    │  ├─ Assemble TutorContextSnapshot (CorrectAnswer never included)
    │  └─ HTTP POST to AI service (internal network only, not public)
    ▼
AI Service  (internal — no public auth, not internet-facing)
    │  POST /api/v1/tutor/ask
    │  Input: TutorAskPayload { context_snapshot, student_message }
    ▼
LLM Provider  (Ollama / OpenAI / Anthropic — Settings.ai_default_provider)
    │  BaseLLMProvider.complete(prompt, system_prompt, max_tokens=2000)
    ▼
AI Service returns TutorAskResponse { reply, tokens_used }
    │
Backend persists AiMessage records via AiConversation.AddMessage()
    │
Backend returns TutorReplyDto to frontend
```

### Context Assembly Boundary

The `TutorContextSnapshot` record defines exactly what travels from backend to AI service. It encodes the security invariants at the type level:

**Always included:**
- `StudentFirstName` — tone personalization (first name only; no last name in AI payloads)
- `LessonTitle`, `LessonContent`, `GradeName`, `SubjectName`
- `LessonProgressStatus` — "NotStarted" | "InProgress" | "Completed"
- `ConversationId` + `History` — current session messages only

**Added only for hint requests (`HintForQuestionId ≠ null`):**
- `QuestionText`
- `QuestionType` — "MultipleChoice" | "TrueFalse" | "ShortAnswer"
- `QuestionOptions` — MC option texts (4 items)
- **`CorrectAnswer` is absent — intentionally and permanently**

**Never included in any snapshot:**
- `CorrectAnswer`
- Other students' data
- `LearnerProfile`, `TopicMasterySnapshot` (Phase 3)
- Cross-session message history
- Parent or teacher notes

### Data Ownership

`ITutorService` owns:

| Entity | Allowed operations |
|--------|-------------------|
| `AiConversation` | Create (StartSessionAsync), End (EndSessionAsync) |
| `AiMessage` | Append via `AiConversation.AddMessage()` |

`ITutorService` reads (no writes):

| Entity | What is read |
|--------|-------------|
| `Lesson` | content, title, grade, subject |
| `LessonProgress` | status only |
| `Question` | text, type, options (never CorrectAnswer) |
| `Enrollment` | active status check |
| `StudentProfile` | id, first name, grade |

`ITutorService` **never touches**:
- `QuestionAnswerRecord` — owned by `IStudentService.CompleteLessonAsync`
- `LessonProgress` writes — owned by `IStudentService`
- Admin or teacher tables

---

## Safety Constraints

Encoded in `TutorBoundaryNotes.cs` and to be implemented in the Part 2 system prompt:

1. **Lesson-grounded prompt**: System prompt must instruct the model to answer only from lesson content. "You are a helpful tutor for `[LessonTitle]` only. If asked something outside this lesson, say 'That is outside our lesson for today' and redirect the student."
2. **Message length cap**: Student messages max 1,000 characters. Validated by the backend and by Pydantic in the AI service.
3. **Response length cap**: AI replies capped at 2,000 tokens in the LLM call parameters.
4. **No CorrectAnswer**: Context assembly is the primary defense; prompt engineering is the secondary layer.
5. **No behavioral telemetry on domain entities**: See `DesignNotes.cs` in `LMS.Domain/Personalization/` for the permanent exclusion list.

---

## Part 1 Endpoint Contracts

All three endpoints exist and are authorized. They return HTTP 501 until Part 2 wires `ITutorService`.

| Method | Route | Request body | Success response |
|--------|-------|-------------|-----------------|
| `POST` | `/api/student/tutor/sessions` | `StartTutorSessionRequest(LessonId)` | `201` `StartTutorSessionResponse` |
| `POST` | `/api/student/tutor/sessions/{id}/ask` | `TutorAskRequest(Message, HintForQuestionId?)` | `200` `TutorReplyDto` |
| `POST` | `/api/student/tutor/sessions/{id}/end` | _(empty)_ | `204` |

AI service internal endpoint:

| Method | Route | Request body | Success response |
|--------|-------|-------------|-----------------|
| `POST` | `/api/v1/tutor/ask` | `TutorAskPayload(context_snapshot, student_message)` | `200` `TutorAskResponse` |

---

## Intentionally Not Added in Part 1

| Item | Reason |
|------|--------|
| `TutorService.cs` implementation | Part 2 work; no LLM infrastructure wired yet |
| Database migration for `AiConversation` / `AiMessage` | Domain entities already exist from a prior section; no schema change needed |
| Integration tests for tutor endpoints | No meaningful behaviour to assert (stubs return 501); tests land in Part 2 |
| Frontend tutor UI components | Section 12+ scope; backend contract must stabilize first |
| Prompt templates | Part 2 implementation detail |
| `ICurrentUserService` call in `TutorController` | Controller returns 501 before resolving identity; added in Part 2 |

---

## Part 2 Implementation Checklist

When implementing `TutorService` in Part 2, follow this order:

1. **Create `TutorService.cs`** in `LMS.Application/AiTutor/` implementing `ITutorService`
2. **Implement `StartSessionAsync`**: enrollment check → create `AiConversation` → persist → return `ConversationId`
3. **Implement `AskAsync`**: ownership check → assemble `TutorContextSnapshot` → POST to AI service → persist both messages → return reply
4. **Implement `EndSessionAsync`**: ownership check → call `AiConversation.End()` → persist
5. **Register** `ITutorService → TutorService` in `ServiceCollectionExtensions.AddApplicationServices()` (TODO comment already in place)
6. **Unwire the 501 stubs** in `TutorController` — replace with actual `_tutor.*` calls
7. **Implement `ask_tutor`** in `ai-service/app/api/v1/tutor.py`:
   - Build system prompt grounded in lesson content
   - Format conversation history for the LLM context window
   - Call `get_provider().complete(...)` with `max_tokens=2000, temperature=0.3`
8. **Add `HttpClient`** configuration in backend to call the AI service (base URL from config)
9. **Add integration tests**: start session → ask → verify 200 + reply shape; end session → verify idempotent
10. **Add AI service tests**: valid payload → mock provider → assert `TutorAskResponse` shape

---

## Perspectives

### Maintainability
The `TutorBoundaryNotes.cs` file co-locates the architecture contract with the production code, so the rules are visible during code review and in IDE symbol navigation — not buried in a separate docs folder. Future contributors implementing Part 2 will see the constraints before writing a single line.

### Security
The `CorrectAnswer` exclusion is enforced at two independent levels: the C# `TutorContextSnapshot` record (which has no `CorrectAnswer` property to populate), and the Python `TutorContextSnapshot` model (same absence). A developer would have to intentionally add the field to both models to violate this constraint.

### Extensibility
The `BaseTutoringService` stub in `ai-service/app/services/base.py` and the `ITutorService` interface are independent — the backend interface can evolve without touching the Python service layer, and the Python service layer can switch LLM providers by swapping `get_provider()` without touching the C# interface.

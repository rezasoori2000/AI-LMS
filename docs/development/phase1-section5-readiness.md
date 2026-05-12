# Phase 1 — Section 5 Readiness Checklist

Use this checklist before starting **Phase 1, Section 6**.
Every item should be verified in a current checkout of the repo.

---

## Automated checks (run these first)

```bash
cd backend
dotnet build LMS.sln          # must succeed, 0 errors, 0 warnings
dotnet test LMS.sln           # must pass: 111 tests (70 domain + 12 application + 29 API)

cd frontend
npm run type-check             # must exit 0, 0 TS errors
npm run lint                   # must exit 0
npm run test -- --run          # must pass: 41 tests

cd ai-service
pytest                         # must pass: 4 tests
```

**Verified ✅ — Phase 1 Section 5 Parts 1–5 complete (2025-07-11)**
`dotnet build`: 0 errors, 0 warnings
`dotnet test`: 111/111 passing
`npm run type-check`: 0 TS errors
`npm run test`: 41/41 passing

---

## Backend content module

### Application layer — services

- [x] `IGradeService` / `GradeService` — CRUD for Grade; duplicate-level → 409
- [x] `ISubjectService` / `SubjectService` — CRUD for Subject; duplicate-slug → 409
- [x] `IChapterService` / `ChapterService` — CRUD for Chapter; validates Subject + Grade FK existence
- [x] `ILessonService` / `LessonService` — CRUD for Lesson; validates Chapter FK existence
- [x] `IQuestionService` / `QuestionService` — CRUD for Question; optional LessonId FK validation

### Application layer — request DTOs

- [x] All 5 DTO files include `[Required]` / `[MaxLength]` / `[Range]` data annotations
- [x] Empty-string / null inputs return HTTP 400 via model binding (not HTTP 500 from domain)
- [x] Auth DTOs (`LoginRequest`, `RegisterRequest`) use identical annotation pattern — consistent
- [x] `CreateQuestionRequest` and `UpdateQuestionRequest` annotate `Text` and `CorrectAnswer` as `[Required]`
- [x] `[Range(1, int.MaxValue)]` on all `Order` and `Level` integer fields
- [x] `[Range(1, 600)]` on nullable `EstimatedMinutes` (Lesson)
- [x] `[MaxLength(4000)]` on `OptionsJson` (Question)

### Application layer — exception types

- [x] `ContentNotFoundException` — thrown when entity not found or not owned by caller; maps to HTTP 404
- [x] `ContentConflictException` — thrown on duplicate slug / duplicate level; maps to HTTP 409
- [x] Both exceptions handled in `ExceptionHandlingMiddleware` (RFC 7807 Problem Details)

### API layer — controllers

- [x] `GradesController` — `GET /api/admin/grades`, `GET :id`, `POST`, `PUT :id`, `DELETE :id`
- [x] `SubjectsController` — `GET /api/admin/subjects`, `GET :id`, `POST`, `PUT :id`, `DELETE :id`
- [x] `ChaptersController` — `GET /api/admin/chapters`, `GET :id`, `POST`, `PUT :id`, `DELETE :id`
- [x] `LessonsController` — `GET /api/admin/lessons`, `GET :id`, `POST`, `PUT :id`, `DELETE :id`
- [x] `QuestionsController` — `GET /api/admin/questions?lessonId=`, `GET :id`, `POST`, `PUT :id`, `DELETE :id`
- [x] All controllers carry `[Authorize]` + `[RequireRole("SuperAdmin", "TenantAdmin")]`
- [x] All controllers inherit `BaseApiController` (`[ApiController]`, `/api/[controller]` route)
- [x] All 5 services registered in `AddApplicationServices()` DI extension

### API layer — integration tests

- [x] 15 new `AdminContentIntegrationTests` — grades, subjects, chapters, lessons, questions, auth guards
- [x] Tests cover: create 201, list 200, get-by-id 200, get-unknown 404, update 200, delete 204, duplicate 409, 401 (no token), 403 (wrong role)
- [x] All tests use `LmsWebApplicationFactory` (InMemory EF Core, no Postgres required)

### Service patterns (all 5 services follow these consistently)

- [x] Tenant scoping: SuperAdmin sees all records; TenantAdmin/ContentEditor scoped to own TenantId
- [x] `IsSuperAdmin` check uses `_currentUser.Role == "SuperAdmin"` string comparison
- [x] `FindOwnedOrThrowAsync` helper — centralises ownership check + not-found throw
- [x] `ToDto()` static projection method on each service — no AutoMapper dependency
- [x] FK validation on create/update uses `AnyAsync` before the domain factory call

---

## Frontend content module

### Type definitions

- [x] `frontend/src/types/content.ts` — all DTO and form-state types for all 5 entities
- [x] `QuestionType`: `'MultipleChoice' | 'TrueFalse' | 'ShortAnswer'` (matches backend enum)
- [x] `DifficultyLevel`: `'Easy' | 'Medium' | 'Hard'` (matches backend enum)
- [x] `QuestionFormState` — separate fields per answer type: `options`, `correctAnswerIndex`, `correctAnswerBool`, `correctAnswerText`

### API service layer

- [x] `frontend/src/services/content.service.ts` — typed `axios` calls for all 5 entities
- [x] `getQuestions(lessonId?)` — optional filter parameter for lesson context view

### TanStack Query hooks

- [x] `useGrades` / `useGrade` / `useCreateGrade` / `useUpdateGrade` / `useDeleteGrade`
- [x] `useSubjects` / `useSubject` / `useCreateSubject` / `useUpdateSubject` / `useDeleteSubject`
- [x] `useChapters` / `useChapter` / `useCreateChapter` / `useUpdateChapter` / `useDeleteChapter`
- [x] `useLessons` / `useLesson` / `useCreateLesson` / `useUpdateLesson` / `useDeleteLesson`
- [x] `useQuestions(lessonId?)` / `useQuestion` / `useCreateQuestion` / `useUpdateQuestion` / `useDeleteQuestion`
- [x] All hooks follow identical key + invalidation pattern

### Admin pages — list views

- [x] `GradesPage` — sortable list; `ConfirmDeleteButton`; `StateWrapper`
- [x] `SubjectsPage` — includes slug badge
- [x] `ChaptersPage` — filters by subject/grade via URL params
- [x] `LessonsPage` — "Questions" ghost button per row → `/admin/content/questions?lessonId=:id`
- [x] `QuestionsPage` — optional `?lessonId=` filter with "View all questions" escape hatch; type + difficulty badges

### Admin pages — form views

- [x] `GradeFormPage` — create / edit; shows "Saving…" text while pending
- [x] `SubjectFormPage` — auto-slugify from name on create; shows "Saving…" while pending
- [x] `ChapterFormPage` — subject + grade dropdowns pre-loaded; shows "Saving…" while pending
- [x] `LessonFormPage` — chapter dropdown + estimated minutes; shows "Saving…" while pending
- [x] `QuestionFormPage` — type-aware conditional sections; `?lessonId=` pre-fill on create; changing type resets type-specific fields; shows "Saving…" while pending

### Navigation, routing, i18n

- [x] `admin.routes.tsx` — 3 question routes: list, new, edit
- [x] `nav.ts` — `questions` entry under content section for `super_admin`, `tenant_admin`, `content_editor`
- [x] `en.json` — full `admin.content.questions` block: title, description, form labels, validation messages, badge labels
- [x] `common.saving` key added and used consistently across all 5 content form pages
- [x] `common.true` / `common.false` keys added for TrueFalse question answer display

### Shared UI component reuse

- [x] `ConfirmDeleteButton` — used on all 5 list pages; two-step delete with 4 s auto-revert
- [x] `StateWrapper` — handles loading / error / empty states on all 5 list pages
- [x] `mapApiError` — used in all 5 form pages for consistent error display
- [x] `InlineFeedback` — `intent` prop (not `type`); used in form pages for error feedback
- [x] `Badge` — `variant` prop; type and difficulty badges on QuestionsPage

---

## Question model

- [x] `Question` domain entity — nullable `LessonId` (bank question vs lesson-attached)
- [x] `QuestionType` enum: `MultipleChoice | TrueFalse | ShortAnswer`
- [x] `DifficultyLevel` enum: `Easy | Medium | Hard`
- [x] `OptionsJson` — JSON string array for MultipleChoice options (`string[]` serialised)
- [x] `CorrectAnswer` — 0-based index string (MC), `"true"`/`"false"` (T/F), reference text (SA)
- [x] No `Explanation` field in Phase 1 — deferred to Phase 3

---

## Pre-Section 6 checklist

Before starting Section 6 (student/learner features), confirm:

- [ ] `docker compose up postgres -d && dotnet run --project src/LMS.Api` starts without errors
- [ ] `POST /api/auth/login` with a SuperAdmin seed user returns a valid JWT
- [ ] `GET /api/admin/grades` with valid Bearer token returns seeded grades
- [ ] `POST /api/admin/grades` with `{"name":"","level":1}` returns HTTP 400 (not 500)
- [ ] Frontend dev server starts: `cd frontend && npm run dev`
- [ ] Admin sidebar shows Grades / Subjects / Chapters / Lessons / Questions nav items
- [ ] `dotnet test LMS.sln` passes: 111/111
- [ ] `npm run test -- --run` passes: 41/41

---

## Section 5 completion summary

### What was built

| Part | Description |
|------|-------------|
| Part 1 | Admin shell and dashboard — sidebar navigation, topbar, role-based layout |
| Part 2 | Backend CRUD — 5 content services, 5 controllers, exception types, DI registration |
| Part 3 | Frontend CRUD — grades, subjects, chapters, lessons pages (list + form) |
| Part 4 | Question CRUD + lesson-question association — type-aware form, lessonId filter, `LessonsPage` integration |
| Part 5 | Audit, validation fix (data annotations on all request DTOs), doc cleanup, this readiness checklist |

### Test counts

| Project | Tests | Status |
|---------|-------|--------|
| LMS.Domain.Tests | 70 | ✅ All passing |
| LMS.Application.Tests | 12 | ✅ All passing |
| LMS.Api.Tests | 29 | ✅ All passing (+15 AdminContent integration tests) |
| **Backend Total** | **111** | **✅** |
| Frontend (Vitest) | 41 | ✅ All passing |

### Key decisions made in Section 5

- No AutoMapper — each service has a static `ToDto()` projection; easy to reason about, easy to extend
- No dedicated repository per entity — services use `ILmsDbContext` directly (same pattern as auth)
- No FluentValidation — data annotations on request records are sufficient for Phase 1; FluentValidation deferred to Phase 2
- `ArgumentException` from domain factories is NOT mapped to 400 — data annotations prevent bad input from ever reaching the domain
- `OptionsJson` stored as a raw JSON string in Phase 1 — no separate `QuestionOption` table until Phase 3
- Questions ordered by `CreatedAt` — no manual reordering field; order-by-position deferred to Phase 3

### What is intentionally deferred

| Feature | Phase |
|---------|-------|
| Publishing workflow (`IsPublished` lifecycle, draft/review/published states) | 3 |
| Media management (video URLs, file attachments, S3/Azure Blob storage) | 3 |
| Advanced filtering, search, and server-side pagination on list pages | 2 |
| Audit history (`CreatedBy` / `UpdatedBy` auto-population via `ICurrentUserService`) | 3 |
| Content localisation (multilingual lesson/question text) | 3 |
| Question `Explanation` field (shown after answer) | 3 |
| Question tags and curriculum-standard alignment | 3 |
| Variable option counts (≠ 4) for MultipleChoice | 3 |
| Question ordering / reordering within a lesson | 3 |
| Assessment runtime (quiz sessions, attempts, scoring, grade book) | 3 |
| Question analytics and adaptive difficulty | 3+ |
| Advanced question types (ordering, matching, drag-and-drop) | 3+ |
| Content service unit tests in `LMS.Application.Tests` | 2 |
| FluentValidation for richer server-side validation | 2 |
| Client-side Zod / react-hook-form validation | 2 |
| Searchable / ComboBox selects for large dropdown lists | 2 |
| Toast notifications for delete success | 2 |
| Subject `Description` field visible in admin form | 2 |

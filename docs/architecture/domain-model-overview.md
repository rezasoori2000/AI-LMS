# Domain Model Overview

Established in Phase 1, Section 4, Parts 1–4.

---

## Aggregate / Entity Map

```
User
├── (auth identity — email, password hash, role, tenant)
│
ParentProfile ──────────────────────── FK → User
StudentProfile ─────────────────────── FK → User
               ┌── GradeId (nullable)  FK → Grade
               └── ParentId (nullable) FK → ParentProfile

Grade          (level + name, optional tenant scope)
Subject        (name + slug, optional tenant scope)
Chapter        (SubjectId + GradeId + order)
Lesson         (ChapterId + order + Markdown content)
Question       (LessonId nullable + type + options JSON)

Enrollment              ─ StudentProfileId + SubjectId + status
LessonProgress          ─ StudentProfileId + LessonId  + status + score
QuestionAnswerRecord    ─ StudentProfileId + LessonId + QuestionId + IsCorrect

TeacherStudentAssignment ─ TeacherUserId + StudentProfileId  (M:M join)

AiConversation ─ StudentProfileId + optional LessonId
AiMessage      ─ ConversationId (owned messages, append-only)

[Personalization]  — namespace reserved; entities planned for Phase 3
                     See docs/architecture/personalization-readiness.md
```

---

## Entity Details

### Identity and Profiles

| Entity | Table | Key Fields |
|--------|-------|------------|
| `User` | `users` | `email` (unique), `password_hash`, `role` (string enum), `tenant_id` (null = SuperAdmin) |
| `ParentProfile` | `parent_profiles` | `user_id` (unique FK) |
| `StudentProfile` | `student_profiles` | `user_id` (unique FK), `grade_id` (nullable), `parent_id` (nullable FK → ParentProfile) |

### Curriculum

| Entity | Table | Key Fields |
|--------|-------|------------|
| `Grade` | `grades` | `level` (int), `name`; unique on `(level, tenant_id)` |
| `Subject` | `subjects` | `name`, `slug` (URL-safe); unique on `(slug, tenant_id)` |
| `Chapter` | `chapters` | `subject_id`, `grade_id`, `order`; unique on `(subject_id, grade_id, order)` |
| `Lesson` | `lessons` | `chapter_id`, `order`, `content` (Markdown text); unique on `(chapter_id, order)` |
| `Question` | `questions` | `lesson_id` (nullable), `type`, `difficulty`, `options_json` (text), `correct_answer` |

### Learning Activity

| Entity | Table | Key Fields |
|--------|-------|------------|
| `Enrollment` | `enrollments` | `student_id`, `subject_id`, `status`; filtered unique on `(student_id, subject_id) WHERE status='Active'` |
| `LessonProgress` | `lesson_progress` | `student_id`, `lesson_id`, `status`, `score_percent` decimal(5,2); unique on `(student_id, lesson_id)` |
| `QuestionAnswerRecord` | `question_answer_records` | `student_id`, `lesson_id` (denorm), `question_id`, `is_correct` bool, `answered_at`; indexes on `(student_id, lesson_id)` and `(student_id, question_id)` |

### Teacher Assignment

| Entity | Table | Key Fields |
|--------|-------|------------|
| `TeacherStudentAssignment` | `teacher_student_assignments` | `teacher_user_id` (FK → Users), `student_profile_id`, `assigned_by_user_id`; unique on `(teacher_user_id, student_profile_id)` |

### AI Conversations

| Entity | Table | Key Fields |
|--------|-------|------------|
| `AiConversation` | `ai_conversations` | `student_id`, `lesson_id` (nullable), `started_at`, `ended_at` |
| `AiMessage` | `ai_messages` | `conversation_id`, `role`, `content` (text), `sent_at`, `token_count` (nullable) |

---

## Relationship Summary

```
Grade (1) ──────────────────────── (N) Chapter
Subject (1) ────────────────────── (N) Chapter
Chapter (1) ────────────────────── (N) Lesson
Lesson (1) ─────────────────────── (N) Question       [nullable FK — question may be standalone]
Lesson (1) ─────────────────────── (N) LessonProgress
Subject (1) ────────────────────── (N) Enrollment
StudentProfile (1) ─────────────── (N) Enrollment
StudentProfile (1) ─────────────── (N) LessonProgress
StudentProfile (1) ─────────────── (N) AiConversation
StudentProfile (1) ─────────────── (N) QuestionAnswerRecord
Question (1) ───────────────────── (N) QuestionAnswerRecord
AiConversation (1) ─────────────── (N) AiMessage           [Cascade delete — messages own nothing]
ParentProfile (1) ──────────────── (N) StudentProfile       [Phase 1: 1:1 via nullable FK]
User (1) ───────────────────────── (N) TeacherStudentAssignment  [as TeacherUserId]
StudentProfile (1) ─────────────── (N) TeacherStudentAssignment
User (1) ───────────────────────── (1) ParentProfile
User (1) ───────────────────────── (1) StudentProfile
```

### Delete behavior

| FK | On Delete |
|----|-----------|
| `chapters.subject_id` → `subjects` | Restrict |
| `chapters.grade_id` → `grades` | Restrict |
| `lessons.chapter_id` → `chapters` | Restrict |
| `questions.lesson_id` → `lessons` | **SetNull** (question survives as standalone) |
| `parent_profiles.user_id` → `users` | Restrict |
| `student_profiles.user_id` → `users` | Restrict |
| `student_profiles.grade_id` → `grades` | **SetNull** (student survives without a grade) |
| `student_profiles.parent_id` → `parent_profiles` | **SetNull** (student survives without a parent) |
| `enrollments.student_id` → `student_profiles` | Restrict |
| `enrollments.subject_id` → `subjects` | Restrict |
| `lesson_progress.student_id` → `student_profiles` | Restrict |
| `lesson_progress.lesson_id` → `lessons` | Restrict |
| `ai_conversations.student_id` → `student_profiles` | Restrict |
| `ai_conversations.lesson_id` → `lessons` | **SetNull** |
| `ai_messages.conversation_id` → `ai_conversations` | **Cascade** |
| `question_answer_records.student_id` → `student_profiles` | Restrict |
| `question_answer_records.lesson_id` → `lessons` | Restrict |
| `question_answer_records.question_id` → `questions` | Restrict |
| `teacher_student_assignments.teacher_user_id` → `users` | Restrict |
| `teacher_student_assignments.student_profile_id` → `student_profiles` | **Cascade** |
| `teacher_student_assignments.assigned_by_user_id` → `users` | Restrict |

---

## Common Fields (Audit)

All entities except `AiMessage` inherit `AuditableEntity`:

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `uuid` | PK, assigned by domain factory |
| `created_at` | `timestamptz` | Set by domain `Create()` factory; defensive hook in `SaveChangesAsync` |
| `updated_at` | `timestamptz` | Set by entity behaviour methods; null until first update |
| `created_by` | `uuid?` | Phase 3: wired to `ICurrentUserService` in `SaveChangesAsync` interceptor |
| `updated_by` | `uuid?` | Phase 3: same |

`AiMessage` inherits `Entity` only (no audit columns — it is append-only; updates are not meaningful for chat messages).

---

## UserRole Enum (stored as string)

| Value | String | Description |
|-------|--------|-------------|
| 0 | `SuperAdmin` | Platform-level admin, `tenant_id = NULL` |
| 1 | `TenantAdmin` | School/organisation admin |
| 2 | `ContentEditor` | Creates and manages curriculum content |
| 3 | `Teacher` | Classroom management, student monitoring |
| 4 | `Parent` | Views child progress |
| 5 | `Student` | Takes lessons, interacts with AI tutor |

---

## Domain Design Patterns

- **Private constructor + static `Create()` factory** — enforces invariants at construction; EF Core uses the parameterless constructor via reflection.
- **Behaviour methods on entities** — `Enrollment.Complete()`, `Enrollment.Drop()`, `LessonProgress.Start()`, `LessonProgress.Complete(score)`, `AiConversation.End()`, `AiConversation.AddMessage()` — state-machine transitions live on the entity, not in application services.
- **`AiMessage.Create()` is `internal`** — only `AiConversation.AddMessage()` may create messages, enforcing the aggregate boundary.
- **`AuditableEntity.CreatedAt` defensive hook** — `LmsDbContext.SaveChangesAsync` sets `CreatedAt` if it is still `default` (guards against bypass of the `Create()` factory).

---

## Intentionally Deferred (Phase 3+)

| Concern | Reason deferred |
|---------|----------------|
| Global `TenantId` query filter | Requires `ICurrentUserService` in `OnModelCreating` — Phase 3 |
| `CreatedBy` / `UpdatedBy` population | Requires `ICurrentUserService` in `SaveChangesAsync` interceptor — Phase 3 |
| `ParentStudentLink` join table (M:M) | Phase 1 uses simple nullable FK; join table needed for step-parents, shared custody — Phase 3 |
| `TeacherProfile` entity | Teacher classroom/room assignments are a Phase 3 feature |
| Assessment `Attempt` / `AttemptAnswer` tables | Formal assessment scoring and history — Phase 3 |
| Curriculum standards tags (`CurriculumStandard`) | External standard mapping (Common Core, etc.) — Phase 3 |
| `Lesson.ContentBlocks` normalised table | Rich-media structured blocks replace `Content` Markdown — Phase 3 |
| Read models / reporting views | CQRS projections or materialised views for dashboards — Phase 4+ |
| `LearnerProfile`, `LearnerPreferences`, `TopicMasterySnapshot`, `ConsistencySignal` | Personalization entities — namespace reserved; see [personalization-readiness.md](personalization-readiness.md) |

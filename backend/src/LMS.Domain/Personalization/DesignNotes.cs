namespace LMS.Domain.Personalization;

// ── PHASE 3 DESIGN STUB ───────────────────────────────────────────────────────
//
// This file reserves the LMS.Domain.Personalization namespace and records the
// confirmed architectural decisions from Section 9, Parts 1–4.
//
// Do not implement any entity here until Phase 3 (AI Tutor section).
//
// ── Confirmed decisions ───────────────────────────────────────────────────────
//
//  1. LEARNER PROFILE KEY: (UserId, TenantId) — one record per student per tenant.
//     LearnerProfile is the primary, shared persistent learner model. All teachers
//     assigned to the student read from the same profile. Learning preferences,
//     strengths/weaknesses, pace, and engagement patterns are properties of the
//     learner, not of any teacher–student relationship.
//
//  2. TEACHER OVERLAY (optional, Phase 3+): Teacher-specific observations or
//     relationship-context notes are a separate extension entity (e.g.
//     TeacherStudentObservation linked to TeacherStudentAssignment.Id).
//     They are NOT part of the core LearnerProfile.
//
//  3. OWNERSHIP: LearnerPreferences is written by a student-facing preference
//     service. All other entities in this namespace are written exclusively by
//     the AI tutor application service (ILearnerProfileService, Phase 3).
//     They are never written by StudentService, AdminService, TeacherService,
//     or ParentService.
//
//  4. LAYER RULE: StudentProfile and User carry NO adaptive-learning or behavioral
//     fields. Phase 1 violations of this rule are breaking changes for Phase 3.
//
//  5. READ CONTRACT: Enrollment, LessonProgress, and QuestionAnswerRecord are
//     read-only source data for this layer. The personalization layer never
//     writes back to those tables.
//
//  6. IDENTITY ANCHOR: LearnerProfile binds to User.Id (not StudentProfile.Id).
//     User.Id is the durable, long-term identity even if a StudentProfile is
//     recreated. Phase 3 context-assembly code must look up StudentProfile by
//     UserId when joining to observation tables (LessonProgress, QuestionAnswerRecord)
//     which are keyed by StudentProfile.Id.
//
// ── Layer model ───────────────────────────────────────────────────────────────
//
//  ┌───────────────────────────────────────────────────────────────────┐
//  │ Layer A — Explicit Preferences  (student-set, revocable)          │
//  │ LearnerPreferences  keyed by LearnerProfile.Id                    │
//  └───────────────────────────────────────────────────────────────────┘
//           ↓ reads from (never writes back to)
//  ┌───────────────────────────────────────────────────────────────────┐
//  │ Layer B — Derived Academic Signals  (computed, periodically        │
//  │           refreshed from observation data)                        │
//  │ TopicMasterySnapshot  per (LearnerProfileId, SubjectId)           │
//  │ ConsistencySignal     per LearnerProfileId + rolling window       │
//  └───────────────────────────────────────────────────────────────────┘
//           ↓ sourced from (read-only, Phase 1 tables)
//  ┌───────────────────────────────────────────────────────────────────┐
//  │ Layer C — Observation Layer  (Phase 1, never modified by Phase 3) │
//  │ LessonProgress · QuestionAnswerRecord · Enrollment                │
//  │ AiConversation · AiMessage                                        │
//  └───────────────────────────────────────────────────────────────────┘
//
// ── Planned Phase 3 entity shapes ────────────────────────────────────────────
//
//  LearnerProfile  (root aggregate — navigation and deletion anchor)
//    Id                Guid
//    UserId            Guid    FK → Users  (durable identity anchor — see decision 6)
//    TenantId          Guid?
//    CreatedAt         DateTime
//    UpdatedAt         DateTime
//    // Navigation (loaded on demand)
//    Preferences       LearnerPreferences?
//    MasterySnapshots  ICollection<TopicMasterySnapshot>
//    ConsistencySignal ConsistencySignal?
//
//  LearnerPreferences  (Layer A — explicit only, never auto-populated from signals)
//    Id                  Guid
//    LearnerProfileId    Guid    FK → LearnerProfile
//    ExplanationStyle    string  "Concise" | "Detailed" | "ExampleFirst"
//    PacingPreference    string  "SelfPaced" | "Guided" | "Accelerated"
//    ChallengeLevelBias  string  "CurrentGrade" | "BelowGrade" | "AboveGrade"
//    HelpSeekingStyle    string  "AskFirst" | "TryAlone" | "HintOnly"
//    PreferredLanguage   string? ISO 639-1; null = tenant default
//    UpdatedAt           DateTime
//  Note: All fields must be set by an explicit actor (student or authorised admin).
//        Auto-population from inferred signals is a violation of this contract.
//        This row is student-viewable and student-editable (GDPR data-export scope).
//
//  TopicMasterySnapshot  (Layer B — derived from QuestionAnswerRecord)
//    Id                Guid
//    LearnerProfileId  Guid     FK → LearnerProfile
//    SubjectId         Guid     FK → Subjects
//    CorrectCount      int
//    AttemptCount      int      (total QuestionAnswerRecords attributable to this subject)
//    MasteryPercent    decimal  (CorrectCount / AttemptCount)
//    LastRefreshedAt   DateTime
//    TenantId          Guid?
//  Note: SubjectId is derived via QuestionAnswerRecord.LessonId → Lesson → Chapter → Subject.
//        Refresh inline at CompleteLessonAsync for Phase 3 MVP; move to a background
//        job when load grows. Do NOT store inferred concept-weakness string labels —
//        surface the numeric ratio; let the application layer interpret it.
//
//  ConsistencySignal  (Layer B — derived from LessonProgress, factual counts only)
//    Id                Guid
//    LearnerProfileId  Guid     FK → LearnerProfile
//    WindowDays        int      (e.g. 7 or 30 — one row per window type)
//    LessonsStarted    int
//    LessonsCompleted  int
//    ActiveDays        int      (distinct calendar days with ≥1 LessonProgress update)
//    ComputedAt        DateTime
//    TenantId          Guid?
//  Note: This is a factual activity count, NOT a gamification score or streak.
//        "streak" must not appear in the entity name, API responses, or UI copy.
//
// ── Excluded — do not add to this namespace ──────────────────────────────────
//
//  ✗  TypicalStudyTimeOfDay  — behavioral telemetry; raises surveillance concerns
//  ✗  AiMemoryStoreRef       — Qdrant/vector-store reference; infrastructure, not domain
//  ✗  Inferred concept-weakness string labels (RecurringWeakArea etc.)
//  ✗  Personality / cognitive-style classifications (MBTI, VARK, etc.)
//  ✗  Health, disability, or mental-health inferences
//  ✗  Engagement-addiction metrics (streak counters, loss-aversion scoring)
//  ✗  General ability / IQ-style ranking
//
// ── GDPR/deletion scope (Phase 3 StudentDataDeletionService) ─────────────────
//
//  Delete in FK-safe order:
//    QuestionAnswerRecord → LessonProgress → AiMessage → AiConversation →
//    ConsistencySignal → TopicMasterySnapshot → LearnerPreferences →
//    LearnerProfile → Enrollment → TeacherStudentAssignment → StudentProfile

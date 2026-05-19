namespace LMS.Domain.Personalization;

// ── PHASE 3 DESIGN STUB ───────────────────────────────────────────────────────
//
// This file reserves the LMS.Domain.Personalization namespace and records the
// confirmed architectural decisions from Section 9, Part 1.
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
//  3. OWNERSHIP: LearnerProfile is written exclusively by the AI tutor application
//     service (ILearnerProfileService, Phase 3). It is never written by
//     StudentService, AdminService, TeacherService, or ParentService.
//
//  4. LAYER RULE: StudentProfile and User carry NO adaptive-learning or behavioral
//     fields. Phase 1 violations of this rule are breaking changes for Phase 3.
//
//  5. READ CONTRACT: Enrollment and LessonProgress are read-only source data for
//     this layer. The personalization layer never writes back to them.
//
//  6. IDENTITY ANCHOR: LearnerProfile binds to User.Id (not StudentProfile.Id).
//     User.Id is the durable, long-term identity even if a StudentProfile is
//     recreated. See also AiConversation design notes for the traversal path.
//
// ── Planned Phase 3 entities ─────────────────────────────────────────────────
//
//  LearnerProfile
//    UserId         Guid    FK → Users (durable identity anchor)
//    TenantId       Guid    tenant scope
//    PreferredExplanationStyle  string?  e.g. "examples-first", "step-by-step"
//    PreferredPace              string?  e.g. "slow", "standard", "fast"
//    AverageSessionScorePercent decimal? refreshed periodically from LessonProgress
//    LessonsCompletedLast30Days decimal? refreshed periodically
//    TypicalStudyTimeOfDay      string?  "morning" | "afternoon" | "evening"
//    AiMemoryStoreRef           Guid?    external memory store ID (Qdrant etc.)
//    LastRefreshedAt            DateTime?
//
//  TopicMasterySnapshot
//    LearnerProfileId  Guid   FK → LearnerProfile
//    SubjectId         Guid   FK → Subjects
//    StrengthScore     decimal  0.0–1.0 (derived from LessonProgress for this subject)
//    RecurringWeakArea string?  inferred concept label — treat cautiously
//    UpdatedAt         DateTime

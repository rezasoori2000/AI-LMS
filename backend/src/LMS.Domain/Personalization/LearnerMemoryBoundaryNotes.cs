namespace LMS.Domain.Personalization;

// ── PHASE 1 — SECTION 13, PART 1: LEARNER MEMORY BOUNDARY NOTES ──────────────
//
// This file records the confirmed architectural decisions for the bounded
// persistent learner-model and tutor-memory foundations introduced in
// Phase 1, Section 13, Part 1.
//
// It complements the existing DesignNotes.cs (Phase 3 entity shapes) by
// documenting the AI-service-layer learner memory architecture, the
// canonical-record isolation contract, the update-boundary rules, and the
// teacher-guide compatibility direction.
//
// Do not implement Phase 3 LearnerProfile entity here — see DesignNotes.cs.
//
// ── What the learner memory is ───────────────────────────────────────────────
//
//  A bounded, derived set of observable educational signals that help the
//  AI tutor behave more like a private teacher who remembers the learner
//  over time, while remaining:
//    - Explainable: every stored signal has an observable source
//    - Bounded: strict field set; no open-ended blob storage
//    - Privacy-aware: minimum data principle; no sensitive inferences
//    - Educationally grounded: all fields have a direct pedagogical purpose
//
// ── What the learner memory is NOT ───────────────────────────────────────────
//
//  ✗  Not a psychological profile or cognitive-style classification
//  ✗  Not a canonical academic record (enrollment, grades, progress)
//  ✗  Not a raw interaction log (AiConversation / AiMessage own that)
//  ✗  Not a full user-profile engine
//  ✗  Not a diagnosis-like labeling system
//  ✗  Not a behavioral telemetry or engagement-addiction store
//  ✗  Not general ability / IQ-style ranking
//
// ── Canonical-record isolation contract (enforced) ───────────────────────────
//
//  The learner memory layer NEVER writes back to:
//    - LessonProgress
//    - QuestionAnswerRecord
//    - Enrollment
//    - AiConversation / AiMessage
//
//  These are read-only source data for the observation layer.
//  Any violation of this rule is a breaking change for Phase 3.
//
//  The learner memory layer is the ONLY writer of:
//    - LearnerMemorySlice (AI service in-memory, Phase 1)
//    - LearnerProfile / TopicMasterySnapshot / ConsistencySignal (Phase 3, DB)
//
// ── Layer separation ──────────────────────────────────────────────────────────
//
//  Layer A — Explicit preferences (student-set or clearly observed):
//    explanation_style    Set only from explicit student statement or ≥2 repeated requests
//    pace_signal          Set only from explicit cues; never inferred from speed telemetry
//
//  Layer B — Derived academic signals (require repeated evidence):
//    weak_topics          MIN_WEAK_TOPIC_EVIDENCE (2) independent observations
//    observed_misconceptions  MIN_MISCONCEPTION_EVIDENCE (2) observations
//
//  Layer C — Short-lived session-adjacent signals (not promoted to A/B):
//    recent_friction_signals  Capped, evicted, never appear in tutor prompt
//
//  Canonical academic records (read-only source — never written here):
//    LessonProgress · QuestionAnswerRecord · Enrollment
//
//  Raw interaction history (read-only source — never written here):
//    AiConversation · AiMessage
//
// ── Update-boundary rules ─────────────────────────────────────────────────────
//
//  Direct (single interaction — stored immediately):
//    friction_observations             → FrictionSignal; capped at MAX (5); evict oldest
//    explanation_style (explicit)      → ExplanationStyleSignal(confidence=explicit)
//    pace_observation (explicit only)  → PaceSignalRecord(evidence_count++)
//
//  Requires repeated evidence (threshold-gated):
//    weak_topic_observations     → promoted only at ≥ MIN_WEAK_TOPIC_EVIDENCE (2) observations
//    misconception_observations  → promoted only at ≥ MIN_MISCONCEPTION_EVIDENCE (2) observations
//    explanation_style (inferred) → promoted only after ≥ 2 same-style requests
//
//  Remains temporary / session-local (discarded if threshold never met):
//    Single-interaction weak topic hint below threshold
//    Single-interaction non-explicit style signal below threshold
//    Pending observations are in-memory only (not persisted in Phase 1)
//    In Phase 3: persist pending in a learner_pending_observations table
//
//  MUST NOT be updated from a single AI interaction without repeated evidence:
//    - Any field that carries persistent educational judgment about the learner
//    - Any field whose value affects future prompt framing must pass the threshold
//
// ── Tutor-facing context slice boundaries ─────────────────────────────────────
//
//  Only a bounded subset of learner memory enters the tutor system prompt:
//    weak_topics              at most MAX_WEAK_TOPICS_IN_CONTEXT (3) most recent
//    observed_misconceptions  at most MAX_MISCONCEPTIONS_IN_CONTEXT (2) most recent
//    explanation_style        if any
//    pace_signal              if any
//
//  Excluded from tutor context (must never enter the system prompt):
//    recent_friction_signals  too session-adjacent; must not frame the system prompt
//    raw observation counts   implementation detail, not tutor-visible
//    user_id / tenant_id      raw identifiers must not appear in prompt text
//
//  Overclaiming guard:
//    The prompt formatter always uses hedged language:
//      'has shown difficulty with' — not 'struggles with' or 'cannot do'
//      'tends to prefer'           — not 'requires' or 'cannot learn without'
//    The block is labeled 'observed tendencies' and includes a caveat to
//    remain open to different behaviour in the current session.
//
// ── Teacher guide architecture compatibility ──────────────────────────────────
//
//  CRITICAL DESIGN DIRECTION (do not violate):
//
//  1. The student textbook / lesson_content is always the canonical learner-facing
//     study content. It is the WHAT-TO-STUDY source.
//
//  2. An optional teacher guide may exist for a subject/lesson. When it does:
//       - It is the HOW-TO-TEACH source (pedagogical method guidance).
//       - It has higher priority than lesson content for tutor teaching style.
//       - The AI service should use it for deciding HOW to explain, not WHAT.
//
//  3. The teacher guide is optional. The system must never assume it exists.
//       - If no teacher guide: tutor derives teaching approach from lesson content.
//       - If teacher guide exists: tutor can use it for pedagogical guidance.
//
//  4. Learner memory and teacher guide are ORTHOGONAL:
//       - Learner memory describes WHO the learner is (tendencies, preferences).
//       - Teacher guide describes HOW to teach the subject.
//       - They are formatted as separate prompt blocks; neither depends on the other.
//
//  5. Planned Phase 3 field in TutorContextSnapshot:
//       teaching_guide_available: bool  (not yet added — Phase 3 only)
//     When True, the backend also passes teacher-guide retrieval context as a
//     separate block in the prompt, between lesson content and learner memory.
//
// ── Phase 1 implementation ────────────────────────────────────────────────────
//
//  AI service (Python / FastAPI):
//    app.models.learner_memory          — Pydantic models (LearnerMemorySlice,
//                                         update input types, TutorFacingLearnerContext)
//    app.services.learner_memory_service — Service + InMemoryLearnerMemoryStore
//    app.services.learner_context_slice  — Prompt block formatter
//    app.models.tutor                   — TutorContextSnapshot.learner_memory_slice
//                                         (optional, None in Phase 1)
//    app.core.tutor_prompts             — build_system_prompt now includes optional
//                                         learner memory block (block 4 of 5)
//
//  Backend (.NET / C#):
//    Phase 1: No LearnerProfile entity or migration. Design is documented here.
//    Phase 3: LearnerProfile + LearnerPreferences + TopicMasterySnapshot +
//             ConsistencySignal entities (see DesignNotes.cs for shapes).
//             Backend assembles TutorFacingLearnerContext and passes it in the
//             TutorContextSnapshot forwarded to the AI service.
//
// ── Phase 3 migration path ────────────────────────────────────────────────────
//
//  1. Create LearnerProfile entity (see DesignNotes.cs for schema).
//  2. Create LearnerPreferences entity (Layer A fields).
//  3. Create TopicMasterySnapshot entity (Layer B — derived from QuestionAnswerRecord).
//  4. Create ConsistencySignal entity (Layer B — derived from LessonProgress).
//  5. Add ILearnerProfileService + implementation.
//  6. Backend TutorContextAssembler queries LearnerProfile and populates
//     TutorContextSnapshot.learner_memory_slice before forwarding to AI service.
//  7. AI service InMemoryLearnerMemoryStore is replaced by backend-managed store.
//  8. Pending observations table: learner_pending_observations
//     (user_id, tenant_id, signal_type, signal_key, count, latest_observed_at).
//     Enables cross-session threshold accumulation.
//
// ── GDPR / deletion scope (Phase 3 — complements DesignNotes.cs order) ────────
//
//  All learner memory tables must be deleted before LearnerProfile:
//    learner_pending_observations (Phase 3) →
//    ConsistencySignal → TopicMasterySnapshot → LearnerPreferences →
//    LearnerProfile
//  (LearnerProfile row deleted last, as navigation anchor)
//
// ── Deferred to Phase 3 ───────────────────────────────────────────────────────
//
//  ✗  LearnerProfile entity + EF Core config + migration
//  ✗  ILearnerProfileService interface + PostgreSQL-backed implementation
//  ✗  Backend TutorContextAssembler learner-memory population
//  ✗  Cross-session pending observations persistence
//  ✗  TopicMasterySnapshot refresh job / trigger
//  ✗  Teacher guide field (teaching_guide_available) in TutorContextSnapshot
//  ✗  Teacher guide retrieval context + prompt block
//  ✗  GDPR student data deletion service (see DesignNotes.cs for order)
//
public static class LearnerMemoryBoundaryNotes
{
    // This class exists solely to anchor the namespace and provide a
    // searchable symbol in IDE tooling. All content is in the file-level
    // comments above.
    //
    // Do not add instance members. Do not inherit from this class.
}

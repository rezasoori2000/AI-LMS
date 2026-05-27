"""
Learner memory service — applies update-boundary rules and manages the
LearnerMemorySlice store.

This is the ONLY writer of LearnerMemorySlice records. Nothing else may
write to the learner memory store — not StudentService, not TutorService,
not any admin or parent service.

CANONICAL ACADEMIC RECORDS are never written here:
  LessonProgress, QuestionAnswerRecord, Enrollment — these are read-only
  source data for the observation layer. Never written back to.

UPDATE-BOUNDARY RULES (full specification)
==========================================

Direct (single interaction — stored immediately):
  friction_observations
    → Stored as FrictionSignal; capped at MAX_FRICTION_SIGNALS_STORED.
      Oldest evicted when cap exceeded. No threshold required.
  explanation_style_observation (is_explicit_statement=True)
    → Stored immediately as ExplanationStyleSignal(confidence='explicit').
  pace_observation (is_explicit_statement=True)
    → Stored immediately as PaceSignalRecord(evidence_count=1 or +1 if exists).

Requires repeated evidence (threshold-gated):
  weak_topic_observations
    → First observation: tracked in session-local pending (NOT stored yet).
    → Each subsequent observation for same (subject_id, topic_label):
        If already promoted in the slice: increment evidence_count, update timestamp.
        If in pending and count reaches MIN_WEAK_TOPIC_EVIDENCE: promote to slice.
    → Eviction: when MAX_WEAK_TOPICS_STORED exceeded, oldest by last_observed_at
      are dropped.
  misconception_observations
    → Same threshold/eviction pattern as weak_topics.
  explanation_style_observation (is_explicit_statement=False)
    → Requires ≥ 2 accumulated non-explicit requests for same style.
    → Session-local pending count. If ≥ 2: stored as
      ExplanationStyleSignal(confidence='inferred_from_repeated_request').

Remains temporary / session-local (discarded if threshold never met):
  Single weak_topic or misconception observation that is never repeated.
  Single non-explicit style request that never accumulates to ≥ 2.
  The pending dict lives only in memory — it is not persisted.

PHASE 3 MIGRATION NOTE
=======================
  Phase 1: InMemoryLearnerMemoryStore — no real persistence. Test-friendly.
           Pending observations are per-service-instance (not cross-session).
  Phase 3: Replace InMemoryLearnerMemoryStore with a PostgreSQL-backed
           implementation. Pending observations should be persisted in a
           separate 'learner_pending_observations' table (one row per
           unthresholded signal) keyed by (user_id, tenant_id, signal_type,
           signal_key, count). This is a Phase 3 schema concern.

TEACHER GUIDE COMPATIBILITY NOTE
==================================
  This service has no dependency on teacher guide content. It is purely a
  learner observation layer. If a teacher guide exists for a subject, it
  affects HOW the tutor teaches — but that is handled in prompt assembly,
  not in learner memory. The two concerns are orthogonal.
"""
from __future__ import annotations

from abc import ABC, abstractmethod
from datetime import datetime, timezone
from dataclasses import dataclass, field

from app.models.learner_memory import (
    ExplanationStyleObservation,
    ExplanationStyleSignal,
    FrictionObservation,
    FrictionSignal,
    LearnerMemorySlice,
    LearnerMemoryUpdateInput,
    MAX_FRICTION_SIGNALS_STORED,
    MAX_MISCONCEPTIONS_STORED,
    MAX_WEAK_TOPICS_STORED,
    MIN_MISCONCEPTION_EVIDENCE,
    MIN_WEAK_TOPIC_EVIDENCE,
    MAX_WEAK_TOPICS_IN_CONTEXT,
    MAX_MISCONCEPTIONS_IN_CONTEXT,
    MisconceptionObservation,
    MisconceptionRecord,
    MisconceptionResolutionObservation,
    PaceObservation,
    PaceSignalRecord,
    SuccessfulRecoveryObservation,
    TutorFacingLearnerContext,
    WeakTopicObservation,
    WeakTopicRecord,
)
from app.models.learner_memory_policy import (
    CATEGORY_POLICIES,
    LearnerMemoryCategory,
    is_stale,
)
from app.services.learner_memory_safety import (
    LearnerMemoryGuardrailError,
    LearnerMemoryResetScope,
    LearnerMemorySafetyGuard,
)


def _utcnow() -> datetime:
    return datetime.now(tz=timezone.utc)


# ── Abstract store ────────────────────────────────────────────────────────────

class LearnerMemoryStore(ABC):
    """
    Abstract persistence interface for LearnerMemorySlice records.

    Phase 1: InMemoryLearnerMemoryStore (no real persistence, test-friendly).
    Phase 3: Replace with a PostgreSQL-backed implementation.

    The store is keyed by (user_id, tenant_id). A None tenant_id is a valid
    key (platform-wide learner, same convention as other domain entities).
    """

    @abstractmethod
    def get(self, user_id: str, tenant_id: str | None) -> LearnerMemorySlice | None:
        """Return the slice for this learner, or None if not yet created."""
        ...

    @abstractmethod
    def save(self, slice_: LearnerMemorySlice) -> None:
        """Persist or update the slice."""
        ...

    @abstractmethod
    def delete(self, user_id: str, tenant_id: str | None) -> None:
        """Remove the slice for this learner entirely. No-op if not found."""
        ...


class InMemoryLearnerMemoryStore(LearnerMemoryStore):
    """
    In-memory implementation for Phase 1 / unit testing.

    Not thread-safe. Replace before production use. The store is a plain dict
    keyed by (user_id, tenant_id).
    """

    def __init__(self) -> None:
        self._store: dict[tuple[str, str | None], LearnerMemorySlice] = {}

    def get(self, user_id: str, tenant_id: str | None) -> LearnerMemorySlice | None:
        return self._store.get((user_id, tenant_id))

    def save(self, slice_: LearnerMemorySlice) -> None:
        self._store[(slice_.user_id, slice_.tenant_id)] = slice_

    def delete(self, user_id: str, tenant_id: str | None) -> None:
        self._store.pop((user_id, tenant_id), None)


# ── Session-local pending observations ───────────────────────────────────────

@dataclass
class _PendingEntry:
    """Accumulator for a single pending observation type."""
    count: int = 0
    latest_observed_at: datetime = field(default_factory=_utcnow)


@dataclass
class _UserPending:
    """
    Session-local accumulator for a single user's unthresholded observations.

    Lives only in memory. Not persisted. Cleared explicitly via
    clear_session_pending().
    """
    # key = f"{subject_id}::{topic_label}"
    weak_topics: dict[str, _PendingEntry] = field(default_factory=dict)
    # key = f"{subject_id}::{description}"
    misconceptions: dict[str, _PendingEntry] = field(default_factory=dict)
    # key = style value
    inferred_styles: dict[str, _PendingEntry] = field(default_factory=dict)


# ── Service ───────────────────────────────────────────────────────────────────

class LearnerMemoryService:
    """
    Applies update-boundary rules and manages the LearnerMemorySlice.

    This is the sole writer of learner memory. Canonical academic records
    are never written by this service.

    Usage pattern (Phase 1):
      service = LearnerMemoryService(InMemoryLearnerMemoryStore())
      # At the end of a tutor interaction (or whenever observations are collected):
      updated = service.apply_update(update_input)
      # At the end of a session:
      service.clear_session_pending(user_id)
      # To assemble the tutor-facing context slice:
      context = service.get_tutor_facing_context(user_id, tenant_id)
    """

    def __init__(self, store: LearnerMemoryStore) -> None:
        self._store = store
        # Session-local pending dict, keyed by user_id.
        # Not persisted — Phase 3 will use a DB table for this.
        self._pending: dict[str, _UserPending] = {}
        self._safety_guard = LearnerMemorySafetyGuard()

    # ── Internal helpers ──────────────────────────────────────────────────────

    def _get_or_create(self, user_id: str, tenant_id: str | None) -> LearnerMemorySlice:
        existing = self._store.get(user_id, tenant_id)
        if existing is not None:
            return existing
        now = _utcnow()
        return LearnerMemorySlice(
            user_id=user_id,
            tenant_id=tenant_id,
            created_at=now,
            updated_at=now,
        )

    def _pending_for(self, user_id: str) -> _UserPending:
        if user_id not in self._pending:
            self._pending[user_id] = _UserPending()
        return self._pending[user_id]

    # ── Public API ────────────────────────────────────────────────────────────

    def apply_update(self, update: LearnerMemoryUpdateInput) -> LearnerMemorySlice:
        """
        Apply all observations from a single interaction to the learner memory.

        Evaluates each observation against the update-boundary rules. Returns
        the (possibly unchanged) LearnerMemorySlice after applying the rules.

        Call this at the end of a tutor interaction or at any checkpoint where
        observations have been collected from the session.

        Raises:
            LearnerMemoryGuardrailError: if any topic label or misconception
                description contains forbidden content (diagnostic labels,
                psychological claims, ability-ranking language, or engagement
                manipulation signals). Raised before any state change —
                guarantees atomicity: either the full update is applied or
                nothing is.
        """
        # Safety guardrail — validate ALL text content before touching any state
        violations = self._safety_guard.validate_update_observations(
            weak_topics=update.weak_topic_observations,
            misconceptions=update.misconception_observations,
        )
        if violations:
            raise LearnerMemoryGuardrailError(violations)

        slice_ = self._get_or_create(update.user_id, update.tenant_id)
        pending = self._pending_for(update.user_id)
        changed = False

        # ── Friction signals (direct, short-lived) ────────────────────────────
        for obs in update.friction_observations:
            slice_.recent_friction_signals.append(
                FrictionSignal(
                    description=obs.description,
                    lesson_id=obs.lesson_id,
                    observed_at=obs.observed_at,
                )
            )
            changed = True
        if len(slice_.recent_friction_signals) > MAX_FRICTION_SIGNALS_STORED:
            # Evict oldest
            slice_.recent_friction_signals = (
                slice_.recent_friction_signals[-MAX_FRICTION_SIGNALS_STORED:]
            )

        # ── Explanation style ─────────────────────────────────────────────────
        if update.explanation_style_observation is not None:
            obs: ExplanationStyleObservation = update.explanation_style_observation

            if obs.is_explicit_statement:
                # Explicit statement → store directly, unconditionally
                slice_.explanation_style = ExplanationStyleSignal(
                    style=obs.style,
                    confidence="explicit",
                    observed_at=obs.observed_at,
                )
                changed = True
            else:
                # Inferred → accumulate in pending; promote only at ≥ 2
                entry = pending.inferred_styles.get(obs.style, _PendingEntry())
                entry.count += 1
                entry.latest_observed_at = obs.observed_at
                pending.inferred_styles[obs.style] = entry
                if entry.count >= 2:
                    slice_.explanation_style = ExplanationStyleSignal(
                        style=obs.style,
                        confidence="inferred_from_repeated_request",
                        observed_at=obs.observed_at,
                    )
                    changed = True

        # ── Pace signal (explicit only) ───────────────────────────────────────
        if update.pace_observation is not None:
            obs_pace: PaceObservation = update.pace_observation
            if obs_pace.is_explicit_statement:
                existing_pace = slice_.pace_signal
                new_count = (existing_pace.evidence_count + 1) if existing_pace else 1
                slice_.pace_signal = PaceSignalRecord(
                    signal=obs_pace.signal,
                    evidence_count=new_count,
                    last_observed_at=obs_pace.observed_at,
                )
                changed = True
            # Non-explicit pace signals are discarded (never auto-inferred)

        # ── Weak topics (threshold-gated) ─────────────────────────────────────
        for obs_wt in update.weak_topic_observations:
            key = f"{obs_wt.subject_id}::{obs_wt.topic_label}"

            # Check if already promoted
            already = next(
                (r for r in slice_.weak_topics
                 if r.subject_id == obs_wt.subject_id
                 and r.topic_label == obs_wt.topic_label),
                None,
            )
            if already is not None:
                # Already promoted — increment evidence count
                idx = slice_.weak_topics.index(already)
                slice_.weak_topics[idx] = WeakTopicRecord(
                    topic_label=already.topic_label,
                    subject_id=already.subject_id,
                    evidence_count=already.evidence_count + 1,
                    last_observed_at=obs_wt.observed_at,
                )
                changed = True
            else:
                # Accumulate in pending
                entry = pending.weak_topics.get(key, _PendingEntry())
                entry.count += 1
                entry.latest_observed_at = obs_wt.observed_at
                pending.weak_topics[key] = entry

                if entry.count >= MIN_WEAK_TOPIC_EVIDENCE:
                    # Threshold reached — promote to slice
                    slice_.weak_topics.append(
                        WeakTopicRecord(
                            topic_label=obs_wt.topic_label,
                            subject_id=obs_wt.subject_id,
                            evidence_count=entry.count,
                            last_observed_at=obs_wt.observed_at,
                        )
                    )
                    # Evict oldest if over cap
                    if len(slice_.weak_topics) > MAX_WEAK_TOPICS_STORED:
                        slice_.weak_topics.sort(
                            key=lambda r: r.last_observed_at, reverse=True
                        )
                        slice_.weak_topics = slice_.weak_topics[:MAX_WEAK_TOPICS_STORED]
                    changed = True

        # ── Misconceptions (threshold-gated) ──────────────────────────────────
        for obs_mc in update.misconception_observations:
            key = f"{obs_mc.subject_id}::{obs_mc.description}"

            already_mc = next(
                (r for r in slice_.observed_misconceptions
                 if r.subject_id == obs_mc.subject_id
                 and r.description == obs_mc.description),
                None,
            )
            if already_mc is not None:
                idx = slice_.observed_misconceptions.index(already_mc)
                slice_.observed_misconceptions[idx] = MisconceptionRecord(
                    description=already_mc.description,
                    subject_id=already_mc.subject_id,
                    evidence_count=already_mc.evidence_count + 1,
                    last_observed_at=obs_mc.observed_at,
                )
                changed = True
            else:
                entry = pending.misconceptions.get(key, _PendingEntry())
                entry.count += 1
                entry.latest_observed_at = obs_mc.observed_at
                pending.misconceptions[key] = entry

                if entry.count >= MIN_MISCONCEPTION_EVIDENCE:
                    slice_.observed_misconceptions.append(
                        MisconceptionRecord(
                            description=obs_mc.description,
                            subject_id=obs_mc.subject_id,
                            evidence_count=entry.count,
                            last_observed_at=obs_mc.observed_at,
                        )
                    )
                    if len(slice_.observed_misconceptions) > MAX_MISCONCEPTIONS_STORED:
                        slice_.observed_misconceptions.sort(
                            key=lambda r: r.last_observed_at, reverse=True
                        )
                        slice_.observed_misconceptions = (
                            slice_.observed_misconceptions[:MAX_MISCONCEPTIONS_STORED]
                        )
                    changed = True

        # ── Recovery signals (positive counter-evidence) ──────────────────────
        for obs_rec in update.weak_topic_recovery_observations:
            if self._apply_weak_topic_recovery(slice_, pending, obs_rec):
                changed = True

        for obs_res in update.misconception_resolution_observations:
            if self._apply_misconception_resolution(slice_, pending, obs_res):
                changed = True

        if changed:
            slice_.updated_at = _utcnow()
            self._store.save(slice_)

        return slice_

    def _apply_weak_topic_recovery(
        self,
        slice_: LearnerMemorySlice,
        pending: _UserPending,
        obs: SuccessfulRecoveryObservation,
    ) -> bool:
        """
        Apply one SuccessfulRecoveryObservation to the slice.

        Decrement evidence_count on any matching promoted WeakTopicRecord.
        If evidence_count - 1 < MIN_WEAK_TOPIC_EVIDENCE: remove the record.
        Also clear from pending if present (recovery cancels an unpromotable signal).
        Returns True if the slice was modified.
        """
        changed = False
        key = f"{obs.subject_id}::{obs.topic_label}"

        # Clear from pending regardless
        if key in pending.weak_topics:
            del pending.weak_topics[key]

        # Decrement or remove the promoted record
        existing = next(
            (r for r in slice_.weak_topics
             if r.subject_id == obs.subject_id and r.topic_label == obs.topic_label),
            None,
        )
        if existing is not None:
            if existing.evidence_count - 1 < MIN_WEAK_TOPIC_EVIDENCE:
                slice_.weak_topics.remove(existing)
            else:
                idx = slice_.weak_topics.index(existing)
                slice_.weak_topics[idx] = WeakTopicRecord(
                    topic_label=existing.topic_label,
                    subject_id=existing.subject_id,
                    evidence_count=existing.evidence_count - 1,
                    last_observed_at=obs.observed_at,
                )
            changed = True

        return changed

    def _apply_misconception_resolution(
        self,
        slice_: LearnerMemorySlice,
        pending: _UserPending,
        obs: MisconceptionResolutionObservation,
    ) -> bool:
        """
        Apply one MisconceptionResolutionObservation to the slice.

        Same decrement/remove pattern as weak topic recovery.
        Returns True if the slice was modified.
        """
        changed = False
        key = f"{obs.subject_id}::{obs.description}"

        if key in pending.misconceptions:
            del pending.misconceptions[key]

        existing = next(
            (r for r in slice_.observed_misconceptions
             if r.subject_id == obs.subject_id and r.description == obs.description),
            None,
        )
        if existing is not None:
            if existing.evidence_count - 1 < MIN_MISCONCEPTION_EVIDENCE:
                slice_.observed_misconceptions.remove(existing)
            else:
                idx = slice_.observed_misconceptions.index(existing)
                slice_.observed_misconceptions[idx] = MisconceptionRecord(
                    description=existing.description,
                    subject_id=existing.subject_id,
                    evidence_count=existing.evidence_count - 1,
                    last_observed_at=obs.observed_at,
                )
            changed = True

        return changed

    def get_tutor_facing_context(
        self,
        user_id: str,
        tenant_id: str | None,
    ) -> TutorFacingLearnerContext:
        """
        Build the narrow, privacy-aware tutor-facing context slice.

        Returns an empty TutorFacingLearnerContext (has_memory=False) if no
        memory exists yet for this learner. The prompt builder omits the
        learner memory block entirely in that case.

        Stale records (older than the category's decay_days) are filtered out
        before building the slice. Filtering is soft — records remain in the
        store until a Phase 3 cleanup job removes them.

        This slice is READ-ONLY — it is never written back to the store.
        """
        slice_ = self._store.get(user_id, tenant_id)
        if slice_ is None:
            return TutorFacingLearnerContext(user_id=user_id, has_memory=False)

        weak_policy = CATEGORY_POLICIES[LearnerMemoryCategory.WEAK_TOPIC]
        misc_policy = CATEGORY_POLICIES[LearnerMemoryCategory.MISCONCEPTION]

        # Filter stale records before building the context slice
        active_weak = [
            t for t in slice_.weak_topics
            if not is_stale(t.last_observed_at, weak_policy.decay_days)
        ]
        active_misc = [
            m for m in slice_.observed_misconceptions
            if not is_stale(m.last_observed_at, misc_policy.decay_days)
        ]

        # Sort by most recent, then cap to context limits
        sorted_weak = sorted(
            active_weak,
            key=lambda r: r.last_observed_at,
            reverse=True,
        )[:MAX_WEAK_TOPICS_IN_CONTEXT]

        sorted_misc = sorted(
            active_misc,
            key=lambda r: r.last_observed_at,
            reverse=True,
        )[:MAX_MISCONCEPTIONS_IN_CONTEXT]

        has_memory = bool(
            sorted_weak
            or sorted_misc
            or slice_.explanation_style
            or slice_.pace_signal
        )

        return TutorFacingLearnerContext(
            user_id=user_id,
            weak_topics=sorted_weak,
            observed_misconceptions=sorted_misc,
            explanation_style=slice_.explanation_style,
            pace_signal=slice_.pace_signal,
            has_memory=has_memory,
        )

    def clear_session_pending(self, user_id: str) -> None:
        """
        Discard all session-local pending observations for this user.

        Call this when a tutor session ends. Pending observations that never
        met the evidence threshold are discarded — they remain temporary and
        do not persist to the learner profile.
        """
        self._pending.pop(user_id, None)

    def reset_memory(
        self,
        user_id: str,
        tenant_id: str | None,
        scope: LearnerMemoryResetScope,
    ) -> None:
        """
        Reset a specific scope of learner memory for a user.

        This is a SOFT operation — the LearnerMemorySlice record is preserved
        in the store with the relevant fields cleared. For hard deletion
        (account closure, GDPR erasure), use delete_user_memory().

        No-op if no slice exists for this learner (except SESSION_PENDING,
        which clears the in-memory pending dict regardless of slice state).

        Scope semantics:
          SESSION_PENDING : clears the _pending dict only; slice unchanged.
          TOPIC_SIGNALS   : clears weak_topics + observed_misconceptions from
                            the slice; clears pending.weak_topics + .misconceptions.
          PREFERENCES     : clears explanation_style + pace_signal from the slice;
                            clears pending.inferred_styles.
          FRICTION        : clears recent_friction_signals from the slice.
          ALL             : clears all signals from the slice and all pending.

        Phase 3 note: this operation should write an audit log entry (actor,
        timestamp, scope) before clearing. Not implemented in Phase 1.
        """
        if scope == LearnerMemoryResetScope.SESSION_PENDING:
            self._pending.pop(user_id, None)
            return

        # Clear pending entries for the relevant scope unconditionally —
        # pending may exist even if no slice has been saved yet (e.g. when an
        # observation accumulated in pending but never reached the threshold).
        pending = self._pending.get(user_id)
        if scope in (
            LearnerMemoryResetScope.TOPIC_SIGNALS,
            LearnerMemoryResetScope.ALL,
        ):
            if pending is not None:
                pending.weak_topics.clear()
                pending.misconceptions.clear()
        if scope in (
            LearnerMemoryResetScope.PREFERENCES,
            LearnerMemoryResetScope.ALL,
        ):
            if pending is not None:
                pending.inferred_styles.clear()
        if scope == LearnerMemoryResetScope.ALL:
            self._pending.pop(user_id, None)

        slice_ = self._store.get(user_id, tenant_id)
        if slice_ is None:
            return

        changed = False

        if scope == LearnerMemoryResetScope.TOPIC_SIGNALS:
            if slice_.weak_topics:
                slice_.weak_topics.clear()
                changed = True
            if slice_.observed_misconceptions:
                slice_.observed_misconceptions.clear()
                changed = True

        elif scope == LearnerMemoryResetScope.PREFERENCES:
            if slice_.explanation_style is not None:
                slice_.explanation_style = None
                changed = True
            if slice_.pace_signal is not None:
                slice_.pace_signal = None
                changed = True

        elif scope == LearnerMemoryResetScope.FRICTION:
            if slice_.recent_friction_signals:
                slice_.recent_friction_signals.clear()
                changed = True

        elif scope == LearnerMemoryResetScope.ALL:
            slice_.weak_topics.clear()
            slice_.observed_misconceptions.clear()
            slice_.recent_friction_signals.clear()
            slice_.explanation_style = None
            slice_.pace_signal = None
            changed = True

        if changed:
            slice_.updated_at = _utcnow()
            self._store.save(slice_)

    def delete_user_memory(self, user_id: str, tenant_id: str | None) -> None:
        """
        Hard-delete all learner memory for a user.

        Removes the LearnerMemorySlice from the store entirely AND clears
        all session-local pending observations. No-op if no slice exists.

        Use this for:
          - Account closure
          - Parent-requested full memory deletion
          - GDPR / right-to-erasure requests

        After this call, get_tutor_facing_context() returns has_memory=False.
        The next apply_update() call will create a fresh slice.

        Phase 3 note:
          - Write an audit log entry before deletion
          - Expose via a parent/admin-only API endpoint with appropriate auth
          - Also delete related PostgreSQL rows (pending_observations, etc.)
            from plans in the Phase 3 schema — those are NOT cascade-deleted here
        """
        self._pending.pop(user_id, None)
        self._store.delete(user_id, tenant_id)

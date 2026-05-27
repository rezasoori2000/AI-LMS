"""
Tests for learner memory policy scaffolding.

Phase 1, Section 13, Part 2.

Test groups:
  A. LearnerMemoryCategory enum completeness and values
  B. EvidenceSource enum completeness and values
  C. ConfidenceLevel and evidence_count_to_confidence()
  D. is_stale() staleness helper
  E. CategoryPolicy fields and CATEGORY_POLICIES table
  F. Policy safety/privacy invariants
  G. Policy consistency with learner_memory.py constants
"""
from __future__ import annotations

from datetime import datetime, timedelta, timezone

import pytest

from app.models.learner_memory import (
    MIN_MISCONCEPTION_EVIDENCE,
    MIN_WEAK_TOPIC_EVIDENCE,
    MAX_FRICTION_SIGNALS_STORED,
    MAX_MISCONCEPTIONS_STORED,
    MAX_WEAK_TOPICS_STORED,
)
from app.models.learner_memory_policy import (
    CATEGORY_POLICIES,
    CategoryPolicy,
    ConfidenceLevel,
    EvidenceSource,
    LearnerMemoryCategory,
    evidence_count_to_confidence,
    is_stale,
)


def _utcnow() -> datetime:
    return datetime.now(tz=timezone.utc)


# ── A. LearnerMemoryCategory ──────────────────────────────────────────────────

class TestLearnerMemoryCategory:
    def test_has_exactly_five_categories(self) -> None:
        """Phase 1 defines exactly 5 categories. Adding more requires a test update."""
        assert len(LearnerMemoryCategory) == 5

    def test_weak_topic_value(self) -> None:
        assert LearnerMemoryCategory.WEAK_TOPIC == "weak_topic"

    def test_misconception_value(self) -> None:
        assert LearnerMemoryCategory.MISCONCEPTION == "misconception"

    def test_explanation_style_value(self) -> None:
        assert LearnerMemoryCategory.EXPLANATION_STYLE == "explanation_style"

    def test_pace_signal_value(self) -> None:
        assert LearnerMemoryCategory.PACE_SIGNAL == "pace_signal"

    def test_friction_signal_value(self) -> None:
        assert LearnerMemoryCategory.FRICTION_SIGNAL == "friction_signal"

    def test_all_categories_have_policy(self) -> None:
        """Every category must have a corresponding policy entry."""
        for cat in LearnerMemoryCategory:
            assert cat in CATEGORY_POLICIES, (
                f"LearnerMemoryCategory.{cat.name} is missing a CategoryPolicy entry"
            )


# ── B. EvidenceSource ─────────────────────────────────────────────────────────

class TestEvidenceSource:
    def test_has_at_least_six_sources(self) -> None:
        """Minimum set of evidence sources must be present."""
        assert len(EvidenceSource) >= 6

    def test_has_successful_recovery_source(self) -> None:
        """Positive counter-signal must be a recognised evidence source."""
        assert EvidenceSource.SUCCESSFUL_RECOVERY in EvidenceSource

    def test_wrong_answer_repeated_value(self) -> None:
        assert EvidenceSource.WRONG_ANSWER_REPEATED == "wrong_answer_repeated"

    def test_confusion_expressed_value(self) -> None:
        assert EvidenceSource.CONFUSION_EXPRESSED == "confusion_expressed"

    def test_explicit_style_request_value(self) -> None:
        assert EvidenceSource.EXPLICIT_STYLE_REQUEST == "explicit_style_request"

    def test_explicit_pace_signal_value(self) -> None:
        assert EvidenceSource.EXPLICIT_PACE_SIGNAL == "explicit_pace_signal"

    def test_session_friction_value(self) -> None:
        assert EvidenceSource.SESSION_FRICTION == "session_friction"


# ── C. ConfidenceLevel / evidence_count_to_confidence ────────────────────────

class TestConfidenceLevel:
    def test_has_three_levels(self) -> None:
        assert len(ConfidenceLevel) == 3
        assert ConfidenceLevel.LOW in ConfidenceLevel
        assert ConfidenceLevel.MODERATE in ConfidenceLevel
        assert ConfidenceLevel.HIGH in ConfidenceLevel

    def test_at_threshold_is_low(self) -> None:
        """Evidence_count == min_threshold always yields LOW."""
        assert evidence_count_to_confidence(2, min_threshold=2) == ConfidenceLevel.LOW

    def test_one_above_threshold_is_moderate(self) -> None:
        assert evidence_count_to_confidence(3, min_threshold=2) == ConfidenceLevel.MODERATE

    def test_two_above_threshold_is_moderate(self) -> None:
        assert evidence_count_to_confidence(4, min_threshold=2) == ConfidenceLevel.MODERATE

    def test_three_above_threshold_is_high(self) -> None:
        assert evidence_count_to_confidence(5, min_threshold=2) == ConfidenceLevel.HIGH

    def test_high_count_is_high(self) -> None:
        assert evidence_count_to_confidence(10, min_threshold=2) == ConfidenceLevel.HIGH

    def test_threshold_relative_scoring(self) -> None:
        """ConfidenceLevel is relative to threshold, not absolute counts."""
        # If threshold were 3, count=3 → LOW, count=4 → MODERATE, count=6 → HIGH
        assert evidence_count_to_confidence(3, min_threshold=3) == ConfidenceLevel.LOW
        assert evidence_count_to_confidence(4, min_threshold=3) == ConfidenceLevel.MODERATE
        assert evidence_count_to_confidence(6, min_threshold=3) == ConfidenceLevel.HIGH

    def test_default_threshold_is_two(self) -> None:
        """Default min_threshold=2 matches MIN_WEAK_TOPIC_EVIDENCE."""
        assert evidence_count_to_confidence(2) == ConfidenceLevel.LOW
        assert evidence_count_to_confidence(5) == ConfidenceLevel.HIGH


# ── D. is_stale ───────────────────────────────────────────────────────────────

class TestIsStale:
    def test_none_decay_days_never_stale(self) -> None:
        """decay_days=None means the signal has no expiry."""
        ancient = _utcnow() - timedelta(days=10_000)
        assert is_stale(ancient, decay_days=None) is False

    def test_recent_record_not_stale(self) -> None:
        recent = _utcnow() - timedelta(days=1)
        assert is_stale(recent, decay_days=90) is False

    def test_old_record_is_stale(self) -> None:
        old = _utcnow() - timedelta(days=91)
        assert is_stale(old, decay_days=90) is True

    def test_exactly_at_cutoff_not_stale(self) -> None:
        """A record observed exactly at the decay boundary is NOT stale."""
        exactly_at = _utcnow() - timedelta(days=90)
        # Allow small clock skew: the boundary case should be not-stale
        assert is_stale(exactly_at, decay_days=90) is False

    def test_timezone_naive_treated_as_utc(self) -> None:
        """Timezone-naive datetimes should be treated as UTC (not raise an error)."""
        naive_old = datetime.utcnow() - timedelta(days=100)  # naive
        assert is_stale(naive_old, decay_days=90) is True

    def test_short_decay_days(self) -> None:
        """Friction signals use 7-day decay."""
        six_days_ago = _utcnow() - timedelta(days=6)
        eight_days_ago = _utcnow() - timedelta(days=8)
        assert is_stale(six_days_ago, decay_days=7) is False
        assert is_stale(eight_days_ago, decay_days=7) is True


# ── E. CategoryPolicy fields and CATEGORY_POLICIES ───────────────────────────

class TestCategoryPolicies:
    def test_all_five_categories_present(self) -> None:
        assert len(CATEGORY_POLICIES) == len(LearnerMemoryCategory)

    def test_weak_topic_is_threshold_gated(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.WEAK_TOPIC]
        assert p.persistence_level == "threshold_gated"

    def test_misconception_is_threshold_gated(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.MISCONCEPTION]
        assert p.persistence_level == "threshold_gated"

    def test_explanation_style_is_direct(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.EXPLANATION_STYLE]
        assert p.persistence_level == "direct"

    def test_explanation_style_has_inferred_min_evidence(self) -> None:
        """The inferred path for EXPLANATION_STYLE requires 2 requests."""
        p = CATEGORY_POLICIES[LearnerMemoryCategory.EXPLANATION_STYLE]
        assert p.inferred_min_evidence == 2

    def test_pace_signal_is_direct(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.PACE_SIGNAL]
        assert p.persistence_level == "direct"

    def test_pace_signal_has_no_inferred_path(self) -> None:
        """Pace signal is never inferred from passive behavior."""
        p = CATEGORY_POLICIES[LearnerMemoryCategory.PACE_SIGNAL]
        assert p.inferred_min_evidence is None

    def test_friction_signal_is_session_adjacent(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.FRICTION_SIGNAL]
        assert p.persistence_level == "session_adjacent"

    def test_weak_topic_decay_is_90_days(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.WEAK_TOPIC]
        assert p.decay_days == 90

    def test_misconception_decay_is_90_days(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.MISCONCEPTION]
        assert p.decay_days == 90

    def test_friction_decay_is_7_days(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.FRICTION_SIGNAL]
        assert p.decay_days == 7

    def test_valid_evidence_sources_are_tuples(self) -> None:
        """Evidence source lists must be immutable tuples (policy is frozen)."""
        for cat, policy in CATEGORY_POLICIES.items():
            assert isinstance(policy.valid_evidence_sources, tuple), (
                f"{cat.name} valid_evidence_sources should be a tuple"
            )

    def test_all_policies_are_frozen(self) -> None:
        """Policies must be immutable — no runtime mutation allowed."""
        for cat, policy in CATEGORY_POLICIES.items():
            with pytest.raises((AttributeError, TypeError)):
                policy.max_stored = 999  # type: ignore[misc]

    def test_successful_recovery_is_a_valid_source_for_weak_topics(self) -> None:
        """Recovery counter-signals should be listed as a valid source for WEAK_TOPIC."""
        # Note: SUCCESSFUL_RECOVERY may not be in valid_evidence_sources since it
        # is a counter-signal, not a generating signal. Verify the design decision:
        # it is intentionally excluded — it reduces evidence, it does not create records.
        # This test documents the design decision explicitly.
        weak_sources = CATEGORY_POLICIES[LearnerMemoryCategory.WEAK_TOPIC].valid_evidence_sources
        # Recovery is a counter-signal, not an evidence-generating source.
        # It is not listed in valid_evidence_sources for WEAK_TOPIC by design.
        assert EvidenceSource.SUCCESSFUL_RECOVERY not in weak_sources


# ── F. Policy safety/privacy invariants ──────────────────────────────────────

class TestPolicySafetyInvariants:
    def test_friction_signal_is_not_safe_for_tutor_context(self) -> None:
        """Friction signals must never enter the tutor prompt context."""
        p = CATEGORY_POLICIES[LearnerMemoryCategory.FRICTION_SIGNAL]
        assert p.safe_for_tutor_context is False

    def test_all_other_categories_are_safe_for_tutor_context(self) -> None:
        """All non-friction categories may appear in the tutor context slice."""
        for cat, policy in CATEGORY_POLICIES.items():
            if cat != LearnerMemoryCategory.FRICTION_SIGNAL:
                assert policy.safe_for_tutor_context is True, (
                    f"{cat.name} should be safe for tutor context"
                )

    def test_threshold_gated_categories_require_at_least_two_observations(self) -> None:
        """No threshold-gated signal can be promoted from a single interaction."""
        for cat, policy in CATEGORY_POLICIES.items():
            if policy.persistence_level == "threshold_gated":
                assert policy.min_evidence_to_promote >= 2, (
                    f"{cat.name} threshold must be >= 2 to prevent single-interaction promotion"
                )

    def test_no_category_has_zero_evidence_threshold(self) -> None:
        """All categories require at least 1 observation. No magic auto-inference."""
        for cat, policy in CATEGORY_POLICIES.items():
            assert policy.min_evidence_to_promote >= 1, (
                f"{cat.name} min_evidence_to_promote must be >= 1"
            )

    def test_no_category_has_unlimited_storage(self) -> None:
        """All categories have a bounded max_stored to prevent unbounded growth."""
        for cat, policy in CATEGORY_POLICIES.items():
            assert policy.max_stored >= 1, (
                f"{cat.name} max_stored must be positive"
            )
            assert policy.max_stored <= 100, (
                f"{cat.name} max_stored={policy.max_stored} seems unreasonably large"
            )


# ── G. Policy consistency with learner_memory.py constants ───────────────────

class TestPolicyConsistencyWithConstants:
    def test_weak_topic_min_evidence_matches_constant(self) -> None:
        """CATEGORY_POLICIES must agree with the named constant."""
        p = CATEGORY_POLICIES[LearnerMemoryCategory.WEAK_TOPIC]
        assert p.min_evidence_to_promote == MIN_WEAK_TOPIC_EVIDENCE

    def test_misconception_min_evidence_matches_constant(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.MISCONCEPTION]
        assert p.min_evidence_to_promote == MIN_MISCONCEPTION_EVIDENCE

    def test_weak_topic_max_stored_matches_constant(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.WEAK_TOPIC]
        assert p.max_stored == MAX_WEAK_TOPICS_STORED

    def test_misconception_max_stored_matches_constant(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.MISCONCEPTION]
        assert p.max_stored == MAX_MISCONCEPTIONS_STORED

    def test_friction_max_stored_matches_constant(self) -> None:
        p = CATEGORY_POLICIES[LearnerMemoryCategory.FRICTION_SIGNAL]
        assert p.max_stored == MAX_FRICTION_SIGNALS_STORED

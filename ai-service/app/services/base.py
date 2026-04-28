"""
Service layer base interfaces.

Each service encapsulates a specific AI capability and depends on the
BaseLLMProvider abstraction — never on a concrete provider directly.

Services are the single place where prompt construction, output parsing,
and business rules live. They are called by API route handlers.

Phase 2+ implementation plan:
  TutoringService      — session-based AI tutoring, context management
  RAGService           — document ingestion, chunking, retrieval, augmentation
  ScoringService       — questionnaire / open-answer scoring and explanation
  InsightService       — learner progress inference, weak-area detection
  RecommendationService — next-lesson and content path suggestions
"""
from __future__ import annotations

from abc import ABC


class BaseAIService(ABC):
    """
    Marker base class for all AI service implementations.

    Concrete services should define their own abstract methods specific
    to their domain. The shared base allows type-safe dependency injection
    and uniform lifecycle management.
    """


# ── Per-service placeholder interfaces ──────────────────────────────────────
# These stubs define the contracts that Phase 2 implementations must fulfil.
# They live here so route handlers can import and type-annotate against them
# before the real implementations exist.


class BaseTutoringService(BaseAIService):
    """
    Manages AI tutoring sessions for individual learners.

    Phase 2 responsibilities:
      - Maintain per-session conversation context
      - Select appropriate prompts based on learner profile and curriculum
      - Stream responses for real-time interaction
      - Support multilingual prompts (RTL and LTR)
    """


class BaseRAGService(BaseAIService):
    """
    Retrieval-Augmented Generation pipeline.

    Phase 2 responsibilities:
      - Ingest curriculum documents (PDF, DOCX, text)
      - Chunk, embed, and store documents in a vector store
      - Retrieve semantically relevant context for a given query
      - Compose augmented prompts for the LLM

    Phase 3+:
      - Multi-tenant document isolation
      - Incremental index updates
    """


class BaseScoringService(BaseAIService):
    """
    Questionnaire and open-answer scoring.

    Phase 2 responsibilities:
      - Score multiple-choice, short-answer, and essay responses
      - Return structured score + rationale
      - Support rubric-based scoring
      - Language-aware scoring for multilingual content
    """


class BaseInsightService(BaseAIService):
    """
    Learner progress insight and weak-area inference.

    Phase 2 responsibilities:
      - Analyse assessment history to identify knowledge gaps
      - Produce natural-language progress summaries for teachers/parents
      - Feed signals into the recommendation engine
    """


class BaseRecommendationService(BaseAIService):
    """
    Content and learning path recommendation.

    Phase 2 responsibilities:
      - Recommend next lessons based on progress and performance
      - Personalise difficulty and pacing per learner
      - Support curriculum-aligned learning paths
    """

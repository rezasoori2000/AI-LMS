"""
Internal lesson ingestion/indexing pipeline sketch.

This module defines narrow, explicit boundaries for turning canonical lesson
content into derived retrieval artifacts.

Included in Part 2:
  - Lesson event handling (create/update/unpublish/delete)
  - Chunk preparation via retrieval_prep
  - Embedding provider boundary (interface only)
  - Artifact repository boundary (interface + in-memory scaffold)
  - Lesson-scoped retrieval query boundary

Intentionally excluded:
  - Production vector DB integration
  - Production embedding provider implementation
  - Async workers/queues and large-scale indexing orchestration
"""
from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from enum import Enum
from typing import Any

from app.services.retrieval_prep import LessonRetrievalChunk, build_lesson_chunks


class LessonIndexEvent(str, Enum):
    CREATED = "created"
    UPDATED = "updated"
    UNPUBLISHED = "unpublished"
    DELETED = "deleted"


@dataclass(frozen=True, slots=True)
class LessonIngestionInput:
    """Canonical lesson data needed to derive retrieval artifacts."""

    lesson_id: str
    chapter_id: str
    lesson_title: str
    lesson_content: str
    subject_name: str
    grade_name: str
    tenant_id: str | None
    event: LessonIndexEvent


@dataclass(frozen=True, slots=True)
class RetrievalArtifact:
    """Derived retrieval artifact stored independently from canonical lessons."""

    chunk_id: str
    lesson_id: str
    text: str
    metadata: dict[str, Any]
    embedding: list[float] | None = None


@dataclass(frozen=True, slots=True)
class RetrievalCandidate:
    chunk_id: str
    lesson_id: str
    text: str
    metadata: dict[str, Any]
    score: float


@dataclass(frozen=True, slots=True)
class RetrievalQuery:
    """Query boundary for tutor retrieval (lesson-scoped first)."""

    query_text: str
    lesson_id: str
    top_k: int = 4


@dataclass(frozen=True, slots=True)
class IndexingResult:
    lesson_id: str
    event: LessonIndexEvent
    status: str
    chunk_count: int = 0
    content_hash: str | None = None
    note: str | None = None


class EmbeddingProvider(ABC):
    """Boundary for future embedding integrations."""

    @abstractmethod
    async def embed(self, texts: list[str]) -> list[list[float]]:
        ...


class RetrievalArtifactRepository(ABC):
    """Storage boundary for derived retrieval artifacts."""

    @abstractmethod
    def get_lesson_content_hash(self, lesson_id: str) -> str | None:
        ...

    @abstractmethod
    def replace_lesson_artifacts(
        self,
        lesson_id: str,
        *,
        content_hash: str,
        artifacts: list[RetrievalArtifact],
    ) -> None:
        ...

    @abstractmethod
    def delete_lesson_artifacts(self, lesson_id: str) -> int:
        ...

    @abstractmethod
    def query_lesson(self, query: RetrievalQuery) -> list[RetrievalCandidate]:
        ...


class InMemoryRetrievalArtifactRepository(RetrievalArtifactRepository):
    """Simple in-memory scaffold for tests and local design validation."""

    def __init__(self) -> None:
        self._hash_by_lesson: dict[str, str] = {}
        self._artifacts_by_lesson: dict[str, list[RetrievalArtifact]] = {}

    def get_lesson_content_hash(self, lesson_id: str) -> str | None:
        return self._hash_by_lesson.get(lesson_id)

    def replace_lesson_artifacts(
        self,
        lesson_id: str,
        *,
        content_hash: str,
        artifacts: list[RetrievalArtifact],
    ) -> None:
        self._hash_by_lesson[lesson_id] = content_hash
        self._artifacts_by_lesson[lesson_id] = list(artifacts)

    def delete_lesson_artifacts(self, lesson_id: str) -> int:
        removed = len(self._artifacts_by_lesson.get(lesson_id, []))
        self._artifacts_by_lesson.pop(lesson_id, None)
        self._hash_by_lesson.pop(lesson_id, None)
        return removed

    def query_lesson(self, query: RetrievalQuery) -> list[RetrievalCandidate]:
        artifacts = self._artifacts_by_lesson.get(query.lesson_id, [])
        terms = _tokenize(query.query_text)

        ranked: list[RetrievalCandidate] = []
        for artifact in artifacts:
            score = _lexical_score(artifact.text, terms)
            ranked.append(
                RetrievalCandidate(
                    chunk_id=artifact.chunk_id,
                    lesson_id=artifact.lesson_id,
                    text=artifact.text,
                    metadata=artifact.metadata,
                    score=score,
                )
            )

        ranked.sort(key=lambda c: c.score, reverse=True)
        return ranked[: query.top_k]


@dataclass(slots=True)
class LessonRetrievalIndexer:
    """Orchestrates ingestion and indexing of lesson-derived retrieval artifacts."""

    repository: RetrievalArtifactRepository
    embedding_provider: EmbeddingProvider | None = None
    default_chunk_chars: int = 900

    async def handle_event(self, lesson: LessonIngestionInput) -> IndexingResult:
        if lesson.event in {LessonIndexEvent.UNPUBLISHED, LessonIndexEvent.DELETED}:
            removed = self.repository.delete_lesson_artifacts(lesson.lesson_id)
            return IndexingResult(
                lesson_id=lesson.lesson_id,
                event=lesson.event,
                status="deleted",
                chunk_count=removed,
                note="Derived retrieval artifacts removed for lesson.",
            )

        return await self._index_or_reindex(lesson)

    async def _index_or_reindex(self, lesson: LessonIngestionInput) -> IndexingResult:
        prepared_chunks = build_lesson_chunks(
            lesson_id=lesson.lesson_id,
            lesson_title=lesson.lesson_title,
            lesson_content=lesson.lesson_content,
            grade_name=lesson.grade_name,
            subject_name=lesson.subject_name,
            chapter_id=lesson.chapter_id,
            tenant_id=lesson.tenant_id,
            max_chars=self.default_chunk_chars,
        )

        if not prepared_chunks:
            removed = self.repository.delete_lesson_artifacts(lesson.lesson_id)
            return IndexingResult(
                lesson_id=lesson.lesson_id,
                event=lesson.event,
                status="deleted",
                chunk_count=removed,
                note="Lesson content empty after normalization; artifacts removed.",
            )

        content_hash = str(prepared_chunks[0].metadata["content_hash"])
        existing_hash = self.repository.get_lesson_content_hash(lesson.lesson_id)

        if existing_hash == content_hash:
            return IndexingResult(
                lesson_id=lesson.lesson_id,
                event=lesson.event,
                status="skipped",
                chunk_count=0,
                content_hash=content_hash,
                note="Content hash unchanged; re-index skipped.",
            )

        artifacts = await self._to_artifacts(prepared_chunks)
        self.repository.replace_lesson_artifacts(
            lesson.lesson_id,
            content_hash=content_hash,
            artifacts=artifacts,
        )

        return IndexingResult(
            lesson_id=lesson.lesson_id,
            event=lesson.event,
            status="indexed",
            chunk_count=len(artifacts),
            content_hash=content_hash,
            note="Derived artifacts replaced for lesson.",
        )

    async def _to_artifacts(self, chunks: list[LessonRetrievalChunk]) -> list[RetrievalArtifact]:
        embeddings: list[list[float]] | None = None
        if self.embedding_provider is not None:
            embeddings = await self.embedding_provider.embed([c.text for c in chunks])

        artifacts: list[RetrievalArtifact] = []
        for i, chunk in enumerate(chunks):
            embedding = embeddings[i] if embeddings is not None else None
            artifacts.append(
                RetrievalArtifact(
                    chunk_id=chunk.chunk_id,
                    lesson_id=chunk.lesson_id,
                    text=chunk.text,
                    metadata=chunk.metadata,
                    embedding=embedding,
                )
            )
        return artifacts


@dataclass(slots=True)
class LessonScopedRetriever:
    """Simple query boundary that enforces lesson-scoped retrieval."""

    repository: RetrievalArtifactRepository

    def retrieve(self, query: RetrievalQuery) -> list[RetrievalCandidate]:
        return self.repository.query_lesson(query)


@dataclass(slots=True)
class NoOpEmbeddingProvider(EmbeddingProvider):
    """Embedding boundary stub for local flow validation without external services."""

    dimensions: int = 0

    async def embed(self, texts: list[str]) -> list[list[float]]:
        if self.dimensions <= 0:
            return [[] for _ in texts]
        return [[0.0] * self.dimensions for _ in texts]


def _tokenize(text: str) -> set[str]:
    return {part for part in text.lower().split() if part}


def _lexical_score(text: str, terms: set[str]) -> float:
    if not terms:
        return 0.0
    words = _tokenize(text)
    if not words:
        return 0.0
    overlap = len(words.intersection(terms))
    return overlap / max(len(terms), 1)

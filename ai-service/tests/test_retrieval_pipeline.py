from __future__ import annotations

import pytest

from app.services.retrieval_pipeline import (
    InMemoryRetrievalArtifactRepository,
    LessonIndexEvent,
    LessonIngestionInput,
    LessonRetrievalIndexer,
    LessonScopedRetriever,
    RetrievalQuery,
)


def _lesson_input(content: str, event: LessonIndexEvent) -> LessonIngestionInput:
    return LessonIngestionInput(
        lesson_id="lesson-001",
        chapter_id="chapter-001",
        lesson_title="Introduction to Fractions",
        lesson_content=content,
        subject_name="Mathematics",
        grade_name="Grade 4",
        tenant_id="tenant-001",
        event=event,
    )


@pytest.mark.asyncio
async def test_created_event_indexes_chunks() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    indexer = LessonRetrievalIndexer(repository=repo)

    result = await indexer.handle_event(
        _lesson_input("## Concept\nFractions represent parts of a whole.", LessonIndexEvent.CREATED)
    )

    assert result.status == "indexed"
    assert result.chunk_count > 0
    assert repo.get_lesson_content_hash("lesson-001") is not None


@pytest.mark.asyncio
async def test_update_with_same_content_hash_skips_reindex() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    indexer = LessonRetrievalIndexer(repository=repo)
    content = "## Concept\nA numerator sits above a denominator."

    first = await indexer.handle_event(_lesson_input(content, LessonIndexEvent.CREATED))
    second = await indexer.handle_event(_lesson_input(content, LessonIndexEvent.UPDATED))

    assert first.status == "indexed"
    assert second.status == "skipped"
    assert second.content_hash == first.content_hash


@pytest.mark.asyncio
async def test_unpublish_or_delete_removes_artifacts() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    indexer = LessonRetrievalIndexer(repository=repo)

    await indexer.handle_event(
        _lesson_input("## Concept\nFractions can be equivalent.", LessonIndexEvent.CREATED)
    )
    removed = await indexer.handle_event(_lesson_input("", LessonIndexEvent.UNPUBLISHED))

    assert removed.status == "deleted"
    assert repo.get_lesson_content_hash("lesson-001") is None


@pytest.mark.asyncio
async def test_lesson_scoped_retrieval_returns_ranked_candidates() -> None:
    repo = InMemoryRetrievalArtifactRepository()
    indexer = LessonRetrievalIndexer(repository=repo)
    retriever = LessonScopedRetriever(repository=repo)

    await indexer.handle_event(
        _lesson_input(
            "## Numerator\nThe numerator is the top number.\n\n"
            "## Denominator\nThe denominator is the bottom number.",
            LessonIndexEvent.CREATED,
        )
    )

    results = retriever.retrieve(
        RetrievalQuery(query_text="top number numerator", lesson_id="lesson-001", top_k=2)
    )

    assert results
    assert len(results) <= 2
    assert all(r.lesson_id == "lesson-001" for r in results)
    assert results[0].score >= results[-1].score

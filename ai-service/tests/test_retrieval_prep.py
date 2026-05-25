from __future__ import annotations

from app.services.retrieval_prep import (
    build_lesson_chunks,
    normalize_lesson_markdown,
    split_into_section_blocks,
)


def test_normalize_lesson_markdown_normalizes_newlines_and_blanks() -> None:
    raw = "Line 1\r\n\r\n\r\nLine 2   \rLine 3\n\n\n"
    actual = normalize_lesson_markdown(raw)
    assert actual == "Line 1\n\nLine 2\nLine 3"


def test_split_into_section_blocks_builds_overview_and_headings() -> None:
    content = (
        "Intro text before headings.\n\n"
        "## Key Idea\n"
        "Fractions are parts of a whole.\n\n"
        "### Example\n"
        "1/2 means one out of two equal parts."
    )
    blocks = split_into_section_blocks(content)

    assert len(blocks) == 3
    assert blocks[0].title == "Overview"
    assert blocks[0].heading_level is None
    assert "Intro text" in blocks[0].text

    assert blocks[1].title == "Key Idea"
    assert blocks[1].heading_level == 2

    assert blocks[2].title == "Example"
    assert blocks[2].heading_level == 3


def test_build_lesson_chunks_creates_lesson_scoped_chunks_with_metadata() -> None:
    content = (
        "## Concept\n"
        "A fraction has a numerator and denominator.\n\n"
        "## Practice\n"
        "Look at 3/4 and identify numerator and denominator."
    )
    chunks = build_lesson_chunks(
        lesson_id="lesson-001",
        lesson_title="Introduction to Fractions",
        lesson_content=content,
        grade_name="Grade 4",
        subject_name="Mathematics",
        chapter_id="chapter-001",
        tenant_id="tenant-001",
        max_chars=300,
    )

    assert len(chunks) >= 2
    assert all(c.lesson_id == "lesson-001" for c in chunks)
    assert all(c.metadata["lesson_id"] == "lesson-001" for c in chunks)
    assert all(c.metadata["subject_name"] == "Mathematics" for c in chunks)
    assert all(c.metadata["content_format"] == "markdown_v1" for c in chunks)
    assert all(c.metadata["content_hash"] for c in chunks)


def test_build_lesson_chunks_respects_max_chars() -> None:
    content = "## Long\n" + ("Sentence. " * 200)
    chunks = build_lesson_chunks(
        lesson_id="lesson-xyz",
        lesson_title="Long Lesson",
        lesson_content=content,
        grade_name="Grade 5",
        subject_name="Science",
        chapter_id="chapter-xyz",
        tenant_id=None,
        max_chars=300,
    )

    assert chunks
    assert all(len(c.text) <= 300 for c in chunks)

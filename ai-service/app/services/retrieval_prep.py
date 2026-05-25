"""
Internal lesson retrieval preparation foundations.

This module intentionally does NOT do provider-specific embedding, vector DB
indexing, or runtime retrieval. It only prepares deterministic, lesson-scoped
chunk records from canonical lesson content.

Design goals for Phase 1 Section 12 Part 1:
  - Keep canonical content ownership in the backend lesson model.
  - Produce derived chunk artifacts that can be embedded/indexed later.
  - Use stable section boundaries (Markdown headings) for tutor-grounded chunks.
  - Attach metadata needed for lesson-scoped retrieval and safety filtering.
"""
from __future__ import annotations

from dataclasses import dataclass
from hashlib import sha256
import re


@dataclass(frozen=True, slots=True)
class LessonSectionBlock:
    """A deterministic section boundary extracted from lesson Markdown."""

    block_index: int
    title: str
    heading_level: int | None
    text: str


@dataclass(frozen=True, slots=True)
class LessonRetrievalChunk:
    """A derived retrieval artifact created from canonical lesson content."""

    chunk_id: str
    lesson_id: str
    text: str
    metadata: dict[str, str | int | None]


_HEADING_RE = re.compile(r"^(#{2,6})\s+(.*\S)\s*$")
_BLANKS_RE = re.compile(r"\n{3,}")


def normalize_lesson_markdown(content: str) -> str:
    """
    Normalize lesson Markdown into a stable text form for chunk derivation.

    Normalization keeps semantics while reducing chunk drift:
      - convert CRLF/CR to LF
      - strip trailing whitespace
      - collapse 3+ blank lines to 2
      - trim leading/trailing blank lines
    """
    content = content.replace("\r\n", "\n").replace("\r", "\n")
    lines = [line.rstrip() for line in content.split("\n")]
    normalized = "\n".join(lines)
    normalized = _BLANKS_RE.sub("\n\n", normalized)
    return normalized.strip()


def split_into_section_blocks(content: str) -> list[LessonSectionBlock]:
    """
    Split normalized Markdown into stable section blocks.

    Boundaries are based on H2+ headings. Text before first heading becomes an
    "Overview" block. Empty blocks are dropped.
    """
    normalized = normalize_lesson_markdown(content)
    if not normalized:
        return []

    blocks: list[LessonSectionBlock] = []
    current_title = "Overview"
    current_level: int | None = None
    current_lines: list[str] = []

    def flush() -> None:
        nonlocal current_lines
        text = "\n".join(current_lines).strip()
        if text:
            blocks.append(
                LessonSectionBlock(
                    block_index=len(blocks),
                    title=current_title,
                    heading_level=current_level,
                    text=text,
                )
            )
        current_lines = []

    for line in normalized.split("\n"):
        m = _HEADING_RE.match(line)
        if m:
            flush()
            current_level = len(m.group(1))
            current_title = m.group(2).strip()
            continue
        current_lines.append(line)

    flush()
    return blocks


def build_lesson_chunks(
    *,
    lesson_id: str,
    lesson_title: str,
    lesson_content: str,
    grade_name: str,
    subject_name: str,
    chapter_id: str,
    tenant_id: str | None,
    max_chars: int = 900,
) -> list[LessonRetrievalChunk]:
    """
    Build lesson-scoped chunk artifacts from canonical lesson Markdown.

    Returned chunks are deterministic and carry metadata required for future
    retrieval filters. Chunks are derived artifacts and should be treated as
    re-creatable from canonical lesson content.
    """
    if max_chars < 300:
        raise ValueError("max_chars must be >= 300.")

    normalized = normalize_lesson_markdown(lesson_content)
    if not normalized:
        return []

    lesson_hash = _sha(normalized)
    blocks = split_into_section_blocks(normalized)
    chunks: list[LessonRetrievalChunk] = []

    for block in blocks:
        for chunk_index, chunk_text in enumerate(_chunk_block_text(block.text, max_chars=max_chars)):
            chunk_id = f"{lesson_id}:b{block.block_index}:c{chunk_index}"
            chunks.append(
                LessonRetrievalChunk(
                    chunk_id=chunk_id,
                    lesson_id=lesson_id,
                    text=chunk_text,
                    metadata={
                        "tenant_id": tenant_id,
                        "chapter_id": chapter_id,
                        "lesson_id": lesson_id,
                        "lesson_title": lesson_title,
                        "subject_name": subject_name,
                        "grade_name": grade_name,
                        "block_index": block.block_index,
                        "block_title": block.title,
                        "block_heading_level": block.heading_level,
                        "content_format": "markdown_v1",
                        "content_hash": lesson_hash,
                    },
                )
            )

    return chunks


def _chunk_block_text(text: str, *, max_chars: int) -> list[str]:
    """Chunk one block by paragraph, then by sentence when needed."""
    paragraphs = [p.strip() for p in text.split("\n\n") if p.strip()]
    if not paragraphs:
        return []

    out: list[str] = []
    current = ""

    def flush() -> None:
        nonlocal current
        if current.strip():
            out.append(current.strip())
            current = ""

    for paragraph in paragraphs:
        if len(paragraph) > max_chars:
            flush()
            out.extend(_split_long_paragraph(paragraph, max_chars=max_chars))
            continue

        candidate = f"{current}\n\n{paragraph}".strip() if current else paragraph
        if len(candidate) <= max_chars:
            current = candidate
        else:
            flush()
            current = paragraph

    flush()
    return out


def _split_long_paragraph(text: str, *, max_chars: int) -> list[str]:
    """Best-effort split for long paragraphs using sentence punctuation."""
    sentences = re.split(r"(?<=[.!?])\s+", text)
    sentences = [s.strip() for s in sentences if s.strip()]
    if not sentences:
        return [text[:max_chars]]

    parts: list[str] = []
    current = ""
    for sentence in sentences:
        candidate = f"{current} {sentence}".strip() if current else sentence
        if len(candidate) <= max_chars:
            current = candidate
            continue

        if current:
            parts.append(current)

        if len(sentence) <= max_chars:
            current = sentence
        else:
            parts.extend(_hard_split(sentence, max_chars=max_chars))
            current = ""

    if current:
        parts.append(current)

    return parts


def _hard_split(text: str, *, max_chars: int) -> list[str]:
    return [text[i : i + max_chars].strip() for i in range(0, len(text), max_chars)]


def _sha(text: str) -> str:
    return sha256(text.encode("utf-8")).hexdigest()

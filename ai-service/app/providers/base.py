"""
Abstract LLM provider interface.

All provider implementations (Ollama, OpenAI, Anthropic, Google, Azure OpenAI)
must subclass BaseLLMProvider and implement every abstract method.

The application layer interacts only with this interface — never with concrete
provider classes directly. This makes it trivial to:
  - Swap providers at runtime
  - Mock providers in tests
  - Add new providers without touching service logic

Usage pattern (Phase 2+):
    from app.providers.registry import get_provider

    provider = get_provider()          # returns the configured default
    response = await provider.complete(prompt)
    embeddings = await provider.embed(["text one", "text two"])
"""
from __future__ import annotations

from abc import ABC, abstractmethod
from collections.abc import AsyncIterator


class BaseLLMProvider(ABC):
    """
    Contract that every LLM provider must fulfil.

    Three capabilities:
      1. complete  — single-turn text completion (tutoring responses, scoring rationale)
      2. stream    — streaming completion (real-time tutoring sessions, TTS pipeline)
      3. embed     — text embeddings (RAG document retrieval, semantic search)
    """

    # ── Completion ──────────────────────────────────────────────────────────

    @abstractmethod
    async def complete(
        self,
        prompt: str,
        *,
        system_prompt: str | None = None,
        model: str | None = None,
        max_tokens: int = 1024,
        temperature: float = 0.7,
    ) -> str:
        """
        Send a completion request and return the full response text.

        Args:
            prompt: The user/task prompt.
            system_prompt: Optional system/instruction context.
            model: Override the provider's default model.
            max_tokens: Maximum tokens to generate.
            temperature: Sampling temperature (0 = deterministic, 1 = creative).

        Returns:
            Generated text string.
        """
        ...

    @abstractmethod
    async def stream(
        self,
        prompt: str,
        *,
        system_prompt: str | None = None,
        model: str | None = None,
        max_tokens: int = 1024,
        temperature: float = 0.7,
    ) -> AsyncIterator[str]:
        """
        Stream a completion response chunk by chunk.
        Used for real-time tutoring sessions and future TTS pipelines.

        Yields:
            Text delta strings as they are generated.
        """
        ...

    # ── Embeddings ──────────────────────────────────────────────────────────

    @abstractmethod
    async def embed(
        self,
        texts: list[str],
        *,
        model: str | None = None,
    ) -> list[list[float]]:
        """
        Generate dense vector embeddings for a list of texts.
        Used for RAG document retrieval and semantic search (Phase 2+).

        Args:
            texts: List of strings to embed. Batch together when possible.
            model: Override the provider's default embedding model.

        Returns:
            List of float vectors, one per input text.
        """
        ...

    # ── Lifecycle ────────────────────────────────────────────────────────────

    async def health_check(self) -> bool:
        """
        Optional: verify the provider is reachable.
        Called during startup to log provider availability.
        Default implementation returns True (no-op).
        Override in concrete implementations to do a real ping.
        """
        return True

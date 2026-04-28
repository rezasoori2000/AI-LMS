"""
Provider registry — central factory for obtaining the active LLM provider.

Phase 2: Replace the stub below with real instantiation and optional pooling.

Usage:
    from app.providers.registry import get_provider

    provider = get_provider()
    text = await provider.complete("Explain photosynthesis to a 10-year-old.")
"""
from __future__ import annotations

from app.core.config import settings
from app.providers.base import BaseLLMProvider


def get_provider(name: str | None = None) -> BaseLLMProvider:
    """
    Return a provider instance by name, or the configured default.

    Args:
        name: Provider key ("ollama", "openai", "anthropic", "google", "azure_openai").
              If None, uses settings.ai_default_provider.

    Raises:
        NotImplementedError: Until concrete providers are implemented (Phase 2).
    """
    provider_name = name or settings.ai_default_provider

    # TODO (Phase 2): Instantiate real providers
    # match provider_name:
    #     case "ollama":
    #         from app.providers.ollama import OllamaProvider
    #         return OllamaProvider()
    #     case "openai":
    #         from app.providers.openai import OpenAIProvider
    #         return OpenAIProvider()
    #     case "anthropic":
    #         from app.providers.anthropic import AnthropicProvider
    #         return AnthropicProvider()
    #     case "google":
    #         from app.providers.google import GoogleProvider
    #         return GoogleProvider()
    #     case _:
    #         raise ValueError(f"Unknown provider: {provider_name!r}")

    raise NotImplementedError(
        f"Provider '{provider_name}' is not yet implemented. "
        "Providers are added in Phase 2."
    )

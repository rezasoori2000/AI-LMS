from __future__ import annotations

from pydantic import field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """
    Application settings loaded from environment variables.
    Precedence: env vars > .env file > defaults.
    All secrets (API keys, DB passwords) must come from env vars, never hardcoded.
    """

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",  # Silently ignore unknown env vars instead of crashing
    )

    # ── App ─────────────────────────────────────────────────────────────────
    app_name: str = "LMS AI Service"
    app_version: str = "0.1.0"
    debug: bool = False

    # ── Server ───────────────────────────────────────────────────────────────
    host: str = "0.0.0.0"
    port: int = 8000

    # ── CORS ────────────────────────────────────────────────────────────────
    # Explicit list of allowed origins — never use ["*"] in production
    cors_allowed_origins: list[str] = ["http://localhost:5173"]

    # ── Default LLM provider ────────────────────────────────────────────────
    # Options: "ollama" | "openai" | "azure_openai" | "anthropic" | "google"
    ai_default_provider: str = "ollama"

    # ── Ollama (local, no API key required) ─────────────────────────────────
    ollama_base_url: str = "http://localhost:11434"
    ollama_default_model: str = "llama3"
    ollama_embed_model: str = "nomic-embed-text"

    # ── OpenAI ──────────────────────────────────────────────────────────────
    openai_api_key: str | None = None
    openai_default_model: str = "gpt-4o-mini"
    openai_embed_model: str = "text-embedding-3-small"

    # ── Azure OpenAI ────────────────────────────────────────────────────────
    azure_openai_api_key: str | None = None
    azure_openai_endpoint: str | None = None
    azure_openai_api_version: str = "2024-02-01"

    # ── Anthropic ──────────────────────────────────────────────────────────
    anthropic_api_key: str | None = None
    anthropic_default_model: str = "claude-3-haiku-20240307"

    # ── Google Gemini ───────────────────────────────────────────────────────
    google_api_key: str | None = None
    google_default_model: str = "gemini-1.5-flash"

    # ── RAG / Vector store (Phase 2+) ──────────────────────────────────────
    # vector_store_url: str | None = None
    # vector_store_collection: str = "lms_documents"

    @field_validator("cors_allowed_origins", mode="before")
    @classmethod
    def parse_cors_origins(cls, v: object) -> list[str]:
        """Accept both a JSON list string and a comma-separated string from env vars."""
        if isinstance(v, str):
            # e.g. CORS_ALLOWED_ORIGINS="http://a.com,http://b.com"
            return [o.strip() for o in v.split(",") if o.strip()]
        return v  # type: ignore[return-value]


# Module-level singleton — import this instead of constructing Settings() directly
settings = Settings()

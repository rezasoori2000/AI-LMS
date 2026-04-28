"""
Logging configuration for the AI service.
Uses Python's standard library — no extra dependencies.
In development: human-readable level + message.
In production: structured format ready for log aggregators (Datadog, Loki, etc.).
"""
from __future__ import annotations

import logging
import sys


def configure_logging(debug: bool = False) -> None:
    """
    Configure the root logger. Call once at application startup (lifespan).

    Args:
        debug: When True, set level to DEBUG and use verbose format.
    """
    level = logging.DEBUG if debug else logging.INFO

    handler = logging.StreamHandler(sys.stdout)
    handler.setLevel(level)

    fmt = (
        "%(asctime)s %(levelname)-8s %(name)s  %(message)s"
        if debug
        else "%(asctime)s %(levelname)s %(name)s %(message)s"
    )
    handler.setFormatter(logging.Formatter(fmt=fmt, datefmt="%Y-%m-%dT%H:%M:%SZ"))

    root = logging.getLogger()
    root.setLevel(level)
    # Avoid duplicate handlers if called more than once (e.g. during testing)
    root.handlers.clear()
    root.addHandler(handler)

    # Reduce noise from third-party libraries in production
    if not debug:
        logging.getLogger("uvicorn.access").setLevel(logging.WARNING)
        logging.getLogger("httpx").setLevel(logging.WARNING)

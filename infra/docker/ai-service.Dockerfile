# =============================================================
# AI-LMS AI Service — Multi-stage Dockerfile
# infra/docker/ai-service.Dockerfile
#
# Stage 1 (deps)    : install Python dependencies in a venv
# Stage 2 (runtime) : lean image — copy only the venv + app source
#
# Build context: ./ai-service
# =============================================================

# ── Stage 1: Build dependencies ──────────────────────────────────────────
FROM python:3.12-slim AS deps

WORKDIR /app

# Install system packages required to compile some Python wheels.
# --no-install-recommends keeps the image lean.
RUN apt-get update \
  && apt-get install -y --no-install-recommends \
       gcc \
       libffi-dev \
  && rm -rf /var/lib/apt/lists/*

# Create a virtual environment inside the image so we can copy
# it cleanly into the runtime stage.
RUN python -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# Install dependencies — pinned in requirements.txt for reproducibility.
# Upgrade pip first to avoid hash-verification warnings.
COPY requirements.txt .
RUN pip install --upgrade pip --quiet \
  && pip install --no-cache-dir -r requirements.txt


# ── Stage 2: Runtime ──────────────────────────────────────────────────────
FROM python:3.12-slim AS runtime

WORKDIR /app

# Copy the pre-built virtual environment from the deps stage
COPY --from=deps /opt/venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# Run as non-root user
RUN groupadd -g 1001 appgroup \
  && useradd -u 1001 -g appgroup -s /bin/false appuser

# Copy application source
COPY --chown=appuser:appgroup . .

USER appuser

EXPOSE 8000

# Never run with --reload in production; workers count tuned externally
ENV PYTHONUNBUFFERED=1
ENV PYTHONDONTWRITEBYTECODE=1

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health')" || exit 1

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]

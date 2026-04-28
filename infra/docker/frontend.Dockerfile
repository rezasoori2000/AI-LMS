# =============================================================
# AI-LMS Frontend — Multi-stage Dockerfile
# infra/docker/frontend.Dockerfile
#
# Build context: repo root (.)
# This allows access to both frontend/ source and infra/docker/nginx.conf.
#
# Stage 1 (deps)  : install node_modules (cache layer)
# Stage 2 (build) : run tsc + vite build
# Stage 3 (serve) : serve static output with nginx (production)
#
# For local dev, docker-compose.override.yml mounts source and
# runs `vite dev` instead — the nginx serve stage is production only.
# =============================================================

# ── Stage 1: Install dependencies ─────────────────────────────────────────
FROM node:22-alpine AS deps

WORKDIR /app

# Copy only lock files first to maximise layer cache reuse.
# node_modules are rebuilt only when package.json or lock file changes.
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci --omit=dev=false


# ── Stage 2: Build ────────────────────────────────────────────────────────
FROM node:22-alpine AS builder

WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY frontend/ .

# Build arg injected at build time from docker-compose or CI.
# Becomes an embedded VITE_ variable in the static bundle.
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build


# ── Stage 3: Serve (production) ───────────────────────────────────────────
FROM nginx:1.27-alpine AS serve

# Remove default nginx static content
RUN rm -rf /usr/share/nginx/html/*

# Copy built assets from builder stage
COPY --from=builder /app/dist /usr/share/nginx/html

# nginx SPA config (served from repo root context)
COPY infra/docker/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

# Run as non-root
RUN addgroup -g 1001 -S appgroup && adduser -u 1001 -S appuser -G appgroup \
  && chown -R appuser:appgroup /usr/share/nginx/html \
  && chown -R appuser:appgroup /var/cache/nginx \
  && chown -R appuser:appgroup /var/log/nginx \
  && touch /var/run/nginx.pid \
  && chown appuser:appgroup /var/run/nginx.pid

USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:80/ || exit 1

CMD ["nginx", "-g", "daemon off;"]

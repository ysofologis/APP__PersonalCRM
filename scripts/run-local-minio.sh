#!/usr/bin/env bash
# scripts/run-local-minio.sh — run Personal CRM from the host, pointed at a
# MinIO sidecar. Use this when you want S3-mode attachments without bringing
# up the full docker-compose stack.
#
# Usage:
#   ./scripts/run-local-minio.sh                    # default: minio:9000
#   MINIO_HOST=127.0.0.1 MINIO_PORT=9000 ./scripts/run-local-minio.sh
#
# Env vars (all optional except MINIO_ROOT_USER/MINIO_ROOT_PASSWORD):
#   MINIO_HOST                defaults to "minio"
#   MINIO_PORT                defaults to 9000
#   MINIO_ROOT_USER           defaults to "crm"
#   MINIO_ROOT_PASSWORD       defaults to "S@gapo123"
#   MINIO_BUCKET              defaults to "personal-crm"
#   MINIO_REGION              defaults to "us-east-1"
#   PORT                      port to bind the CRM on (default 8080)
#
# Prerequisite: a reachable MinIO at MINIO_HOST:MINIO_PORT with the bucket
# `MINIO_BUCKET` already created. If `minio` doesn't resolve on the host,
# either run MinIO in Docker and add `127.0.0.1 minio` to /etc/hosts after
# `docker compose up`, or set MINIO_HOST=127.0.0.1 and use port forwarding.

set -euo pipefail

cd "$(dirname "$0")/.."

PORT="${PORT:-8080}"
MINIO_HOST="${MINIO_HOST:-minio}"
MINIO_PORT="${MINIO_PORT:-9000}"
MINIO_ROOT_USER="${MINIO_ROOT_USER:-crm}"
MINIO_ROOT_PASSWORD="${MINIO_ROOT_PASSWORD:-S@gapo123}"
MINIO_BUCKET="${MINIO_BUCKET:-personal-crm}"
MINIO_REGION="${MINIO_REGION:-us-east-1}"

# Generate signing key if missing.
if [[ -z "${Auth__JwtSigningKey:-}" ]]; then
    export Auth__JwtSigningKey="$(openssl rand -hex 32)"
    echo "→ Generated Auth__JwtSigningKey (32 random bytes)"
fi

export Auth__JwtIssuer="${Auth__JwtIssuer:-http://localhost:${PORT}}"
export DOTNET_ENVIRONMENT="${DOTNET_ENVIRONMENT:-Development}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://+:${PORT}}"

# S3-compatible storage pointing at the MinIO sidecar.
export AttachmentStore__Provider=s3
export AttachmentStore__S3__Endpoint="http://${MINIO_HOST}:${MINIO_PORT}"
export AttachmentStore__S3__Bucket="${MINIO_BUCKET}"
export AttachmentStore__S3__Region="${MINIO_REGION}"
export AttachmentStore__S3__AccessKey="${MINIO_ROOT_USER}"
export AttachmentStore__S3__SecretKey="${MINIO_ROOT_PASSWORD}"
export AttachmentStore__S3__ForcePathStyle="true"

echo
echo "  ┌──────────────────────────────────────────────────┐"
echo "  │  Personal CRM — local dev runner (MinIO mode)    │"
echo "  │  URL      : ${ASPNETCORE_URLS}                  "
echo "  │  Issuer   : ${Auth__JwtIssuer}                   "
echo "  │  Storage  : S3 → ${AttachmentStore__S3__Endpoint}"
echo "  │  Bucket   : ${MINIO_BUCKET}                       "
echo "  └──────────────────────────────────────────────────┘"
echo

# Verify MinIO is reachable before starting the app — fail fast.
echo "→ Checking MinIO at ${AttachmentStore__S3__Endpoint}…"
if command -v curl >/dev/null 2>&1; then
    if ! curl -sf -m 3 "${AttachmentStore__S3__Endpoint}/minio/health/live" >/dev/null; then
        echo "  ! MinIO health check failed." >&2
        echo "  ! Make sure MinIO is running and the bucket exists." >&2
        echo "  ! From a MinIO host, run:" >&2
        echo "      mc alias set local ${AttachmentStore__S3__Endpoint} ${MINIO_ROOT_USER} ${MINIO_ROOT_PASSWORD}" >&2
        echo "      mc mb -p local/${MINIO_BUCKET}" >&2
        exit 1
    fi
    echo "  ✓ MinIO reachable"
else
    echo "  (curl not installed; skipping MinIO health check)"
fi
echo

mkdir -p ./data

echo "First visit: complete the setup wizard at /setup."
echo "Stop with: Ctrl+C"
echo

dotnet run --project src/PersonalCrm.App --launch-profile http

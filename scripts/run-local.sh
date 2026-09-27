#!/usr/bin/env bash
# scripts/run-local.sh — run Personal CRM from the host with `dotnet run`.
#
# Usage:
#   ./scripts/run-local.sh                    # defaults: HTTP, port 8080
#   ./scripts/run-local.sh --https           # use the "https" launch profile
#   PORT=9000 ./scripts/run-local.sh          # bind to a different port
#   RESET=1   ./scripts/run-local.sh          # wipe ./data/ before booting
#   SKIP_BUILD=1 ./scripts/run-local.sh       # skip the `dotnet build` check
#
# Env vars:
#   Auth__JwtSigningKey   auto-generated if unset (recommended)
#   Auth__JwtIssuer       defaults to http://localhost:${PORT:-8080}
#   SMTP__*               optional; leave unset to disable email

set -euo pipefail

cd "$(dirname "$0")/.."

PORT="${PORT:-8080}"
ISSUER="${Auth__JwtIssuer:-http://localhost:${PORT}}"
PROFILE="http"
RESET="${RESET:-0}"
SKIP_BUILD="${SKIP_BUILD:-0}"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --https) PROFILE="https"; shift ;;
        --http)  PROFILE="http";  shift ;;
        --reset) RESET=1; shift ;;
        --no-build) SKIP_BUILD=1; shift ;;
        -h|--help)
            sed -n '2,15p' "$0"
            exit 0
            ;;
        *) echo "Unknown arg: $1" >&2; exit 2 ;;
    esac
done

# Generate a strong signing key if one isn't already in the environment.
if [[ -z "${Auth__JwtSigningKey:-}" ]]; then
    export Auth__JwtSigningKey="$(openssl rand -hex 32)"
    echo "→ Generated Auth__JwtSigningKey (32 random bytes)"
fi

export Auth__JwtIssuer="${ISSUER}"
export DOTNET_ENVIRONMENT="${DOTNET_ENVIRONMENT:-Development}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://+:${PORT}}"

# Pin the SQLite file to the repo's ./data/ directory so it doesn't get
# scattered by ContentRootPath resolution (which for `dotnet run --project`
# would otherwise point at src/PersonalCrm.App/).
export ConnectionStrings__Default="Data Source=$(pwd)/data/personal-crm.db"

if [[ "$RESET" == "1" ]]; then
    echo "→ RESET=1: removing ./data/"
    rm -rf ./data
fi

if [[ "$SKIP_BUILD" != "1" ]]; then
    echo "→ Verifying build..."
    dotnet build -nologo --no-restore -clp:ErrorsOnly
fi

mkdir -p ./data

echo
echo "  ┌──────────────────────────────────────────────────┐"
echo "  │  Personal CRM — local dev runner                 │"
echo "  │  URL    : ${ASPNETCORE_URLS}                    "
echo "  │  Issuer : ${Auth__JwtIssuer}                     "
echo "  │  Profile: ${PROFILE}                              "
echo "  │  Storage: local filesystem (./data/attachments)  │"
echo "  └──────────────────────────────────────────────────┘"
echo
echo "First visit: complete the setup wizard at /setup."
echo "Stop with: Ctrl+C"
echo

dotnet run --project src/PersonalCrm.App --launch-profile "${PROFILE}"

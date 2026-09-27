# scripts/

Small operational scripts for running Personal CRM from the host. All scripts
auto-`cd` to the repo root and use `dotnet run` against `src/PersonalCrm.App`.

## `run-local.sh`

Run the CRM with **local filesystem attachments** (no external services needed).
Best for first-run and offline development.

```bash
./scripts/run-local.sh
```

| Env var | Default | Notes |
|---|---|---|
| `PORT` | `8080` | Binds Kestrel to `http://+:${PORT}` |
| `Auth__JwtSigningKey` | auto-generated | 32 random bytes via `openssl rand -hex 32` |
| `Auth__JwtIssuer` | `http://localhost:${PORT}` | Drives the refresh cookie's `Secure` flag |
| `RESET` | `0` | Set to `1` to wipe `./data/` before booting |
| `SKIP_BUILD` | `0` | Set to `1` to skip the `dotnet build` sanity check |

Flags:

| Flag | Effect |
|---|---|
| `--https` | Use the `https` launch profile (self-signed dev cert) |
| `--reset` | Same as `RESET=1` |
| `--no-build` | Same as `SKIP_BUILD=1` |
| `-h`, `--help` | Print the script's header comment |

## `run-local-minio.sh`

Run the CRM with **S3-compatible attachments against a MinIO sidecar** (no
Docker Compose required). Best for testing the S3 storage path on a
non-composed host.

```bash
./scripts/run-local-minio.sh
```

| Env var | Default | Notes |
|---|---|---|
| `MINIO_HOST` | `minio` | DNS name of the MinIO host |
| `MINIO_PORT` | `9000` | |
| `MINIO_ROOT_USER` | `crm` | |
| `MINIO_ROOT_PASSWORD` | `S@gapo123` | |
| `MINIO_BUCKET` | `personal-crm` | Bucket **must exist** before first boot |
| `MINIO_REGION` | `us-east-1` | |
| `PORT` | `8080` | Binds the CRM |

The script does a `curl` health check against
`http://${MINIO_HOST}:${MINIO_PORT}/minio/health/live` and refuses to start
the app if MinIO isn't reachable — fail fast instead of letting the
attachment store blow up on first upload.

## Typical first-time run

```bash
# Local mode — zero external deps
./scripts/run-local.sh

# Open http://localhost:8080 → /setup wizard
```

## Run against a sidecar MinIO

```bash
# Start MinIO (any way; one option: docker run)
docker run -d --name minio \
    -p 9000:9000 -p 9001:9001 \
    -e MINIO_ROOT_USER=crm \
    -e MINIO_ROOT_PASSWORD='S@gapo123' \
    minio/minio server /data --console-address ":9001"

# Create the bucket
docker exec minio mc alias set local http://localhost:9000 crm 'S@gapo123'
docker exec minio mc mb -p local/personal-crm
docker exec minio mc anonymous set none local/personal-crm

# Run the CRM pointed at it
./scripts/run-local-minio.sh
```

If MinIO is on a Docker network but not resolvable as `minio` on the host:

```bash
MINIO_HOST=127.0.0.1 ./scripts/run-local-minio.sh
```

## Reset everything

```bash
RESET=1 ./scripts/run-local.sh        # wipes ./data/, re-runs setup wizard
```

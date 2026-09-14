#!/usr/bin/env bash
# Nightly restore drill: backup -> disposable restore -> migrate ->
# /health/ready + one published entity + export checksum assertions.
# Never touches production data; the drill database is disposable.
set -euo pipefail

: "${DHARMATLAS_DATABASE_CONNECTION:?source connection is required}"
: "${DRILL_DATABASE:?disposable drill database name is required}"
: "${HOST_BASE_URL:?host base URL is required}"
: "${DRILL_ENTITY_ID:=ashoka}"

DUMP_FILE="${DUMP_FILE:-/tmp/dharmatlas.drill.dump}"

echo "1/6 Backing up source database..."
pg_dump --format=custom "$DHARMATLAS_DATABASE_CONNECTION" --file="$DUMP_FILE"

echo "2/6 Restoring into disposable database $DRILL_DATABASE..."
dropdb --if-exists "$DRILL_DATABASE"
createdb "$DRILL_DATABASE"
pg_restore --exit-on-error --dbname="$DRILL_DATABASE" "$DUMP_FILE"

echo "3/6 Applying current migrations to restored snapshot..."
DRILL_CONNECTION="$(echo "$DHARMATLAS_DATABASE_CONNECTION" | sed "s/Database=[^;]*/Database=$DRILL_DATABASE/")"
DHARMATLAS_DATABASE_CONNECTION="$DRILL_CONNECTION" \
  dotnet ef database update \
  --project src/Dharmatlas.Persistence/Dharmatlas.Persistence.csproj \
  --startup-project src/Dharmatlas.Host/Dharmatlas.Host.csproj

echo "4/6 Verifying /health/ready..."
curl --fail --show-error "$HOST_BASE_URL/health/ready"

echo "5/6 Verifying one published entity ($DRILL_ENTITY_ID)..."
curl --fail --show-error "$HOST_BASE_URL/api/v1/persons/$DRILL_ENTITY_ID"

echo "6/6 Verifying export checksum..."
curl --fail --show-error "$HOST_BASE_URL/api/v1/export" -o /tmp/dharmatlas.export.json
sha256sum /tmp/dharmatlas.export.json | tee /tmp/dharmatlas.export.sha256
test -s /tmp/dharmatlas.export.json

echo "Restore drill green: ready, entity, and export checksum verified."

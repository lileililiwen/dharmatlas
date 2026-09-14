#!/usr/bin/env bash
# Migration pre-check: verify the EF model has no pending changes and that
# migrations apply cleanly to a disposable database before staging promotion.
set -euo pipefail

echo "Checking for pending model changes..."
dotnet ef migrations has-pending-model-changes \
  --project src/Dharmatlas.Persistence/Dharmatlas.Persistence.csproj \
  --startup-project src/Dharmatlas.Host/Dharmatlas.Host.csproj \
  && echo "Model check passed."

if [ -n "${MIGRATION_CHECK_CONNECTION:-}" ]; then
  echo "Applying migrations to disposable check database..."
  DHARMATLAS_DATABASE_CONNECTION="$MIGRATION_CHECK_CONNECTION" \
    dotnet ef database update \
    --project src/Dharmatlas.Persistence/Dharmatlas.Persistence.csproj \
    --startup-project src/Dharmatlas.Host/Dharmatlas.Host.csproj
  echo "Disposable migration check passed."
else
  echo "MIGRATION_CHECK_CONNECTION unset; model check only."
fi

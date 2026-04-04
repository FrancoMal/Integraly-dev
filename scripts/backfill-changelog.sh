#!/bin/bash
# Genera changelogs para los últimos N días (default: 30)
# Uso: ./scripts/backfill-changelog.sh [dias]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DAYS="${1:-30}"

echo "=== Backfill de changelog: ultimos $DAYS dias ==="
echo ""

PROCESSED=0
SKIPPED=0
ERRORS=0

for i in $(seq 0 "$DAYS"); do
    DATE=$(date -d "$i days ago" +%Y-%m-%d 2>/dev/null || date -v-"${i}"d +%Y-%m-%d)

    echo "--- Procesando $DATE ---"

    if "$SCRIPT_DIR/generate-changelog.sh" "$DATE"; then
        if grep -q "No hay commits" /dev/stdin 2>/dev/null; then
            SKIPPED=$((SKIPPED + 1))
        else
            PROCESSED=$((PROCESSED + 1))
        fi
    else
        ERRORS=$((ERRORS + 1))
        echo "Error procesando $DATE, continuando..."
    fi

    # Esperar entre llamadas para no saturar la API de Claude
    if [ "$i" -lt "$DAYS" ]; then
        echo "Esperando 3 segundos..."
        sleep 3
    fi
    echo ""
done

echo "=== Backfill completado ==="
echo "Procesados: $PROCESSED"
echo "Errores: $ERRORS"

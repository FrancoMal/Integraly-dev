#!/bin/bash
# Genera un resumen de changelog para una fecha específica usando Claude Code
# Uso: ./scripts/generate-changelog.sh [YYYY-MM-DD]
# Requisito: tener claude autenticado (claude code ya logueado)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
DATE="${1:-$(date +%Y-%m-%d)}"

# Conexion a SQL Server (container de desarrollo)
SQLCMD="docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -d AIcoding -C"

cd "$PROJECT_DIR"

echo "[$DATE] Generando changelog..."

# Obtener commits del día en develop
NEXT_DATE=$(date -d "$DATE + 1 day" +%Y-%m-%d 2>/dev/null || date -v+1d -j -f "%Y-%m-%d" "$DATE" +%Y-%m-%d)

RAW_LOG=$(git log develop --after="$DATE 00:00:00" --before="$NEXT_DATE 00:00:00" \
    --pretty=format:'COMMIT_START%n{"hash":"%H","message":"%s","timestamp":"%ai"}%nCOMMIT_FILES' \
    --name-only 2>/dev/null || true)

if [ -z "$RAW_LOG" ]; then
    echo "[$DATE] No hay commits para esta fecha"
    exit 0
fi

# Parsear el log para construir texto estructurado con archivos por commit
FORMATTED_COMMITS=""
CURRENT_COMMIT=""
IN_FILES=false
COMMIT_COUNT=0

while IFS= read -r line; do
    if [[ "$line" == "COMMIT_START" ]]; then
        if [ -n "$CURRENT_COMMIT" ]; then
            FORMATTED_COMMITS="$FORMATTED_COMMITS$CURRENT_COMMIT\n"
        fi
        CURRENT_COMMIT=""
        IN_FILES=false
        COMMIT_COUNT=$((COMMIT_COUNT + 1))
    elif [[ "$line" == "COMMIT_FILES" ]]; then
        IN_FILES=true
    elif [ "$IN_FILES" = true ] && [ -n "$line" ]; then
        if [[ "$CURRENT_COMMIT" == *'"filesChanged":['* ]]; then
            CURRENT_COMMIT="${CURRENT_COMMIT},\"$line\""
        else
            CURRENT_COMMIT="${CURRENT_COMMIT} filesChanged:[\"$line\""
        fi
    elif [[ "$line" == '{"hash":'* ]]; then
        CURRENT_COMMIT="$line"
    fi
done <<< "$RAW_LOG"

if [ -n "$CURRENT_COMMIT" ]; then
    FORMATTED_COMMITS="$FORMATTED_COMMITS$CURRENT_COMMIT\n"
fi

if [ "$COMMIT_COUNT" -eq 0 ]; then
    echo "[$DATE] No hay commits para esta fecha"
    exit 0
fi

echo "[$DATE] Encontrados $COMMIT_COUNT commits, enviando a Claude..."

# Crear el prompt para Claude
PROMPT="Analiza estos commits de git del dia $DATE y genera un JSON con este formato exacto. RESPONDE SOLAMENTE EL JSON, sin explicaciones ni markdown.

Formato esperado:
{
  \"date\": \"$DATE\",
  \"generalSummary\": \"Resumen general del dia en espanol simple, entendible para alguien NO tecnico\",
  \"totalCommits\": $COMMIT_COUNT,
  \"groups\": [
    {
      \"groupTitle\": \"Titulo corto del grupo\",
      \"groupSummary\": \"Descripcion en espanol simple de que se hizo\",
      \"tags\": [\"backend\"],
      \"commitsJson\": \"[{\\\"hash\\\":\\\"abc123\\\",\\\"message\\\":\\\"mensaje\\\",\\\"timestamp\\\":\\\"2024-01-01\\\",\\\"filesChanged\\\":[\\\"archivo.cs\\\"]}]\",
      \"displayOrder\": 1
    }
  ]
}

Reglas IMPORTANTES:
- Agrupa commits relacionados (ej: 5 commits arreglando lo mismo = 1 grupo)
- Tags posibles: backend, frontend, infra, database, fix, feature, refactor
- El resumen general y de cada grupo debe ser entendible para alguien NO tecnico
- El campo commitsJson debe ser un STRING con JSON escapado adentro (no un objeto JSON directo)
- Cada commit en commitsJson debe tener: hash, message, timestamp, filesChanged (array de strings)
- RESPONDE SOLO EL JSON, sin backticks, sin explicaciones

Commits del dia:
$(echo -e "$FORMATTED_COMMITS")"

# Llamar a Claude Code (usa tu autenticacion existente)
RESULT=$(echo "$PROMPT" | claude -p 2>/dev/null)

if [ -z "$RESULT" ]; then
    echo "[$DATE] Error: Claude no devolvio resultado"
    exit 1
fi

# Limpiar posible markdown del resultado
CLEAN_RESULT=$(echo "$RESULT" | sed '/^```/d' | sed 's/^```json//' | sed 's/^```//')

# Verificar que es JSON válido
if ! echo "$CLEAN_RESULT" | python3 -c "import sys,json; json.load(sys.stdin)" 2>/dev/null; then
    echo "[$DATE] Error: El resultado no es JSON valido"
    echo "$CLEAN_RESULT" | head -5
    exit 1
fi

echo "[$DATE] Guardando en la base de datos..."

# Guardar JSON en archivo temporal
TMPFILE=$(mktemp /tmp/changelog-XXXXXX.json)
echo "$CLEAN_RESULT" > "$TMPFILE"

# Usar script Python para insertar en la DB
python3 "$SCRIPT_DIR/save-changelog-to-db.py" "$TMPFILE" "$PROJECT_DIR"
SAVE_RESULT=$?

rm -f "$TMPFILE"

if [ "$SAVE_RESULT" -eq 0 ]; then
    echo "[$DATE] Changelog generado exitosamente"
else
    echo "[$DATE] Error al guardar en la base de datos"
    exit 1
fi

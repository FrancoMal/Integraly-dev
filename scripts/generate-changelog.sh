#!/bin/bash
# Genera un resumen de changelog para una fecha específica usando Claude
# Uso: ./scripts/generate-changelog.sh [YYYY-MM-DD]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# Cargar variables de entorno
if [ -f "$PROJECT_DIR/.env" ]; then
    set -a
    source "$PROJECT_DIR/.env"
    set +a
fi

DATE="${1:-$(date +%Y-%m-%d)}"
API_URL="${CHANGELOG_API_URL:-http://localhost:3000/api/changelog}"
API_KEY="${CHANGELOG_API_KEY:-changelog-secret-key-2024}"
AUTH_USER="${CHANGELOG_USER:-admin}"
AUTH_PASS="${CHANGELOG_PASS:-admin123}"
AUTH_LOGIN_URL="${CHANGELOG_LOGIN_URL:-http://localhost:3000/api/auth/login}"

cd "$PROJECT_DIR"

echo "[$DATE] Generando changelog..."

# Obtener JWT token via login
echo "[$DATE] Autenticando..."
LOGIN_RESPONSE=$(curl -s -X POST "$AUTH_LOGIN_URL" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$AUTH_USER\",\"password\":\"$AUTH_PASS\"}" 2>/dev/null || true)

JWT_TOKEN=$(echo "$LOGIN_RESPONSE" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null || true)

if [ -n "$JWT_TOKEN" ]; then
    AUTH_HEADER="Authorization: Bearer $JWT_TOKEN"
    echo "[$DATE] Autenticado con JWT"
else
    AUTH_HEADER="X-Changelog-Key: $API_KEY"
    echo "[$DATE] JWT no disponible, usando API key"
fi

# Obtener commits del día en develop
NEXT_DATE=$(date -d "$DATE + 1 day" +%Y-%m-%d 2>/dev/null || date -v+1d -j -f "%Y-%m-%d" "$DATE" +%Y-%m-%d)

# Git log con formato estructurado: hash, mensaje, timestamp, y archivos
RAW_LOG=$(git log develop --after="$DATE 00:00:00" --before="$NEXT_DATE 00:00:00" \
    --pretty=format:'COMMIT_START%n{"hash":"%H","message":"%s","timestamp":"%ai"}%nCOMMIT_FILES' \
    --name-only 2>/dev/null || true)

if [ -z "$RAW_LOG" ]; then
    echo "[$DATE] No hay commits para esta fecha"
    exit 0
fi

# Parsear el log para construir un texto estructurado con archivos por commit
FORMATTED_COMMITS=""
CURRENT_COMMIT=""
IN_FILES=false
COMMIT_COUNT=0

while IFS= read -r line; do
    if [[ "$line" == "COMMIT_START" ]]; then
        # Guardar el commit anterior si existe
        if [ -n "$CURRENT_COMMIT" ]; then
            FORMATTED_COMMITS="$FORMATTED_COMMITS$CURRENT_COMMIT\n"
        fi
        CURRENT_COMMIT=""
        IN_FILES=false
        COMMIT_COUNT=$((COMMIT_COUNT + 1))
    elif [[ "$line" == "COMMIT_FILES" ]]; then
        IN_FILES=true
    elif [ "$IN_FILES" = true ] && [ -n "$line" ]; then
        # Es un nombre de archivo
        if [[ "$CURRENT_COMMIT" == *'"filesChanged":['* ]]; then
            CURRENT_COMMIT="${CURRENT_COMMIT},\"$line\""
        else
            CURRENT_COMMIT="${CURRENT_COMMIT} filesChanged:[\"$line\""
        fi
    elif [[ "$line" == '{"hash":'* ]]; then
        CURRENT_COMMIT="$line"
    fi
done <<< "$RAW_LOG"

# Guardar el último commit
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

# Llamar a Claude CLI
RESULT=$(echo "$PROMPT" | claude -p 2>/dev/null)

if [ -z "$RESULT" ]; then
    echo "[$DATE] Error: Claude no devolvio resultado"
    exit 1
fi

# Limpiar posible markdown del resultado (```json ... ```)
CLEAN_RESULT=$(echo "$RESULT" | sed '/^```/d' | sed 's/^```json//' | sed 's/^```//')

# Verificar que es JSON válido
if ! echo "$CLEAN_RESULT" | python3 -c "import sys,json; json.load(sys.stdin)" 2>/dev/null; then
    echo "[$DATE] Error: El resultado no es JSON valido"
    echo "$CLEAN_RESULT" | head -5
    exit 1
fi

echo "[$DATE] Enviando a la API..."

# POST a la API
HTTP_CODE=$(curl -s -o /tmp/changelog-response.txt -w "%{http_code}" \
    -X POST "$API_URL" \
    -H "Content-Type: application/json" \
    -H "$AUTH_HEADER" \
    -d "$CLEAN_RESULT")

if [ "$HTTP_CODE" -eq 200 ]; then
    echo "[$DATE] Changelog generado exitosamente"
else
    echo "[$DATE] Error HTTP $HTTP_CODE al guardar en la API"
    cat /tmp/changelog-response.txt 2>/dev/null
    exit 1
fi

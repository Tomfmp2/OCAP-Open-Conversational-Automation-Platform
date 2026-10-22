#!/usr/bin/env bash
# Levanta OCAP con Docker Compose: prepara el .env, construye y espera a que la API y el panel respondan.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

NO_BUILD=0
for arg in "$@"; do
  case "$arg" in
    --no-build) NO_BUILD=1 ;;
    -h|--help)
      echo "Uso: ./scripts/ocap-up.sh [--no-build]"
      echo "  --no-build  docker compose up -d sin reconstruir las imágenes"
      exit 0
      ;;
  esac
done

if ! docker compose version >/dev/null 2>&1; then
  echo "Error: Docker Compose no está disponible." >&2
  exit 1
fi

"$ROOT/scripts/ocap-setup.sh"

env_value() {
  grep -E "^$1=" .env | tail -n1 | cut -d= -f2- || true
}
PANEL_PORT="$(env_value FRONTEND_HOST_PORT)"; PANEL_PORT="${PANEL_PORT:-3000}"
API_PORT="$(env_value API_HOST_PORT)"; API_PORT="${API_PORT:-5000}"

if [[ "$NO_BUILD" -eq 1 ]]; then
  docker compose up -d
else
  docker compose up --build -d
fi

wait_for() {
  local name="$1" url="$2" tries=72
  echo -n "==> Esperando $name "
  for ((i = 0; i < tries; i++)); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      echo " OK"
      return 0
    fi
    echo -n "."
    sleep 5
  done
  echo
  echo "Error: $name no respondió a tiempo. Revisa: docker compose logs" >&2
  return 1
}

wait_for "la API" "http://127.0.0.1:${API_PORT}/health/ready"
wait_for "el panel" "http://127.0.0.1:${PANEL_PORT}/"

echo
echo "=============================================="
echo "  OCAP en marcha"
echo "  Panel: http://localhost:${PANEL_PORT}"
echo "  API:   http://localhost:${API_PORT}"

if curl -fsS "http://127.0.0.1:${API_PORT}/api/setup/status" 2>/dev/null | grep -q '"required":true'; then
  # El código solo se imprime en los logs de la API: quien levanta el servidor es quien configura.
  CODE="$(docker compose logs ocap-api 2>/dev/null | grep -oE '[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}' | tail -n1 || true)"
  echo
  echo "  Configuración inicial pendiente:"
  echo "  1. Abre http://localhost:${PANEL_PORT}/setup"
  if [[ -n "$CODE" ]]; then
    echo "  2. Ingresa el código: ${CODE}"
  else
    echo "  2. Busca el código en: docker compose logs ocap-api | grep 'código'"
  fi
fi
echo "=============================================="
echo "  Detener:     docker compose down"
echo "  Reset total: docker compose down -v && ./scripts/ocap-up.sh"

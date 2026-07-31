#!/usr/bin/env bash
# Monta el stack OCAP (API + frontend + dependencias) con Docker Compose.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

FROM_GENERATED=0
NO_BUILD=0
for arg in "$@"; do
  case "$arg" in
    --from-generated) FROM_GENERATED=1 ;;
    --no-build) NO_BUILD=1 ;;
    -h|--help)
      echo "Uso: ./scripts/ocap-up.sh [--from-generated] [--no-build]"
      echo "  --from-generated  Usa config/generated.env como .env si existe"
      echo "  --no-build        docker compose up -d sin --build"
      exit 0
      ;;
  esac
done

echo "==> OCAP up (raíz: $ROOT)"

if ! command -v docker >/dev/null 2>&1; then
  echo "Error: Docker no está instalado o no está en PATH." >&2
  exit 1
fi

if ! docker compose version >/dev/null 2>&1; then
  echo "Error: 'docker compose' no está disponible." >&2
  exit 1
fi

if [[ ! -f docker-compose.yml ]]; then
  echo "Error: no se encontró docker-compose.yml en $ROOT" >&2
  exit 1
fi

ensure_env() {
  if [[ -f .env ]]; then
    echo "==> Usando .env existente"
    return
  fi

  if [[ "$FROM_GENERATED" -eq 1 && -f config/generated.env ]]; then
    echo "==> Copiando config/generated.env -> .env"
    # Quita BOM si existe
    sed '1s/^\xEF\xBB\xBF//' config/generated.env > .env
    return
  fi

  if [[ -f .env.example ]]; then
    echo "==> Copiando .env.example -> .env"
    cp .env.example .env
    return
  fi

  echo "Error: no hay .env ni .env.example. Genera uno con el instalador o DeploymentManager." >&2
  exit 1
}

# Fuerza puertos estándar del panel/API en Local para no dejar el stack inaccesible.
normalize_local_ports() {
  if [[ ! -f .env ]]; then
    return
  fi
  # Solo normaliza si DEPLOYMENT_TARGET=Local o no está definido
  local target
  target="$(grep -E '^DEPLOYMENT_TARGET=' .env | tail -n1 | cut -d= -f2- || true)"
  if [[ -n "$target" && "$target" != "Local" ]]; then
    echo "==> DEPLOYMENT_TARGET=$target — no se fuerzan puertos 3000/5000"
    return
  fi

  if grep -q '^FRONTEND_HOST_PORT=' .env; then
    sed -i 's/^FRONTEND_HOST_PORT=.*/FRONTEND_HOST_PORT=3000/' .env
  else
    echo 'FRONTEND_HOST_PORT=3000' >> .env
  fi
  if grep -q '^API_HOST_PORT=' .env; then
    sed -i 's/^API_HOST_PORT=.*/API_HOST_PORT=5000/' .env
  else
    echo 'API_HOST_PORT=5000' >> .env
  fi
  if grep -q '^PUBLIC_PANEL_URL=' .env; then
    sed -i 's|^PUBLIC_PANEL_URL=.*|PUBLIC_PANEL_URL=http://localhost:3000|' .env
  fi
  if grep -q '^PUBLIC_API_URL=' .env; then
    sed -i 's|^PUBLIC_API_URL=.*|PUBLIC_API_URL=http://localhost:5000|' .env
  fi
  # Password de Compose por defecto: cambiarlo sin down -v rompe la API.
  if grep -q '^POSTGRES_PASSWORD=' .env; then
    sed -i 's/^POSTGRES_PASSWORD=.*/POSTGRES_PASSWORD=OcapSecurePass2026!/' .env
  else
    echo 'POSTGRES_PASSWORD=OcapSecurePass2026!' >> .env
  fi
  echo "==> Puertos Local normalizados a frontend :3000 / API :5000 (Postgres password = default Compose)"
}

ensure_env
normalize_local_ports
mkdir -p config

if [[ "$NO_BUILD" -eq 1 ]]; then
  echo "==> docker compose up -d"
  docker compose up -d
else
  echo "==> docker compose up --build -d"
  docker compose up --build -d
fi

# Si algún servicio quedó en Created tras un healthcheck flaky, reintentar up.
docker compose up -d >/dev/null 2>&1 || true

wait_healthy() {
  local service="$1"
  local retries="${2:-60}"
  local i=0
  echo -n "==> Esperando healthy: $service "
  while (( i < retries )); do
    local status
    status="$(docker compose ps --format json "$service" 2>/dev/null | head -n1 || true)"
    if echo "$status" | grep -qi '"Health":"healthy"'; then
      echo " OK"
      return 0
    fi
    # Fallback: container running + health endpoint for api/frontend
    if [[ "$service" == "ocap-api" ]] && curl -fsS "http://127.0.0.1:5000/health/ready" >/dev/null 2>&1; then
      echo " OK (ready)"
      return 0
    fi
    if [[ "$service" == "ocap-frontend" ]] && curl -fsS "http://127.0.0.1:3000/" >/dev/null 2>&1; then
      echo " OK (http)"
      return 0
    fi
    echo -n "."
    sleep 5
    ((i++)) || true
  done
  echo
  echo "Error: $service no quedó healthy a tiempo. Revisa: docker compose logs $service" >&2
  docker compose ps
  return 1
}

wait_healthy ocap-api 72
wait_healthy ocap-frontend 72

echo
echo "=============================================="
echo "  OCAP montado"
echo "=============================================="
echo "  Panel admin:  http://localhost:3000"
echo "  Instalador:   http://localhost:3000/installer"
echo "  API:          http://localhost:5000"
echo "  Health:       http://localhost:5000/health/ready"
echo
echo "  Reset total:  docker compose down -v && ./scripts/ocap-up.sh"
echo "=============================================="

#!/usr/bin/env bash
# Corre en local las mismas comprobaciones que .github/workflows/ci.yml, sin depender de GitHub Actions.
#
#   ./scripts/ci-local.sh            backend + frontend + dependencias vulnerables (lo que corre el hook pre-push)
#   ./scripts/ci-local.sh --docker   además construye las imágenes y las escanea con Trivy
#   ./scripts/ci-local.sh --e2e      además levanta un stack aislado y corre el smoke de Playwright
#   ./scripts/ci-local.sh --all      todo lo anterior
#
# Termina con código distinto de 0 en cuanto falla un paso.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

RUN_DOCKER=0
RUN_E2E=0
for arg in "$@"; do
  case "$arg" in
    --docker) RUN_DOCKER=1 ;;
    --e2e) RUN_E2E=1 ;;
    --all) RUN_DOCKER=1; RUN_E2E=1 ;;
    -h|--help) sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Opción desconocida: $arg (usa --help)" >&2; exit 2 ;;
  esac
done

TRIVY_IMAGE="aquasec/trivy:0.75.0"

passed=()
current=""
started=$SECONDS

step() {
  current="$1"
  printf '\n\033[1m==> %s\033[0m\n' "$current"
}
ok() { passed+=("$current"); }

on_exit() {
  local code=$?
  if (( code != 0 )) && [[ -n "$current" ]]; then
    printf '\n\033[31m✗ Falló: %s\033[0m\n' "$current" >&2
  fi
}
trap on_exit EXIT

need() {
  command -v "$1" >/dev/null 2>&1 || { echo "Error: falta '$1' en el PATH." >&2; exit 1; }
}
need dotnet
need npm

# --- Backend (job "Backend Build & Test") ---------------------------------------------------------
step "Backend: restore"
dotnet restore OCAP.slnx --verbosity quiet
ok

step "Backend: build sin avisos"
dotnet build OCAP.slnx -c Release --no-restore -p:TreatWarningsAsErrors=true --verbosity quiet
ok

step "Backend: pruebas"
dotnet test OCAP.slnx -c Release --no-build --verbosity quiet
ok

# --- Frontend (job "Frontend Lint Typecheck Build") -----------------------------------------------
pushd frontend >/dev/null
step "Frontend: dependencias"
# npm ci si el lockfile cambió desde la última instalación; si no, se reutiliza node_modules.
if [[ ! -d node_modules || package-lock.json -nt node_modules/.package-lock.json ]]; then
  npm ci --no-audit --no-fund
fi
ok

step "Frontend: lint"
npm run --silent lint
ok

step "Frontend: tipos"
npm run --silent type-check
ok

step "Frontend: pruebas"
npm test --silent
ok

step "Frontend: build"
NEXT_PUBLIC_API_URL="" npm run --silent build >/dev/null
ok
popd >/dev/null

# --- Dependencias (job "Security & Dependency Scan") ----------------------------------------------
step "Dependencias NuGet vulnerables"
vuln="$(dotnet list OCAP.slnx package --vulnerable --include-transitive 2>&1)"
if grep -q "has the following vulnerable packages" <<<"$vuln"; then
  echo "$vuln" | grep -A20 "has the following vulnerable packages"
  exit 1
fi
ok

step "Dependencias npm vulnerables (producción)"
(cd frontend && npm audit --omit=dev --audit-level=high)
ok

# --- Imágenes (job "Docker Build & Container Scan") -----------------------------------------------
if (( RUN_DOCKER )); then
  need docker
  step "Docker: imagen de la API"
  docker build -q -f docker/backend/Dockerfile -t ocap-api:ci . >/dev/null
  ok

  step "Docker: imagen del panel"
  docker build -q -f docker/frontend/Dockerfile -t ocap-frontend:ci \
    --build-arg NEXT_PUBLIC_API_URL= --build-arg API_INTERNAL_URL=http://ocap-api:5000 . >/dev/null
  ok

  for image in ocap-api:ci ocap-frontend:ci; do
    step "Trivy: $image (HIGH/CRITICAL con parche)"
    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock -v ocap-trivy-cache:/root/.cache/ \
      "$TRIVY_IMAGE" image --quiet --exit-code 1 --ignore-unfixed --severity CRITICAL,HIGH "$image"
    ok
  done
fi

# --- E2E (job "End-to-end smoke") -----------------------------------------------------------------
if (( RUN_E2E )); then
  need docker
  need openssl
  workdir="$(mktemp -d)"
  compose=(docker compose -p ocap-ci --env-file "$workdir/.env" -f docker-compose.yml -f docker/ci/compose.e2e.yml)
  cleanup_e2e() {
    "${compose[@]}" down -v --remove-orphans >/dev/null 2>&1 || true
    rm -rf "$workdir"
  }
  trap 'cleanup_e2e; on_exit' EXIT

  step "E2E: .env desechable"
  # Secretos y admin propios del E2E: nunca se toca el .env del proyecto.
  secret() { openssl rand -hex 24; }
  admin_password="E2e$(openssl rand -hex 16)"
  {
    grep -vE '^(POSTGRES_PASSWORD|RABBITMQ_PASSWORD|JWT_SECRET_KEY|VAULT_MASTER_KEY|EVOLUTION_API_KEY|BOOTSTRAP_ADMIN_EMAIL|BOOTSTRAP_ADMIN_PASSWORD)=' .env.example
    for key in POSTGRES_PASSWORD RABBITMQ_PASSWORD JWT_SECRET_KEY VAULT_MASTER_KEY EVOLUTION_API_KEY; do
      echo "$key=$(secret)"
    done
    echo "BOOTSTRAP_ADMIN_EMAIL=e2e-admin@ocap.io"
    echo "BOOTSTRAP_ADMIN_PASSWORD=$admin_password"
  } > "$workdir/.env"
  ok

  step "E2E: levantar el stack aislado"
  "${compose[@]}" up -d --build --quiet-pull ocap-api ocap-frontend >/dev/null
  for _ in $(seq 1 72); do
    states="$(docker inspect -f '{{.State.Health.Status}}' ocap-ci-api ocap-ci-frontend 2>/dev/null | tr '\n' ' ')"
    [[ "$states" == "healthy healthy " ]] && break
    sleep 5
  done
  [[ "$states" == "healthy healthy " ]] || { "${compose[@]}" logs --tail 80 ocap-api; exit 1; }
  ok

  step "E2E: smoke de Playwright"
  # La imagen debe ser de la misma versión que el @playwright/test instalado.
  playwright_version="$(node -p "require('./frontend/node_modules/@playwright/test/package.json').version")"
  network="$(docker inspect -f '{{range $k, $v := .NetworkSettings.Networks}}{{$k}}{{end}}' ocap-ci-api)"
  docker run --rm --network "$network" --ipc=host \
    -e OCAP_BASE_URL=http://ocap-frontend:3000 \
    -e OCAP_ADMIN_EMAIL=e2e-admin@ocap.io -e OCAP_ADMIN_PASSWORD="$admin_password" \
    -v "$ROOT/frontend:/work:ro" \
    "mcr.microsoft.com/playwright:v${playwright_version}-noble" \
    bash -c 'mkdir -p /tmp/w && ln -s /work/node_modules /tmp/w/node_modules \
      && cp /work/playwright.config.ts /tmp/w/ && cp -r /work/e2e /tmp/w/ \
      && cd /tmp/w && npx playwright test --reporter=line'
  ok
fi

current=""
printf '\n\033[32m✓ CI local en verde (%ss)\033[0m\n' "$((SECONDS - started))"
printf '  - %s\n' "${passed[@]}"

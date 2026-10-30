#!/usr/bin/env bash
# OCAP en local sin Docker (Linux y macOS): API en memoria (:5229) y panel Next.js (:3000) en la misma terminal.
# Equivalente a scripts/ocap-dev.ps1. Ctrl+C detiene ambos procesos.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="$ROOT/.env"

for tool in dotnet npm; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "Error: falta '$tool' en el PATH." >&2
    exit 1
  fi
done

# Carga el .env línea a línea (sin `source`: los valores pueden tener caracteres que el shell interpretaría).
if [[ -f "$ENV_FILE" ]]; then
  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line#"${line%%[![:space:]]*}"}"
    [[ -z "$line" || "$line" == \#* || "$line" != *=* ]] && continue
    key="${line%%=*}"
    key="${key%"${key##*[![:space:]]}"}"
    [[ "$key" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] || continue
    value="${line#*=}"
    # ocap install guarda los textos libres entre comillas simples.
    if [[ "$value" == \'*\' && ${#value} -ge 2 ]]; then value="${value:1:${#value}-2}"; fi
    export "$key=$value"
  done < "$ENV_FILE"
fi

export ASPNETCORE_ENVIRONMENT=Development
export UseInMemory="${UseInMemory:-true}"
if [[ -n "${BOOTSTRAP_ADMIN_EMAIL:-}" && -n "${BOOTSTRAP_ADMIN_PASSWORD:-}" ]]; then
  export Bootstrap__AdminEmail="$BOOTSTRAP_ADMIN_EMAIL"
  export Bootstrap__AdminPassword="$BOOTSTRAP_ADMIN_PASSWORD"
fi
# WhatsApp con Evolution en Docker (docker compose up -d evolution-api): la API lee estos nombres.
[[ -n "${EVOLUTION_API_KEY:-}" ]] && export WhatsApp__ApiKey="${WhatsApp__ApiKey:-$EVOLUTION_API_KEY}"

# Cada proceso en su propio grupo: al salir se termina el grupo entero (dotnet, next y sus hijos),
# no solo el subshell que los lanzó.
set -m
pids=()
cleanup() {
  trap - INT TERM EXIT
  for pid in "${pids[@]}"; do kill -TERM -- "-$pid" 2>/dev/null || true; done
  wait 2>/dev/null || true
}
trap cleanup INT TERM EXIT

# Antepone [api] / [web] a cada línea para distinguir los dos procesos en la misma terminal.
prefix() { while IFS= read -r line; do printf '%s %s\n' "$1" "$line"; done; }

(cd "$ROOT" && dotnet run --project src/Api/OCAP.Api --launch-profile http 2>&1 | prefix "[api]") &
pids+=($!)

(
  cd "$ROOT/frontend"
  [[ -d node_modules ]] || npm install
  npm run dev 2>&1 | prefix "[web]"
) &
pids+=($!)

cat <<'EOF'
OCAP local (sin Docker)
  API    http://localhost:5229
  Panel  http://localhost:3000
Primer arranque: abre http://localhost:3000/setup con el código que imprime [api].
La base es en memoria: define BOOTSTRAP_ADMIN_EMAIL y BOOTSTRAP_ADMIN_PASSWORD en .env para no repetirlo.
Sin keys de IA el asistente responde en modo demo. Ctrl+C para detener.
EOF

# Si uno de los dos procesos termina (p. ej. error de compilación), se detiene el otro.
# Sin `wait -n`: macOS trae bash 3.2.
while true; do
  for pid in "${pids[@]}"; do
    kill -0 "$pid" 2>/dev/null || exit 0
  done
  sleep 1
done

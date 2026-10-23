#!/usr/bin/env bash
# Prepara el .env de OCAP: lo crea desde .env.example y genera los secretos que falten.
# Es idempotente: nunca cambia un valor que ya tenga contenido, así que se puede ejecutar siempre.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="$ROOT/.env"
EXAMPLE="$ROOT/.env.example"
SECRETS=(POSTGRES_PASSWORD RABBITMQ_PASSWORD JWT_SECRET_KEY VAULT_MASTER_KEY EVOLUTION_API_KEY WHATSAPP_WEBHOOK_SECRET)

if [[ ! -f "$EXAMPLE" ]]; then
  echo "Error: no se encontró $EXAMPLE" >&2
  exit 1
fi

random_secret() {
  # 48 caracteres hexadecimales (192 bits): válidos en URLs de conexión y sin caracteres que el shell interprete.
  # Se lee una cantidad finita de /dev/urandom: con `tr < /dev/urandom | head` el SIGPIPE corta el script (pipefail).
  head -c 24 /dev/urandom | od -An -tx1 | tr -d ' \n'
}

if [[ ! -f "$ENV_FILE" ]]; then
  cp "$EXAMPLE" "$ENV_FILE"
  echo "==> .env creado desde .env.example"
else
  echo "==> Usando el .env existente"
fi
chmod 600 "$ENV_FILE"

generated=()
for key in "${SECRETS[@]}"; do
  current="$(grep -E "^${key}=" "$ENV_FILE" | tail -n1 | cut -d= -f2- || true)"
  if [[ -n "$current" ]]; then
    continue
  fi
  value="$(random_secret)"
  if grep -qE "^${key}=" "$ENV_FILE"; then
    sed -i "s|^${key}=.*|${key}=${value}|" "$ENV_FILE"
  else
    echo "${key}=${value}" >> "$ENV_FILE"
  fi
  generated+=("$key")
done

if (( ${#generated[@]} )); then
  echo "==> Secretos generados: ${generated[*]}"
else
  echo "==> Los secretos ya estaban definidos; no se cambió nada"
fi
echo "==> Listo. Levanta OCAP con ./scripts/ocap-up.sh"

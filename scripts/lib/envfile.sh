# shellcheck shell=bash
# Lectura y escritura del .env de OCAP sin evaluarlo. Los textos libres (nombres, correo, contraseña, API keys) se
# guardan entre comillas simples: Docker Compose no interpreta `$` dentro de ellas. Por eso no pueden contener `'`.

# Valor de una clave (sin las comillas simples con que se haya guardado); vacío si no existe.
env_get() {
  local file="$1" key="$2" line value
  [[ -f "$file" ]] || return 0
  line="$(grep -E "^${key}=" "$file" | tail -n1 || true)"
  value="${line#*=}"
  [[ "$line" == "" ]] && return 0
  if [[ "$value" == \'*\' && ${#value} -ge 2 ]]; then value="${value:1:${#value}-2}"; fi
  printf '%s' "$value"
}

# Fija una clave (la reemplaza o la agrega al final). quote=1 la guarda entre comillas simples.
env_set() {
  local file="$1" key="$2" value="$3" quote="${4:-0}" tmp
  if [[ "$quote" == "1" ]]; then value="'${value}'"; fi
  tmp="$(mktemp "${file}.XXXXXX")"
  # ENVIRON evita que awk reinterprete barras invertidas del valor.
  OCAP_ENV_KEY="$key" OCAP_ENV_VALUE="$value" awk '
    BEGIN { key = ENVIRON["OCAP_ENV_KEY"]; value = ENVIRON["OCAP_ENV_VALUE"]; done = 0 }
    index($0, key "=") == 1 { if (!done) { print key "=" value; done = 1 }; next }
    { print }
    END { if (!done) print key "=" value }
  ' "$file" > "$tmp"
  chmod 600 "$tmp"
  mv "$tmp" "$file"
}

# shellcheck shell=bash
# Asistente de la configuración inicial de `ocap install` (y de `ocap config`): idioma primero, luego organización,
# cuenta de administrador, IA, acceso desde la red, navegador e inicio automático. Revisa los puertos solo y al
# final muestra un resumen. Las respuestas quedan en variables W_* y wizard_apply las escribe en el .env.
#
# Sin terminal, o con OCAP_YES=1, no pregunta: toma OCAP_LANG, OCAP_ORG_NAME, OCAP_ADMIN_NAME, OCAP_ADMIN_EMAIL,
# OCAP_ADMIN_PASSWORD, OCAP_AUTOSTART (1/0) y valores por defecto.

MIN_PASSWORD_LENGTH=12

# Puertos que publica docker-compose.yml: clave del .env, valor por defecto y nombre para mostrar.
OCAP_PORTS="FRONTEND_HOST_PORT:3000:panel API_HOST_PORT:5000:API POSTGRES_HOST_PORT:5433:PostgreSQL REDIS_HOST_PORT:6379:Redis RABBITMQ_HOST_PORT:5672:RabbitMQ RABBITMQ_UI_HOST_PORT:15672:RabbitMQ-UI NATS_HOST_PORT:4222:NATS NATS_MONITOR_HOST_PORT:8222:NATS-monitor JAEGER_HOST_PORT:16686:Jaeger OTLP_GRPC_HOST_PORT:4317:OTLP-gRPC OTLP_HTTP_HOST_PORT:4318:OTLP-HTTP PROMETHEUS_HOST_PORT:9090:Prometheus NGINX_HOST_PORT:80:Nginx EVOLUTION_HOST_PORT:8088:Evolution"

interactive() { [[ "${OCAP_YES:-}" != "1" && -r /dev/tty && -w /dev/tty ]]; }

_ask() { # prompt -> respuesta por stdout (lee de la terminal también con curl | bash)
  local answer
  printf '%s' "$1" > /dev/tty
  IFS= read -r answer < /dev/tty || answer=""
  printf '%s' "$answer"
}

_ask_secret() {
  local answer
  printf '%s' "$1" > /dev/tty
  IFS= read -rs answer < /dev/tty || answer=""
  printf '\n' > /dev/tty
  printf '%s' "$answer"
}

# ask_yes_no "pregunta" s|n (valor por defecto) -> 0 = sí
ask_yes_no() {
  local default="$2" hint answer
  if [[ "$default" == "s" ]]; then hint="$(t yes_no_default_yes)"; else hint="$(t yes_no_default_no)"; fi
  answer="$(_ask "$1$hint")"
  [[ -z "$answer" ]] && answer="$default"
  [[ "$answer" =~ ^[sSyY] ]]
}

_section() { printf '\n\033[1m%s\033[0m\n' "$1" > /dev/tty; }

valid_email() { [[ "$1" =~ ^[^@[:space:]\']+@[^@[:space:]\']+\.[^@[:space:]\']+$ ]]; }

strong_password() {
  local p="$1"
  [[ ${#p} -ge $MIN_PASSWORD_LENGTH && "$p" =~ [[:alpha:]] && "$p" =~ [0-9] ]]
}

# Texto libre seguro para el .env: sin comillas simples ni saltos de línea.
clean_text() { printf '%s' "$1" | tr -d "'\r\n" | sed 's/^ *//; s/ *$//'; }

slugify() {
  local s
  s="$(printf '%s' "$1" | iconv -f utf-8 -t ascii//TRANSLIT 2>/dev/null || printf '%s' "$1")"
  s="$(printf '%s' "$s" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]\{1,\}/-/g; s/^-//; s/-$//' | cut -c1-40)"
  printf '%s' "${s:-default}"
}

port_in_use() { (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null; }

# Primer puerto libre desde $1 que no esté ya asignado a otro servicio de OCAP ($2: lista de puertos tomados).
free_port_from() {
  local p="$1" taken=" ${2:-} "
  while port_in_use "$p" || [[ "$taken" == *" $p "* ]]; do p=$((p + 1)); done
  printf '%s' "$p"
}

lan_ip() {
  if [[ "$(uname -s)" == "Darwin" ]]; then
    ipconfig getifaddr en0 2>/dev/null || true
  else
    ip route get 1.1.1.1 2>/dev/null | awk '{for (i = 1; i <= NF; i++) if ($i == "src") { print $(i + 1); exit }}'
  fi
}

ask_language() {
  # Ya elegido (p. ej. por install.sh, que pregunta antes de descargar): no se vuelve a preguntar.
  if [[ -n "${OCAP_LANG:-}" ]]; then OCAP_LANGUAGE="$OCAP_LANG"; export OCAP_LANGUAGE; return; fi
  if ! interactive; then
    OCAP_LANGUAGE="${OCAP_LANG:-${OCAP_LANGUAGE:-es}}"
    return
  fi
  local answer
  answer="$(_ask "$(t lang_prompt)")"
  case "$answer" in
    2|en|EN|English|english) OCAP_LANGUAGE=en ;;
    *) OCAP_LANGUAGE=es ;;
  esac
  export OCAP_LANGUAGE
}

_ask_admin() {
  _section "$(t section_admin)"
  W_ADMIN_NAME="$(clean_text "$(_ask "$(t ask_admin_name "$(t default_admin_name)")")")"
  [[ -z "$W_ADMIN_NAME" ]] && W_ADMIN_NAME="$(t default_admin_name)"

  while :; do
    W_ADMIN_EMAIL="$(clean_text "$(_ask "$(t ask_email)")")"
    valid_email "$W_ADMIN_EMAIL" && break
    t bad_email > /dev/tty; echo > /dev/tty
  done

  local again
  while :; do
    W_ADMIN_PASSWORD="$(_ask_secret "$(t ask_password)")"
    if [[ "$W_ADMIN_PASSWORD" == *"'"* ]]; then t quote_password "'" > /dev/tty; echo > /dev/tty; continue; fi
    if ! strong_password "$W_ADMIN_PASSWORD"; then t weak_password > /dev/tty; echo > /dev/tty; continue; fi
    again="$(_ask_secret "$(t ask_password_again)")"
    [[ "$again" == "$W_ADMIN_PASSWORD" ]] && break
    t password_mismatch > /dev/tty; echo > /dev/tty
  done
}

_ask_ai() {
  _section "$(t section_ai)"
  local choice
  choice="$(_ask "$(t ask_ai)")"
  case "$choice" in
    1) W_AI_PROVIDER=Gemini ;;
    2) W_AI_PROVIDER=OpenAI ;;
    3) W_AI_PROVIDER=Claude ;;
    *) W_AI_PROVIDER=""; W_AI_KEY=""; return ;;
  esac
  W_AI_KEY="$(clean_text "$(_ask_secret "$(t ask_ai_key "$W_AI_PROVIDER")")")"
  if [[ -z "$W_AI_KEY" ]]; then
    W_AI_PROVIDER=""
    t empty_key > /dev/tty; echo > /dev/tty
  fi
}

_ask_prefs() {
  _section "$(t section_prefs)"
  if ask_yes_no "$(t ask_lan)" n; then W_BIND=0.0.0.0; t lan_warning > /dev/tty; echo > /dev/tty; else W_BIND=127.0.0.1; fi
  if ask_yes_no "$(t ask_browser)" s; then W_BROWSER=true; else W_BROWSER=false; fi
  if ask_yes_no "$(t ask_autostart)" n; then W_AUTOSTART=1; else W_AUTOSTART=0; fi
}

# Para cada puerto: el del .env (o el por defecto); si está ocupado por otro programa, el siguiente libre.
_resolve_ports() {
  local env_file="$1" entry key default label current chosen taken=""
  W_PORTS=""
  for entry in $OCAP_PORTS; do
    key="${entry%%:*}"; default="${entry#*:}"; label="${default#*:}"; default="${default%%:*}"
    current="$(env_get "$env_file" "$key")"
    current="${current:-$default}"
    # Con OCAP encendido, sus propios puertos aparecen ocupados: se conservan.
    if [[ "${OCAP_SKIP_PORT_CHECK:-}" == "1" ]]; then chosen="$current"; else chosen="$(free_port_from "$current" "$taken")"; fi
    taken="$taken $chosen"
    if [[ "$chosen" != "$current" ]]; then
      t port_changed "$current" "$label" "$chosen" > /dev/tty; echo > /dev/tty
    fi
    W_PORTS="$W_PORTS $key=$chosen"
  done
}

_port_of() { # clave -> puerto elegido
  local pair
  for pair in $W_PORTS; do [[ "${pair%%=*}" == "$1" ]] && { printf '%s' "${pair#*=}"; return; }; done
}

_yes_no() { if [[ "$1" == "$2" ]]; then t yes; else t no; fi; }

# Línea del resumen alineada por caracteres (printf %-Ns cuenta bytes y desalinea tildes y eñes).
_row() {
  local label="$1" value="$2" pad=22
  printf '  %s%*s %s\n' "$label" $((pad - ${#label})) '' "$value"
}

_summary() {
  local ai="${W_AI_PROVIDER:-$(t summary_demo)}" access panel
  if [[ "$W_BIND" == "0.0.0.0" ]]; then access="$(t summary_lan)"; else access="$(t summary_local)"; fi
  panel="http://localhost:$(_port_of FRONTEND_HOST_PORT)"
  {
    _section "$(t summary)"
    if [[ "$OCAP_LANGUAGE" == en ]]; then _row "$(t summary_lang)" English; else _row "$(t summary_lang)" Español; fi
    [[ -n "${W_ORG:-}" ]] && _row "$(t summary_org)" "$W_ORG"
    [[ -n "${W_ADMIN_EMAIL:-}" ]] && _row "$(t summary_admin)" "$W_ADMIN_EMAIL ($W_ADMIN_NAME)"
    _row "$(t summary_ai)" "$ai"
    _row "$(t summary_access)" "$access"
    _row "$(t summary_browser)" "$(_yes_no "$W_BROWSER" true)"
    _row "$(t summary_autostart)" "$(_yes_no "$W_AUTOSTART" 1)"
    _row "$(t summary_panel)" "$panel"
    echo
  } > /dev/tty
}

# wizard_run install|config ENV_FILE — pregunta (o toma variables) y deja las respuestas en W_*.
wizard_run() {
  local mode="$1" env_file="$2"
  W_ORG=""; W_ADMIN_NAME=""; W_ADMIN_EMAIL=""; W_ADMIN_PASSWORD=""; W_AI_PROVIDER=""; W_AI_KEY=""
  W_BIND="$(env_get "$env_file" OCAP_BIND_ADDRESS)"; W_BIND="${W_BIND:-127.0.0.1}"
  W_BROWSER="$(env_get "$env_file" OCAP_OPEN_BROWSER)"; W_BROWSER="${W_BROWSER:-true}"
  W_AUTOSTART=0; autostart_is_enabled && W_AUTOSTART=1

  if ! interactive; then
    if [[ "$mode" == "install" ]]; then
      W_ORG="$(clean_text "${OCAP_ORG_NAME:-}")"
      W_ADMIN_NAME="$(clean_text "${OCAP_ADMIN_NAME:-}")"
      W_ADMIN_EMAIL="$(clean_text "${OCAP_ADMIN_EMAIL:-}")"
      W_ADMIN_PASSWORD="${OCAP_ADMIN_PASSWORD:-}"
      [[ -n "$W_ADMIN_PASSWORD" ]] && { strong_password "$W_ADMIN_PASSWORD" || fail "$(t weak_password)"; }
    fi
    [[ -n "${OCAP_AUTOSTART:-}" ]] && W_AUTOSTART="$OCAP_AUTOSTART"
    _resolve_ports "$env_file"
    return
  fi

  while :; do
    if [[ "$mode" == "install" ]]; then
      t welcome > /dev/tty; echo > /dev/tty
      _section "$(t section_org)"
      W_ORG="$(clean_text "$(_ask "$(t ask_org "$(t default_org)")")")"
      [[ -z "$W_ORG" ]] && W_ORG="$(t default_org)"
      _ask_admin
    fi
    _ask_ai
    _ask_prefs
    _resolve_ports "$env_file"
    _summary
    if [[ "$mode" == "install" ]]; then ask_yes_no "$(t confirm_summary)" s && break
    else ask_yes_no "$(t confirm_config)" s && break
    fi
    t restart_wizard > /dev/tty; echo > /dev/tty
  done
}

# Escribe en el .env lo que se respondió (el .env ya existe y tiene sus secretos).
wizard_apply() {
  local env_file="$1" pair panel_port
  env_set "$env_file" OCAP_LANGUAGE "$OCAP_LANGUAGE"
  env_set "$env_file" OCAP_BIND_ADDRESS "$W_BIND"
  env_set "$env_file" OCAP_OPEN_BROWSER "$W_BROWSER"
  for pair in $W_PORTS; do env_set "$env_file" "${pair%%=*}" "${pair#*=}"; done
  panel_port="$(_port_of FRONTEND_HOST_PORT)"
  env_set "$env_file" PUBLIC_PANEL_URL "http://localhost:${panel_port}"

  if [[ -n "${W_ADMIN_EMAIL:-}" && -n "${W_ADMIN_PASSWORD:-}" ]]; then
    env_set "$env_file" BOOTSTRAP_TENANT_NAME "$W_ORG" 1
    env_set "$env_file" BOOTSTRAP_TENANT_SLUG "$(slugify "$W_ORG")"
    env_set "$env_file" BOOTSTRAP_ADMIN_NAME "$W_ADMIN_NAME" 1
    env_set "$env_file" BOOTSTRAP_ADMIN_EMAIL "$W_ADMIN_EMAIL" 1
    env_set "$env_file" BOOTSTRAP_ADMIN_PASSWORD "$W_ADMIN_PASSWORD" 1
  fi
  if [[ -n "${W_AI_PROVIDER:-}" ]]; then
    env_set "$env_file" "AiProviders__${W_AI_PROVIDER}__ApiKey" "$W_AI_KEY" 1
  fi
}

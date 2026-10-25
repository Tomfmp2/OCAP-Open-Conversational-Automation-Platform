#!/usr/bin/env bash
# Instalador de OCAP en una línea:
#
#   curl -fsSL https://raw.githubusercontent.com/Tomfmp2/OCAP-Open-Conversational-Automation-Platform/main/install.sh | bash
#
# Instala git si falta, clona (o actualiza) el repo en ~/ocap y ejecuta `ocap install`, que instala Docker y lo
# demás que haga falta (pidiendo confirmación) y deja el comando `ocap` listo.
#
# Variables: OCAP_DIR (carpeta destino, ~/ocap), OCAP_REPO (URL), OCAP_BRANCH (main), OCAP_YES=1 (sin preguntas).
set -euo pipefail

OCAP_REPO="${OCAP_REPO:-https://github.com/Tomfmp2/OCAP-Open-Conversational-Automation-Platform.git}"
OCAP_BRANCH="${OCAP_BRANCH:-main}"
OCAP_DIR="${OCAP_DIR:-$HOME/ocap}"

info() { printf '==> %s\n' "$*"; }
fail() { printf '\033[31mError:\033[0m %s\n' "$*" >&2; exit 1; }

if [[ ${EUID:-$(id -u)} -eq 0 ]]; then SUDO=(); else SUDO=(sudo); fi

# Solo git hace falta antes de clonar; Docker y el resto los instala `ocap install`.
install_git() {
  command -v git >/dev/null 2>&1 && return 0
  info "Instalando git…"
  if [[ "$(uname -s)" == "Darwin" ]]; then
    command -v brew >/dev/null 2>&1 && brew install git && return 0
    fail "instala git (xcode-select --install) y repite."
  fi
  if command -v pacman >/dev/null 2>&1; then "${SUDO[@]}" pacman -Sy --needed --noconfirm git
  elif command -v apt-get >/dev/null 2>&1; then "${SUDO[@]}" apt-get update -qq && "${SUDO[@]}" apt-get install -y -qq git
  elif command -v dnf >/dev/null 2>&1; then "${SUDO[@]}" dnf -y install git
  else fail "instala git y repite."
  fi
}

main() {
  case "$(uname -s)" in
    Linux|Darwin) ;;
    *) fail "OCAP se instala en Linux o macOS. En Windows, ejecútalo dentro de WSL2." ;;
  esac

  install_git

  if [[ -d "$OCAP_DIR/.git" ]]; then
    info "Actualizando OCAP en $OCAP_DIR"
    git -C "$OCAP_DIR" pull --ff-only
  else
    [[ -e "$OCAP_DIR" ]] && fail "$OCAP_DIR ya existe y no es un clon de OCAP. Usa OCAP_DIR=/otra/carpeta."
    info "Descargando OCAP en $OCAP_DIR"
    git clone --branch "$OCAP_BRANCH" "$OCAP_REPO" "$OCAP_DIR"
  fi

  exec "$OCAP_DIR/ocap" install
}

# Todo dentro de main: con `curl | bash` nada se ejecuta hasta descargar el script completo.
main "$@"

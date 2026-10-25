# shellcheck shell=bash
# Dependencias de OCAP (Docker con Compose v2, git y curl): detecta lo que falta y lo instala con el gestor de
# paquetes del sistema, pidiendo confirmación. Lo usan `ocap install` y `install.sh`.
#
# Sistemas: Arch/CachyOS/Manjaro/EndeavourOS (pacman), Debian/Ubuntu/Mint/Pop (apt + repo oficial de Docker),
# Fedora/RHEL/CentOS/Rocky/Alma (dnf + repo oficial), macOS (Homebrew + Colima). En Linux se instala Docker Engine.
#
# Variables: OCAP_YES=1 responde "sí" a las confirmaciones (instalación desatendida).

# Comando de Docker de esta sesión: pasa a `sudo docker` si el usuario recién se agregó al grupo docker
# (el grupo solo rige desde la próxima sesión).
DOCKER=(docker)
dk() { "${DOCKER[@]}" "$@"; }

# Sin sudo cuando ya se es root (p. ej. contenedores o servidores).
if [[ ${EUID:-$(id -u)} -eq 0 ]]; then SUDO=(); else SUDO=(sudo); fi
as_root() { "${SUDO[@]}" "$@"; }

_pr_info() { printf '==> %s\n' "$*"; }
_pr_fail() { printf '\033[31mError:\033[0m %s\n' "$*" >&2; exit 1; }

# Pregunta s/N leyendo de la terminal (funciona también con `curl … | bash`).
confirm() {
  [[ "${OCAP_YES:-}" == "1" ]] && return 0
  local answer
  if [[ -r /dev/tty ]]; then
    read -r -p "$1 [s/N] " answer < /dev/tty || answer=""
  else
    _pr_fail "se necesita confirmación y no hay terminal. Repite con OCAP_YES=1 para aceptar."
  fi
  [[ "$answer" =~ ^[sSyY] ]]
}

# Valor de /etc/os-release (ID, ID_LIKE, VERSION_CODENAME…), vacío si no existe.
os_release() {
  # shellcheck source=/dev/null
  [[ -r /etc/os-release ]] && (. /etc/os-release && eval "printf '%s' \"\${$1:-}\"")
}

# Familia del sistema: arch | debian | fedora | macos | unsupported.
os_family() {
  if [[ "$(uname -s)" == "Darwin" ]]; then echo macos; return; fi
  [[ -r /etc/os-release ]] || { echo unsupported; return; }
  local id like
  id="$(os_release ID)"
  like="$(os_release ID_LIKE)"
  case " $id $like " in
    *" arch "*|*" cachyos "*|*" manjaro "*|*" endeavouros "*) echo arch ;;
    *" debian "*|*" ubuntu "*) echo debian ;;
    *" fedora "*|*" rhel "*|*" centos "*) echo fedora ;;
    *) echo unsupported ;;
  esac
}

missing_prerequisites() {
  local missing=()
  command -v git >/dev/null 2>&1 || missing+=(git)
  command -v curl >/dev/null 2>&1 || missing+=(curl)
  if ! command -v docker >/dev/null 2>&1; then
    missing+=(docker)
  elif ! docker compose version >/dev/null 2>&1; then
    missing+=(docker-compose)
  fi
  echo "${missing[*]}"
}

_need_sudo() {
  [[ ${#SUDO[@]} -eq 0 ]] && return 0
  command -v sudo >/dev/null 2>&1 || _pr_fail "se necesita sudo (o ejecutar como root) para instalar paquetes."
}

_install_arch() {
  as_root pacman -Sy --needed --noconfirm docker docker-compose docker-buildx git curl
}

_install_debian() {
  local id codename
  id="$(os_release ID)"
  codename="$(os_release VERSION_CODENAME)"
  # Derivadas (Mint, Pop!_OS…) usan el repositorio de la distro base.
  case "$id" in
    debian|ubuntu) ;;
    *)
      if [[ "$(os_release ID_LIKE)" == *ubuntu* ]]; then
        id=ubuntu
        codename="$(os_release UBUNTU_CODENAME)"
      else
        id=debian
      fi
      ;;
  esac
  as_root apt-get update -qq
  as_root apt-get install -y -qq ca-certificates curl git
  as_root install -m 0755 -d /etc/apt/keyrings
  as_root curl -fsSL "https://download.docker.com/linux/${id}/gpg" -o /etc/apt/keyrings/docker.asc
  as_root chmod a+r /etc/apt/keyrings/docker.asc
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/${id} ${codename} stable" \
    | as_root tee /etc/apt/sources.list.d/docker.list >/dev/null
  as_root apt-get update -qq
  as_root apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
}

_install_fedora() {
  local repo="https://download.docker.com/linux/fedora/docker-ce.repo"
  local id
  id="$(os_release ID)"
  [[ "$id" != "fedora" ]] && repo="https://download.docker.com/linux/centos/docker-ce.repo"
  as_root dnf -y install dnf-plugins-core git curl
  # dnf5 (Fedora 41+) cambió la sintaxis de config-manager.
  as_root dnf config-manager addrepo --overwrite --from-repofile="$repo" 2>/dev/null \
    || as_root dnf config-manager --add-repo "$repo"
  as_root dnf -y install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
}

_install_macos() {
  if ! command -v brew >/dev/null 2>&1; then
    confirm "Falta Homebrew (el gestor de paquetes de macOS). ¿Instalarlo?" || _pr_fail "instala Homebrew: https://brew.sh"
    NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    eval "$(/opt/homebrew/bin/brew shellenv 2>/dev/null || /usr/local/bin/brew shellenv)"
  fi
  # Colima: el motor de Docker sin Docker Desktop (sin licencia ni ventana que aceptar).
  brew install git curl docker docker-compose docker-buildx colima
  mkdir -p "$HOME/.docker/cli-plugins"
  ln -sfn "$(brew --prefix)/opt/docker-compose/bin/docker-compose" "$HOME/.docker/cli-plugins/docker-compose"
  ln -sfn "$(brew --prefix)/opt/docker-buildx/bin/docker-buildx" "$HOME/.docker/cli-plugins/docker-buildx"
}

# Deja el servicio de Docker activo (y al iniciar el equipo) y al usuario con permiso para usarlo.
_enable_docker_linux() {
  if command -v systemctl >/dev/null 2>&1 && [[ -d /run/systemd/system ]]; then
    as_root systemctl enable --now docker
  else
    _pr_info "Sin systemd: inicia Docker con el servicio de tu sistema (p. ej. 'sudo service docker start')."
  fi
  grant_docker_group
}

# El grupo docker permite usar Docker sin sudo (equivale a acceso de administrador sobre el equipo).
grant_docker_group() {
  [[ ${#SUDO[@]} -eq 0 ]] && return 0
  id -nG "$USER" 2>/dev/null | tr ' ' '\n' | grep -qx docker && return 0
  as_root usermod -aG docker "$USER"
  DOCKER=(sudo docker)
  _pr_info "Tu usuario se agregó al grupo docker: desde tu próxima sesión no hará falta sudo."
}

# Instala lo que falte (con confirmación). Sin argumentos revisa todo.
ensure_prerequisites() {
  local missing family
  missing="$(missing_prerequisites)"
  [[ -z "$missing" ]] && return 0

  family="$(os_family)"
  if [[ "$family" == "unsupported" ]]; then
    _pr_fail "faltan: ${missing}. Este sistema no se instala solo; sigue https://docs.docker.com/engine/install/ (en Windows: WSL2 + Docker Desktop)."
  fi

  echo "OCAP necesita: ${missing}."
  case "$family" in
    macos) echo "Se instalará con Homebrew (Docker con Colima, sin Docker Desktop)." ;;
    *) echo "Se instalará Docker Engine con el gestor de paquetes del sistema (pedirá tu contraseña)." ;;
  esac
  confirm "¿Continuar?" || _pr_fail "instalación cancelada."

  [[ "$family" != "macos" ]] && _need_sudo
  "_install_${family}"
  [[ "$family" != "macos" ]] && _enable_docker_linux

  missing="$(missing_prerequisites)"
  [[ -z "$missing" ]] || _pr_fail "no se pudo instalar: ${missing}."
  _pr_info "Dependencias instaladas."
}

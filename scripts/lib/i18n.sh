# shellcheck shell=bash
# Textos del instalador y del comando ocap en español e inglés. `t clave [args…]` imprime el texto en el idioma
# de OCAP_LANGUAGE (es por defecto); los %s se reemplazan por los argumentos, como en printf.
# Compatible con bash 3.2 (macOS): sin arrays asociativos.

t() {
  local key="$1"
  shift
  local fmt
  if [[ "${OCAP_LANGUAGE:-es}" == "en" ]]; then fmt="$(_msg_en "$key")"; else fmt="$(_msg_es "$key")"; fi
  [[ -z "$fmt" ]] && fmt="$key"
  # shellcheck disable=SC2059 # el formato viene de este catálogo, nunca del usuario
  printf -- "$fmt" "$@"
}

_msg_es() {
  case "$1" in
    lang_prompt) printf '%s' 'Idioma / Language:\n  1) Español\n  2) English\nElige [1]: ' ;;
    welcome) echo 'Bienvenido a OCAP. Te haré unas preguntas y después instalaré todo sin interrumpirte.' ;;
    section_org) echo 'Tu organización' ;;
    ask_org) echo 'Nombre de tu organización o negocio [%s]: ' ;;
    default_org) echo 'Mi empresa' ;;
    section_admin) echo 'Cuenta de administrador (con ella entras al panel)' ;;
    ask_admin_name) echo 'Tu nombre [%s]: ' ;;
    default_admin_name) echo 'Administrador' ;;
    ask_email) echo 'Correo: ' ;;
    bad_email) echo 'Ese correo no parece válido.' ;;
    ask_password) echo 'Contraseña (mínimo 12 caracteres, con letras y números): ' ;;
    ask_password_again) echo 'Repite la contraseña: ' ;;
    weak_password) echo 'La contraseña necesita al menos 12 caracteres, con letras y números.' ;;
    quote_password) echo 'La contraseña no puede contener comillas simples (%s).' ;;
    password_mismatch) echo 'Las contraseñas no coinciden.' ;;
    section_ai) echo 'Inteligencia artificial (opcional; se puede configurar después en el panel)' ;;
    ask_ai) printf '%s' '  1) Gemini (Google)\n  2) OpenAI\n  3) Claude (Anthropic)\n  4) Ahora no (el asistente responde en modo demo)\nElige [4]: ' ;;
    ask_ai_key) echo 'API key de %s (no se mostrará): ' ;;
    empty_key) echo 'La key está vacía; queda en modo demo.' ;;
    section_prefs) echo 'Preferencias' ;;
    ask_autostart) echo '¿Iniciar OCAP automáticamente al encender el equipo?' ;;
    ask_browser) echo '¿Abrir el navegador cada vez que inicies OCAP?' ;;
    ask_lan) echo '¿Usar el panel también desde otros dispositivos de tu red (celular, otra PC)?' ;;
    lan_warning) echo '  Cualquiera en tu red podrá ver la pantalla de inicio de sesión; usa una contraseña fuerte.' ;;
    port_changed) echo 'El puerto %s (%s) está ocupado en este equipo; se usará el %s.' ;;
    yes_no_default_yes) echo ' [S/n] ' ;;
    yes_no_default_no) echo ' [s/N] ' ;;
    summary) echo 'Resumen' ;;
    summary_lang) echo 'Idioma' ;;
    summary_org) echo 'Organización' ;;
    summary_admin) echo 'Administrador' ;;
    summary_ai) echo 'IA' ;;
    summary_demo) echo 'modo demo' ;;
    summary_autostart) echo 'Iniciar al encender' ;;
    summary_browser) echo 'Abrir el navegador' ;;
    summary_access) echo 'Acceso' ;;
    summary_local) echo 'solo este equipo' ;;
    summary_lan) echo 'este equipo y tu red' ;;
    summary_panel) echo 'Panel' ;;
    yes) echo 'sí' ;;
    no) echo 'no' ;;
    confirm_summary) echo '¿Todo correcto? Se instalará y construirá OCAP (varios minutos).' ;;
    confirm_config) echo '¿Guardar estos cambios?' ;;
    restart_wizard) echo 'De acuerdo, volvamos a empezar.' ;;
    already_configured) echo 'OCAP ya está configurado en este equipo. Para cambiar idioma, IA o preferencias: ocap config' ;;
    installing_from) echo 'Instalando OCAP desde %s' ;;
    building) echo 'Construyendo las imágenes (la primera vez tarda unos minutos)…' ;;
    command_installed) echo 'Comando instalado: %s' ;;
    path_hint) echo '%s no está en tu PATH. Agrégalo y abre una terminal nueva:' ;;
    install_done) echo 'Listo. Para levantar OCAP, en cualquier terminal escribe: ocap' ;;
    start_now) echo '¿Iniciar OCAP ahora?' ;;
    config_saved) echo 'Configuración guardada.' ;;
    config_restart) echo 'Para aplicar los cambios: ocap restart' ;;
    docker_off) echo 'Docker está apagado; encendiéndolo…' ;;
    docker_ready) echo 'Docker listo' ;;
    docker_permission) echo 'Tu usuario no tiene permiso para usar Docker.' ;;
    docker_group_ask) echo '¿Agregarlo al grupo docker (pide tu contraseña)?' ;;
    docker_timeout) echo 'Docker no arrancó a tiempo. Inícialo a mano y vuelve a ejecutar: ocap' ;;
    docker_no_response) echo 'Docker no responde. Inícialo y vuelve a ejecutar: ocap' ;;
    missing_run_install) echo 'faltan: %s. Instálalos con: ocap install' ;;
    starting) echo 'Levantando OCAP…' ;;
    waiting) echo 'Esperando %s ' ;;
    the_api) echo 'la API' ;;
    the_panel) echo 'el panel' ;;
    not_responding) echo '%s no respondió a tiempo. Revisa: ocap logs' ;;
    running) echo 'OCAP en marcha' ;;
    lan_url) echo 'Desde tu red: %s' ;;
    first_setup) echo 'Primera vez: crea tu cuenta en %s' ;;
    setup_code) echo 'Código de configuración: %s' ;;
    admin_ready) echo 'Entra con %s y la contraseña que elegiste.' ;;
    password_removed) echo 'Por seguridad se quitó la contraseña del administrador del archivo .env.' ;;
    hints) echo 'Apagar: ocap stop    Estado: ocap status    Ayuda: ocap help' ;;
    port_busy) echo 'El puerto %s (%s) está ocupado por otro programa. Cámbialo con: ocap config' ;;
    stopping) echo 'Apagando OCAP (los datos se conservan)…' ;;
    already_stopped) echo 'Docker está apagado: OCAP ya está detenido.' ;;
    status_docker_off) echo 'Docker está apagado: OCAP no está corriendo. Levántalo con: ocap' ;;
    status_ok) echo 'OK' ;;
    status_down) echo 'no responde' ;;
    updating) echo 'Actualizando el código…' ;;
    reset_warning) echo 'Esto borra TODOS los datos de OCAP: usuarios, agentes, conversaciones y sesiones de WhatsApp.' ;;
    reset_confirm) echo "Escribe 'borrar' para confirmar: " ;;
    reset_word) echo 'borrar' ;;
    cancelled) echo 'Cancelado.' ;;
    backup_database) echo 'Base de datos…' ;;
    backup_files) echo 'Archivos subidos…' ;;
    backup_done) echo 'Copia de seguridad lista: %s' ;;
    backup_env_warning) echo 'Guarda también el .env en un lugar seguro: sin VAULT_MASTER_KEY las API keys y tokens de la copia no se pueden descifrar.' ;;
    restore_usage) echo 'Uso: ocap restore <archivo .tar de ocap backup>' ;;
    restore_invalid) echo 'Ese archivo no es una copia de ocap backup.' ;;
    restore_warning) echo 'Esto reemplaza TODOS los datos actuales de OCAP por los de la copia. Necesita el mismo .env con el que se hizo.' ;;
    uninstalled) echo 'Comando quitado. El repo y los datos siguen intactos (para borrar datos: ocap reset antes de desinstalar).' ;;
    not_installed) echo 'El comando ocap no estaba instalado en %s.' ;;
    unknown_command) echo 'Comando desconocido: %s' ;;
    autostart_on) echo 'OCAP se iniciará automáticamente al encender el equipo.' ;;
    autostart_off) echo 'Inicio automático desactivado.' ;;
    autostart_status_on) echo 'Inicio automático: activado' ;;
    autostart_status_off) echo 'Inicio automático: desactivado' ;;
    autostart_unsupported) echo 'Este sistema no tiene systemd ni launchd: configura el inicio automático a mano (ejecutar "%s start").' ;;
    autostart_desktop) echo 'Docker Desktop también se iniciará al entrar a tu sesión.' ;;
    autostart_linger_fail) echo 'No se pudo activar el arranque sin iniciar sesión (loginctl enable-linger); OCAP se iniciará al entrar a tu sesión.' ;;
    need_tty) echo 'La instalación interactiva necesita una terminal. Para instalar sin preguntas: OCAP_YES=1 (y opcionalmente OCAP_ADMIN_EMAIL, OCAP_ADMIN_PASSWORD, OCAP_ORG_NAME).' ;;
    prereq_need) echo 'OCAP necesita: %s.' ;;
    prereq_macos) echo 'Se instalará con Homebrew (Docker con Colima, sin Docker Desktop).' ;;
    prereq_linux) echo 'Se instalará Docker Engine con el gestor de paquetes del sistema (pedirá tu contraseña).' ;;
    prereq_continue) echo '¿Continuar?' ;;
    prereq_cancelled) echo 'instalación cancelada.' ;;
    prereq_done) echo 'Dependencias instaladas.' ;;
    prereq_failed) echo 'no se pudo instalar: %s.' ;;
    prereq_unsupported) echo 'faltan: %s. Este sistema no se instala solo; sigue https://docs.docker.com/engine/install/ (en Windows: WSL2 + Docker Desktop).' ;;
    prereq_need_sudo) echo 'se necesita sudo (o ejecutar como root) para instalar paquetes.' ;;
    prereq_brew) echo 'Falta Homebrew (el gestor de paquetes de macOS). ¿Instalarlo?' ;;
    prereq_no_systemd) echo "Sin systemd: inicia Docker con el servicio de tu sistema (p. ej. 'sudo service docker start')." ;;
    prereq_group) echo 'Tu usuario se agregó al grupo docker: desde tu próxima sesión no hará falta sudo.' ;;
    need_confirm_tty) echo 'se necesita confirmación y no hay terminal. Repite con OCAP_YES=1 para aceptar.' ;;
    help) cat <<'EOF'
Uso: ocap [comando]

  (sin comando)     Levanta OCAP y abre el panel. Si el código cambió, reconstruye.
  install           Configura (idioma, cuenta admin, IA, preferencias), instala lo que falte y el comando `ocap`.
  config            Cambia idioma, IA, acceso desde la red, puertos e inicio automático.
  start [--build]   Igual que sin comando; --build fuerza la reconstrucción.
  stop              Apaga OCAP (los datos se conservan).
  restart           stop + start.
  status            Estado de los servicios.
  logs [servicio]   Logs en vivo (p. ej. `ocap logs ocap-api`).
  autostart on|off  Inicia (o no) OCAP al encender el equipo.
  update            git pull + reconstruir + levantar.
  reset             Borra TODOS los datos y empieza de cero (pide confirmación).
  backup            Copia la base y los archivos subidos en backups/ (el .env se guarda aparte).
  restore <archivo> Reemplaza los datos por los de una copia (pide confirmación).
  uninstall         Quita el comando `ocap` y el inicio automático (no borra el repo ni los datos).
  help              Esta ayuda.
EOF
    ;;
  esac
}

_msg_en() {
  case "$1" in
    lang_prompt) printf '%s' 'Idioma / Language:\n  1) Español\n  2) English\nChoose [1]: ' ;;
    welcome) echo "Welcome to OCAP. I'll ask a few questions and then install everything without interrupting you." ;;
    section_org) echo 'Your organization' ;;
    ask_org) echo 'Name of your organization or business [%s]: ' ;;
    default_org) echo 'My company' ;;
    section_admin) echo 'Administrator account (you sign in to the panel with it)' ;;
    ask_admin_name) echo 'Your name [%s]: ' ;;
    default_admin_name) echo 'Administrator' ;;
    ask_email) echo 'Email: ' ;;
    bad_email) echo "That email doesn't look valid." ;;
    ask_password) echo 'Password (at least 12 characters, with letters and numbers): ' ;;
    ask_password_again) echo 'Repeat the password: ' ;;
    weak_password) echo 'The password needs at least 12 characters, with letters and numbers.' ;;
    quote_password) echo 'The password cannot contain single quotes (%s).' ;;
    password_mismatch) echo "The passwords don't match." ;;
    section_ai) echo 'Artificial intelligence (optional; you can set it up later in the panel)' ;;
    ask_ai) printf '%s' '  1) Gemini (Google)\n  2) OpenAI\n  3) Claude (Anthropic)\n  4) Not now (the assistant answers in demo mode)\nChoose [4]: ' ;;
    ask_ai_key) echo '%s API key (hidden): ' ;;
    empty_key) echo 'The key is empty; demo mode it is.' ;;
    section_prefs) echo 'Preferences' ;;
    ask_autostart) echo 'Start OCAP automatically when the computer turns on?' ;;
    ask_browser) echo 'Open the browser every time you start OCAP?' ;;
    ask_lan) echo 'Use the panel from other devices on your network too (phone, another PC)?' ;;
    lan_warning) echo '  Anyone on your network will see the sign-in page; use a strong password.' ;;
    port_changed) echo 'Port %s (%s) is busy on this computer; using %s instead.' ;;
    yes_no_default_yes) echo ' [Y/n] ' ;;
    yes_no_default_no) echo ' [y/N] ' ;;
    summary) echo 'Summary' ;;
    summary_lang) echo 'Language' ;;
    summary_org) echo 'Organization' ;;
    summary_admin) echo 'Administrator' ;;
    summary_ai) echo 'AI' ;;
    summary_demo) echo 'demo mode' ;;
    summary_autostart) echo 'Start at boot' ;;
    summary_browser) echo 'Open the browser' ;;
    summary_access) echo 'Access' ;;
    summary_local) echo 'this computer only' ;;
    summary_lan) echo 'this computer and your network' ;;
    summary_panel) echo 'Panel' ;;
    yes) echo 'yes' ;;
    no) echo 'no' ;;
    confirm_summary) echo 'All good? OCAP will be installed and built (several minutes).' ;;
    confirm_config) echo 'Save these changes?' ;;
    restart_wizard) echo "OK, let's start over." ;;
    already_configured) echo 'OCAP is already set up on this computer. To change language, AI or preferences: ocap config' ;;
    installing_from) echo 'Installing OCAP from %s' ;;
    building) echo 'Building the images (the first time takes a few minutes)…' ;;
    command_installed) echo 'Command installed: %s' ;;
    path_hint) echo '%s is not in your PATH. Add it and open a new terminal:' ;;
    install_done) echo 'Done. To start OCAP, type in any terminal: ocap' ;;
    start_now) echo 'Start OCAP now?' ;;
    config_saved) echo 'Settings saved.' ;;
    config_restart) echo 'To apply the changes: ocap restart' ;;
    docker_off) echo 'Docker is off; starting it…' ;;
    docker_ready) echo 'Docker is ready' ;;
    docker_permission) echo "Your user can't use Docker." ;;
    docker_group_ask) echo 'Add it to the docker group (asks for your password)?' ;;
    docker_timeout) echo "Docker didn't start in time. Start it manually and run: ocap" ;;
    docker_no_response) echo "Docker isn't responding. Start it and run: ocap" ;;
    missing_run_install) echo 'missing: %s. Install them with: ocap install' ;;
    starting) echo 'Starting OCAP…' ;;
    waiting) echo 'Waiting for %s ' ;;
    the_api) echo 'the API' ;;
    the_panel) echo 'the panel' ;;
    not_responding) echo "%s didn't respond in time. Check: ocap logs" ;;
    running) echo 'OCAP is running' ;;
    lan_url) echo 'From your network: %s' ;;
    first_setup) echo 'First time: create your account at %s' ;;
    setup_code) echo 'Setup code: %s' ;;
    admin_ready) echo 'Sign in with %s and the password you chose.' ;;
    password_removed) echo 'For security, the administrator password was removed from the .env file.' ;;
    hints) echo 'Stop: ocap stop    Status: ocap status    Help: ocap help' ;;
    port_busy) echo 'Port %s (%s) is used by another program. Change it with: ocap config' ;;
    stopping) echo 'Stopping OCAP (your data is kept)…' ;;
    already_stopped) echo 'Docker is off: OCAP is already stopped.' ;;
    status_docker_off) echo 'Docker is off: OCAP is not running. Start it with: ocap' ;;
    status_ok) echo 'OK' ;;
    status_down) echo 'not responding' ;;
    updating) echo 'Updating the code…' ;;
    reset_warning) echo 'This deletes ALL OCAP data: users, agents, conversations and WhatsApp sessions.' ;;
    reset_confirm) echo "Type 'delete' to confirm: " ;;
    reset_word) echo 'delete' ;;
    cancelled) echo 'Cancelled.' ;;
    backup_database) echo 'Database…' ;;
    backup_files) echo 'Uploaded files…' ;;
    backup_done) echo 'Backup ready: %s' ;;
    backup_env_warning) echo 'Also keep the .env somewhere safe: without VAULT_MASTER_KEY the API keys and tokens in the backup cannot be decrypted.' ;;
    restore_usage) echo 'Usage: ocap restore <.tar file from ocap backup>' ;;
    restore_invalid) echo 'That file is not an ocap backup.' ;;
    restore_warning) echo 'This replaces ALL current OCAP data with the backup. It needs the same .env the backup was made with.' ;;
    uninstalled) echo 'Command removed. The repo and your data are untouched (to delete data: ocap reset before uninstalling).' ;;
    not_installed) echo 'The ocap command was not installed in %s.' ;;
    unknown_command) echo 'Unknown command: %s' ;;
    autostart_on) echo 'OCAP will start automatically when the computer turns on.' ;;
    autostart_off) echo 'Autostart disabled.' ;;
    autostart_status_on) echo 'Autostart: on' ;;
    autostart_status_off) echo 'Autostart: off' ;;
    autostart_unsupported) echo 'This system has neither systemd nor launchd: set up autostart manually (run "%s start").' ;;
    autostart_desktop) echo 'Docker Desktop will also start when you log in.' ;;
    autostart_linger_fail) echo "Couldn't enable starting without logging in (loginctl enable-linger); OCAP will start when you log in." ;;
    need_tty) echo 'Interactive install needs a terminal. To install without questions: OCAP_YES=1 (optionally OCAP_ADMIN_EMAIL, OCAP_ADMIN_PASSWORD, OCAP_ORG_NAME).' ;;
    prereq_need) echo 'OCAP needs: %s.' ;;
    prereq_macos) echo 'It will be installed with Homebrew (Docker with Colima, no Docker Desktop).' ;;
    prereq_linux) echo "Docker Engine will be installed with your system's package manager (asks for your password)." ;;
    prereq_continue) echo 'Continue?' ;;
    prereq_cancelled) echo 'installation cancelled.' ;;
    prereq_done) echo 'Dependencies installed.' ;;
    prereq_failed) echo 'could not install: %s.' ;;
    prereq_unsupported) echo "missing: %s. This system can't be set up automatically; follow https://docs.docker.com/engine/install/ (on Windows: WSL2 + Docker Desktop)." ;;
    prereq_need_sudo) echo 'sudo (or running as root) is needed to install packages.' ;;
    prereq_brew) echo 'Homebrew (the macOS package manager) is missing. Install it?' ;;
    prereq_no_systemd) echo "No systemd: start Docker with your system's service (e.g. 'sudo service docker start')." ;;
    prereq_group) echo 'Your user was added to the docker group: from your next session sudo is not needed.' ;;
    need_confirm_tty) echo 'confirmation needed and there is no terminal. Run again with OCAP_YES=1 to accept.' ;;
    help) cat <<'EOF'
Usage: ocap [command]

  (no command)      Starts OCAP and opens the panel. Rebuilds if the code changed.
  install           Sets up (language, admin account, AI, preferences), installs what's missing and the `ocap` command.
  config            Changes language, AI, network access, ports and autostart.
  start [--build]   Same as no command; --build forces a rebuild.
  stop              Stops OCAP (your data is kept).
  restart           stop + start.
  status            Status of the services.
  logs [service]    Live logs (e.g. `ocap logs ocap-api`).
  autostart on|off  Starts (or not) OCAP when the computer turns on.
  update            git pull + rebuild + start.
  reset             Deletes ALL data and starts from scratch (asks for confirmation).
  backup            Copies the database and uploaded files into backups/ (keep the .env separately).
  restore <file>    Replaces the data with a backup (asks for confirmation).
  uninstall         Removes the `ocap` command and autostart (keeps the repo and your data).
  help              This help.
EOF
    ;;
  esac
}

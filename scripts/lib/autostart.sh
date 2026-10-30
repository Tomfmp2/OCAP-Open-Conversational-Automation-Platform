# shellcheck shell=bash
# Inicio automático de OCAP al encender el equipo: servicio de usuario de systemd (Linux) o LaunchAgent (macOS).
# Requiere ROOT (carpeta del repo) y las funciones de i18n.sh.

_systemd_unit() { echo "$HOME/.config/systemd/user/ocap.service"; }
_launch_agent() { echo "$HOME/Library/LaunchAgents/io.ocap.plist"; }

_has_user_systemd() { command -v systemctl >/dev/null 2>&1 && systemctl --user show-environment >/dev/null 2>&1; }

autostart_enable() {
  if [[ "$(uname -s)" == "Darwin" ]]; then
    mkdir -p "$(dirname "$(_launch_agent)")"
    cat > "$(_launch_agent)" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key><string>io.ocap</string>
  <key>ProgramArguments</key><array><string>$ROOT/ocap</string><string>start</string></array>
  <key>EnvironmentVariables</key><dict>
    <key>OCAP_NO_BROWSER</key><string>1</string>
    <key>PATH</key><string>/opt/homebrew/bin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin</string>
  </dict>
  <key>RunAtLoad</key><true/>
  <key>StandardOutPath</key><string>$ROOT/.ocap/autostart.log</string>
  <key>StandardErrorPath</key><string>$ROOT/.ocap/autostart.log</string>
</dict>
</plist>
PLIST
    launchctl unload "$(_launch_agent)" >/dev/null 2>&1 || true
    launchctl load -w "$(_launch_agent)"
    t autostart_on; echo
    return 0
  fi

  if ! _has_user_systemd; then
    t autostart_unsupported "$ROOT/ocap"; echo
    return 0
  fi

  mkdir -p "$(dirname "$(_systemd_unit)")"
  cat > "$(_systemd_unit)" <<UNIT
[Unit]
Description=OCAP (Open Conversational Automation Platform)
Wants=network-online.target
After=network-online.target docker-desktop.service

[Service]
Type=oneshot
RemainAfterExit=yes
Environment=OCAP_NO_BROWSER=1
ExecStart=$ROOT/ocap start
ExecStop=$ROOT/ocap stop
TimeoutStartSec=1200

[Install]
WantedBy=default.target
UNIT
  systemctl --user daemon-reload
  systemctl --user enable ocap.service >/dev/null 2>&1

  # Docker Desktop corre como servicio de usuario: se inicia al entrar a la sesión.
  if systemctl --user cat docker-desktop.service >/dev/null 2>&1; then
    systemctl --user enable docker-desktop.service >/dev/null 2>&1 || true
    t autostart_desktop; echo
  # Con Docker Engine, "linger" permite que los servicios de usuario arranquen con el equipo, sin iniciar sesión.
  elif ! loginctl enable-linger "$USER" >/dev/null 2>&1; then
    t autostart_linger_fail; echo
  fi
  t autostart_on; echo
}

autostart_disable() {
  if [[ "$(uname -s)" == "Darwin" ]]; then
    launchctl unload -w "$(_launch_agent)" >/dev/null 2>&1 || true
    rm -f "$(_launch_agent)"
  elif [[ -f "$(_systemd_unit)" ]]; then
    systemctl --user disable ocap.service >/dev/null 2>&1 || true
    rm -f "$(_systemd_unit)"
    systemctl --user daemon-reload >/dev/null 2>&1 || true
  fi
  t autostart_off; echo
}

autostart_is_enabled() {
  if [[ "$(uname -s)" == "Darwin" ]]; then [[ -f "$(_launch_agent)" ]]; else [[ -f "$(_systemd_unit)" ]]; fi
}

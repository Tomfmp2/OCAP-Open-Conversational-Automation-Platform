/*
 * Widget de WebChat de OCAP. Se incrusta en cualquier sitio con:
 *   <script src="https://TU-PANEL/ocap-webchat.js" data-ocap-key="CLAVE_PUBLICA" async></script>
 * Habla con /api/public/webchat del mismo servidor que sirve este archivo. Shadow DOM: no hereda ni pisa los
 * estilos del sitio. Todo el texto se inserta con textContent (nunca HTML).
 */
(function () {
  "use strict";

  var script = document.currentScript;
  if (!script || window.__ocapWebChat) return;
  window.__ocapWebChat = true;

  var key = script.getAttribute("data-ocap-key");
  if (!key) {
    console.warn("[OCAP] Falta data-ocap-key en el script del widget.");
    return;
  }

  var base = new URL(script.src).origin + "/api/public/webchat/" + encodeURIComponent(key);
  var POLL_MS = 4000;
  var reducedMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  // La conversación vive en memoria, no en localStorage: el id es la llave para leer el historial y no queda en el
  // navegador del visitante. Al recargar o cambiar de página empieza una conversación nueva.
  var currentSession = uuid();
  // UUID v4 con getRandomValues: crypto.randomUUID no existe en sitios servidos por http (contexto no seguro).
  function uuid() {
    var b = crypto.getRandomValues(new Uint8Array(16));
    b[6] = (b[6] & 0x0f) | 0x40;
    b[8] = (b[8] & 0x3f) | 0x80;
    var h = Array.prototype.map.call(b, function (x) { return (x + 0x100).toString(16).slice(1); }).join("");
    return h.slice(0, 8) + "-" + h.slice(8, 12) + "-" + h.slice(12, 16) + "-" + h.slice(16, 20) + "-" + h.slice(20);
  }

  function sessionId() {
    return currentSession;
  }

  var css =
    ":host{all:initial;font-family:system-ui,-apple-system,'Segoe UI',sans-serif;color:#0a0a0a}" +
    "*{box-sizing:border-box}" +
    ".launcher{position:fixed;right:20px;bottom:20px;width:56px;height:56px;border-radius:9999px;border:0;background:#0a0a0a;color:#fff;cursor:pointer;box-shadow:0 8px 24px rgba(0,0,0,.18);display:flex;align-items:center;justify-content:center;z-index:2147483646}" +
    ".launcher:focus-visible,button:focus-visible,textarea:focus-visible{outline:2px solid #2563eb;outline-offset:2px}" +
    ".panel{position:fixed;right:20px;bottom:88px;width:min(380px,calc(100vw - 32px));height:min(560px,calc(100vh - 120px));background:#fff;border:1px solid #e5e5e5;border-radius:16px;box-shadow:0 16px 48px rgba(0,0,0,.18);display:flex;flex-direction:column;overflow:hidden;z-index:2147483647;transition:opacity .15s,transform .15s}" +
    ".panel[hidden]{display:none}" +
    ".reduced .panel{transition:none}" +
    "header{display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border-bottom:1px solid #f0f0f0;font-weight:600;font-size:15px}" +
    "header button{border:0;background:none;font-size:20px;line-height:1;cursor:pointer;color:#525252;padding:4px 8px;border-radius:6px}" +
    ".log{flex:1;overflow-y:auto;padding:16px;display:flex;flex-direction:column;gap:8px;font-size:14px;line-height:1.45}" +
    ".msg{max-width:85%;padding:9px 12px;border-radius:14px;white-space:pre-wrap;word-wrap:break-word}" +
    ".visitor{align-self:flex-end;background:#0a0a0a;color:#fff;border-bottom-right-radius:4px}" +
    ".agent,.operator{align-self:flex-start;background:#f4f4f5;border-bottom-left-radius:4px}" +
    ".operator{border:1px solid #d4d4d8}" +
    ".who{display:block;font-size:12px;color:#52525b;margin-bottom:2px}" +
    ".status{font-size:12px;color:#52525b;padding:0 16px 6px;min-height:18px}" +
    "form{display:flex;gap:8px;padding:12px;border-top:1px solid #f0f0f0}" +
    "textarea{flex:1;resize:none;border:1px solid #d4d4d8;border-radius:10px;padding:9px 10px;font:inherit;font-size:14px;max-height:120px}" +
    "form button{border:0;border-radius:10px;background:#0a0a0a;color:#fff;padding:0 14px;font:inherit;font-size:14px;cursor:pointer}" +
    "form button:disabled{opacity:.5;cursor:default}";

  var host = document.createElement("div");
  host.setAttribute("data-ocap-webchat", "");
  var root = host.attachShadow({ mode: "open" });
  var style = document.createElement("style");
  style.textContent = css;
  root.appendChild(style);

  var wrapper = document.createElement("div");
  if (reducedMotion) wrapper.className = "reduced";
  root.appendChild(wrapper);

  var launcher = document.createElement("button");
  launcher.className = "launcher";
  launcher.type = "button";
  launcher.setAttribute("aria-label", "Abrir chat");
  launcher.setAttribute("aria-expanded", "false");
  launcher.textContent = "💬";

  var panel = document.createElement("div");
  panel.className = "panel";
  panel.hidden = true;
  panel.setAttribute("role", "dialog");
  panel.setAttribute("aria-label", "Chat");

  var header = document.createElement("header");
  var title = document.createElement("span");
  title.textContent = "Chat";
  var close = document.createElement("button");
  close.type = "button";
  close.setAttribute("aria-label", "Cerrar chat");
  close.textContent = "×";
  header.appendChild(title);
  header.appendChild(close);

  var log = document.createElement("div");
  log.className = "log";
  log.setAttribute("role", "log");
  log.setAttribute("aria-live", "polite");

  var status = document.createElement("div");
  status.className = "status";
  status.setAttribute("aria-live", "polite");

  var form = document.createElement("form");
  var input = document.createElement("textarea");
  input.rows = 1;
  input.maxLength = 2000;
  input.placeholder = "Escribe tu mensaje…";
  input.setAttribute("aria-label", "Mensaje");
  var send = document.createElement("button");
  send.type = "submit";
  send.textContent = "Enviar";
  form.appendChild(input);
  form.appendChild(send);

  panel.appendChild(header);
  panel.appendChild(log);
  panel.appendChild(status);
  panel.appendChild(form);
  wrapper.appendChild(panel);
  wrapper.appendChild(launcher);

  var seen = {};
  var lastAt = null;
  var pollTimer = null;
  var available = true;

  function render(from, text, id) {
    if (id) {
      if (seen[id]) return;
      seen[id] = true;
    }
    var bubble = document.createElement("div");
    bubble.className = "msg " + from;
    if (from === "operator") {
      var who = document.createElement("span");
      who.className = "who";
      who.textContent = "Equipo";
      bubble.appendChild(who);
    }
    bubble.appendChild(document.createTextNode(text));
    log.appendChild(bubble);
    log.scrollTop = log.scrollHeight;
  }

  function request(path, options) {
    return fetch(base + path, Object.assign({ headers: { "Content-Type": "application/json" } }, options)).then(function (r) {
      if (r.status === 403) throw new Error("Este sitio no está autorizado para usar el chat.");
      if (r.status === 429) throw new Error("Muchos mensajes seguidos. Espera un momento.");
      if (!r.ok) throw new Error("No pudimos conectar con el chat.");
      return r.json();
    });
  }

  function poll() {
    var query = lastAt ? "?after=" + encodeURIComponent(lastAt) : "";
    return request("/sessions/" + sessionId() + "/messages" + query)
      .then(function (messages) {
        messages.forEach(function (m) {
          render(m.from, m.text, m.id);
          lastAt = m.createdAt;
        });
      })
      .catch(function () {});
  }

  function setOpen(open) {
    panel.hidden = !open;
    launcher.setAttribute("aria-expanded", String(open));
    launcher.setAttribute("aria-label", open ? "Cerrar chat" : "Abrir chat");
    clearInterval(pollTimer);
    if (open) {
      poll();
      pollTimer = setInterval(poll, POLL_MS);
      input.focus();
    } else {
      launcher.focus();
    }
  }

  launcher.addEventListener("click", function () {
    setOpen(panel.hidden);
  });
  close.addEventListener("click", function () {
    setOpen(false);
  });
  panel.addEventListener("keydown", function (e) {
    if (e.key === "Escape") setOpen(false);
  });
  input.addEventListener("keydown", function (e) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      form.requestSubmit();
    }
  });

  form.addEventListener("submit", function (e) {
    e.preventDefault();
    var text = input.value.trim();
    if (!text || send.disabled || !available) return;
    input.value = "";
    send.disabled = true;
    status.textContent = "Escribiendo…";
    request("/messages", { method: "POST", body: JSON.stringify({ sessionId: sessionId(), message: text }) })
      .then(function () {
        // Visitante y respuesta llegan con su id desde el servidor: así no se duplican al consultar.
        return poll();
      })
      .catch(function (err) {
        render("agent", err.message);
      })
      .then(function () {
        status.textContent = "";
        send.disabled = false;
        input.focus();
      });
  });

  request("/config")
    .then(function (config) {
      title.textContent = config.title || "Chat";
      panel.setAttribute("aria-label", title.textContent);
      document.body.appendChild(host);
    })
    .catch(function (err) {
      available = false;
      console.warn("[OCAP] " + err.message);
    });
})();

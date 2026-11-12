# Canales

Un canal es por donde te escriben tus clientes. Una organización puede tener varios canales del mismo tipo (por
ejemplo, un bot de Telegram para ventas y otro para soporte). La excepción es WhatsApp por QR, que es una sola
instancia por organización.

| Canal | Qué necesitas | Guía |
| --- | --- | --- |
| WhatsApp (QR) | El teléfono del negocio | [WHATSAPP.md](../channels/WHATSAPP.md) |
| WhatsApp Business | Una app de Meta con la API de WhatsApp | [abajo](#whatsapp-business-api-oficial-de-meta) |
| Telegram | Un bot creado con @BotFather | [abajo](#telegram) |
| WebChat | Tu sitio web | [abajo](#webchat) |

## Qué decide cada canal

En **Canales → Configurar**:

- **Quién responde**: el agente (sus instrucciones y su memoria) y el modelo de IA. Si el modelo asignado se
  desactiva o se borra, el canal usa el predeterminado de la organización y no se queda mudo.
- **Qué usa el agente en este canal**:
  - **Conocimiento**: las colecciones que consulta. Sin marcar ninguna, usa todas.
  - **Acciones**: crear un evento en Google Calendar o anotar una fila en Google Sheets. Solo se ofrecen acciones
    seguras frente a clientes; el correo del negocio nunca se concede a un canal.

  Lo que el canal no concede no se ejecuta, aunque el agente lo tuviera configurado antes. Un canal que todavía no
  guardó esta sección usa lo que tenía el agente.
- **Estado**: encendido, apagándose o apagado (ver [Apagar un canal](#apagar-un-canal)).

## WhatsApp Business (API oficial de Meta)

1. En Meta for Developers, crea una app con el producto WhatsApp y anota el **Phone number ID**, un **token de
   acceso** y el **App Secret** (Configuración de la app → Básica).
2. En OCAP: **Conectar canal → WhatsApp Business**, completa esos datos y conecta.
3. OCAP muestra la **URL de devolución de llamada** y un **token de verificación**. Pégalos en Meta (WhatsApp →
   Configuración → Webhook) y suscríbete a `messages`. El token se muestra una sola vez.

El App Secret se guarda cifrado en la conexión y sirve para verificar la firma de cada mensaje; del token de
verificación solo se guarda su hash. No hace falta tocar el `.env`. Si el servidor define `WhatsAppCloud__AppSecret`,
también se acepta.

API: `POST /api/channels/whatsapp/connect` con `{ displayName, phoneNumberId, apiToken, appSecret }`.

## Telegram

1. Crea un bot con [@BotFather](https://t.me/BotFather) y copia su token.
2. **Conectar canal → Telegram**, ponle un nombre y pega el token.

Por defecto OCAP consulta los mensajes del bot (polling). La posición se guarda en la base, así que tras un
reinicio no se pierden ni se repiten mensajes. Si el bot tenía un webhook de otra herramienta, OCAP lo quita sin
descartar los mensajes pendientes. Un mismo bot no puede conectarse dos veces, ni en otra organización.

Cuando Telegram rechaza una respuesta, el log de `ocap-api` muestra el motivo (bot bloqueado, chat no encontrado).

## WebChat

1. **Conectar canal → WebChat** y ponle un nombre (también es el título del chat).
2. En **Configurar → Chat en tu sitio web**, autoriza los dominios de tu web y copia el `<script>`; pégalo antes de
   `</body>`.

Con dominios autorizados, solo ellos pueden usar el widget; sin ninguno, funciona en cualquier sitio. Si alguien copia el código, **genera una clave nueva** y la
anterior deja de funcionar. El widget no guarda nada en el navegador del visitante.

## Conversaciones y personas del equipo

- **Conversaciones** muestra los chats en vivo, con lo que respondió el agente.
- **Tomar conversación** detiene al agente y te deja responder por el mismo canal. **Devolver al agente** lo
  reanuda.
- Si nadie del equipo escribe en **15 minutos**, el agente retoma la conversación solo, para que el bot no quede
  mudo si alguien se olvida de devolverla.
- Si el cliente pide una persona, el agente transfiere la conversación por sí solo.
- **Memoria** muestra lo que los agentes recuerdan del contacto ([MEMORY.md](MEMORY.md)).

## Apagar un canal

**Configurar → Estado → Apagar**:

1. El canal deja de aceptar chats nuevos.
2. Los chats en curso siguen atendiéndose hasta que pasan **5 minutos sin mensajes**.
3. Entonces el canal se apaga del todo y no responde hasta que pulses **Encender**.

Encender también cancela un apagado en curso. En Telegram, los mensajes que llegaron mientras el canal estaba
apagado se descartan al encenderlo, para no responderlos horas tarde.

API: `POST /api/channels/{id}/shutdown` (apagado gradual), `POST /api/channels/{id}/enable` y
`POST /api/channels/{id}/disable` (inmediato).

## Garantías

- Cada mensaje se responde una sola vez aunque el proveedor lo reenvíe: webhooks y polling se deduplican en la base.
- Los webhooks se verifican antes de procesar nada: firma HMAC por instancia (Evolution), secreto por conexión
  (Telegram) y firma de Meta con el App Secret del número (WhatsApp Business).

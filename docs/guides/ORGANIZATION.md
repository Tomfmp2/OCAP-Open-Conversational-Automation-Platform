# Organización

Todo lo de esta página se configura en el panel (**Organización**) y se guarda en la base. Los secretos se guardan
cifrados en el vault de la organización y nunca vuelven al navegador. No hace falta editar el `.env`.

## Nombre

El nombre se ve en el panel y en las invitaciones.

## Preferencias

| Ajuste | Qué hace |
| --- | --- |
| Idioma del asistente | Español o inglés; vacío usa el del servidor |
| Zona horaria | El asistente la usa para entender «mañana a las 3» y crear eventos a la hora correcta |
| Dirección del panel | Va en los enlaces de invitación y de recuperación de contraseña. Debe ser `https://`. Cambiarla exige el permiso de seguridad |
| Guardar conversaciones con clientes | Siempre, 30 o 90 días, 6 meses o 1 año; un job diario borra los mensajes más antiguos |
| Los agentes recuerdan a cada persona | Plazo de la memoria ([MEMORY.md](MEMORY.md)) |
| Aviso de tratamiento de datos | Texto que reciben los contactos nuevos con la primera respuesta |

## Correo saliente

Sirve para enviar invitaciones al equipo y enlaces de «Olvidé mi contraseña». Sin correo configurado, OCAP funciona
igual y los enlaces se copian a mano.

- Servidor SMTP, puerto (25, 465, 587 o 2525), usuario, contraseña (cifrada), remitente y TLS.
- **Enviar correo de prueba** lo manda a tu dirección.
- El servidor debe ser público: el panel no sirve para alcanzar servicios de la red interna. Para un relé interno, que el panel no
  admite por seguridad, define `Email__Smtp__*` en un `docker-compose.override.yml`.
- Gestionarlo exige el permiso de seguridad, porque por ese correo pasan los enlaces de recuperación de contraseña.

## Google Workspace

Permite que el asistente envíe correos con Gmail y cree eventos en Calendar, y que los agentes de canal usen las
acciones de Calendar y Sheets que cada canal conceda.

1. En Google Cloud Console, crea un cliente OAuth de tipo «Aplicación web» y registra la **URI de redirección** que
   muestra OCAP (`https://<tu-panel>/integrations/google/callback`).
2. En OCAP, pega el **Client ID** y el **Client secret** (se guarda cifrado).
3. Pulsa **Conectar con Google** y autoriza la cuenta.

La conexión usa un parámetro `state` firmado que caduca a los 10 minutos y está atado al usuario que la inició, para
que un código de otra cuenta no se pueda colar (CSRF). Los tokens de Google se guardan cifrados y se renuevan solos.
Sin cliente en el panel, Google Workspace queda sin conectar.

## Equipo

En **Equipo** invitas personas por correo, o copias el enlace si no hay correo configurado, y eliges su rol. Cada
invitado define su contraseña con un enlace de un solo uso.

## Sistema

**Sistema** reúne los ajustes de toda la instalación: límites de uso (peticiones, intentos de inicio de sesión y WebChat
por minuto), cada cuánto se comprueba la sesión y qué dominios pueden llamar a la API desde un navegador. Se aplican al
guardar, sin reiniciar. Un campo vacío usa el valor del servidor.

Solo la ve la cuenta que instaló OCAP. El permiso va en esa cuenta y no en el rol: invitar a otro administrador no se lo
da, y esa cuenta no se puede desactivar ni pasar a operador. Todavía no se puede traspasar a otra persona desde el
panel.

Los dominios definidos en el servidor (`PUBLIC_PANEL_URL`) valen siempre, aunque se borren los del panel.

## Permisos

| Acción | Permiso |
| --- | --- |
| Nombre, idioma, zona horaria, retención, memoria y aviso | `Settings.Manage` |
| Dirección del panel y correo saliente | `Security.Manage` |
| Google Workspace | `OAuth.Manage` |
| Equipo | `Security.Manage` |
| Sistema | `System.Manage`, solo la cuenta que instaló |

Los cambios quedan en el registro de auditoría.

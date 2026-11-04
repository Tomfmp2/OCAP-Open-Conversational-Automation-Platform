# Memoria de los agentes

Los agentes recuerdan a cada persona entre conversaciones: su nombre, su ciudad, sus preferencias o lo que pidió la
última vez. El asistente del panel recuerda igual a cada usuario del equipo.

## Cómo funciona

| Capa | Qué guarda | Cuándo |
| --- | --- | --- |
| Resumen de la conversación | Lo antiguo de una conversación larga, en pocas líneas | Cuando pasa de 12 mensajes |
| Datos de la persona | Hechos con clave: `ciudad`, `alergias`, `pedido_habitual`… | Tras 10 minutos sin mensajes |
| Conversaciones anteriores | Una o dos frases de lo que pasó en cada una | Tras 10 minutos sin mensajes |

Todo ocurre en segundo plano y nunca mientras un cliente espera respuesta. Si el modelo falla o se queda sin cuota, la
memoria de esa organización se pausa (de 5 minutos a 1 hora, o hasta que el proveedor indique) y lo pendiente sigue en cola. Extraer la memoria cuesta una llamada al
modelo por conversación terminada, no una por mensaje. Un saludo suelto no genera ninguna.

Al responder, el agente recibe:
- los últimos 12 mensajes;
- el resumen de lo anterior;
- los 15 datos más recientes de la persona;
- un resumen breve de sus 3 últimas conversaciones.

Todo eso llega marcado como datos, no como instrucciones: lo escribió un cliente o un resumen automático, así que
nunca puede dar órdenes al agente ni autorizar acciones.

**Datos que cambian**: si el cliente da un dato nuevo con la misma clave («me mudé a Cali»), el anterior queda
marcado como reemplazado, no borrado. Si repite uno, se renueva su vigencia.

## Plazo

En **Organización → Preferencias**, «Los agentes recuerdan a cada persona»:

- **90 días** por defecto, o 30, 180 o 365, contados desde la **última vez** que la persona mencionó el dato.
- **No recordar** desactiva la memoria. Lo ya guardado se ignora y caduca solo.

Un job diario borra lo vencido. Cada agente tiene además el interruptor **Recordar a los clientes**.

## Ver, corregir y borrar

- **Conversaciones → Memoria**: lo que se recuerda del contacto, con su fecha de vencimiento. Se puede corregir un
  dato, borrar uno o **borrar todo**.
- **Asistente → Conversaciones → Lo que recuerdo de ti**: tu propia memoria en el asistente.

## Privacidad

- **«Olvídame»**: si un cliente escribe «olvídame», «borra mis datos» o «forget me» en un mensaje corto, se borra lo
  que se recuerda de él, incluido el resumen de la conversación, sin pasar por el modelo. Lo dicho antes no vuelve a
  extraerse.
- **Datos sensibles**: números de tarjeta, contraseñas, PIN, CVV y documentos de identidad no se guardan aunque el
  modelo los proponga.
- **Adjuntos**: lo que llega en archivos adjuntos no se usa para la memoria.
- **Aviso de tratamiento de datos** (opcional): en Preferencias puedes escribir un texto que reciben los contactos
  nuevos con la primera respuesta. Por ejemplo, para cumplir la Ley 1581 de Colombia o el RGPD.
- **Aislamiento**: los recuerdos están aislados por organización (*row-level security*) y por persona.

## API

| Método | Ruta | Uso |
| --- | --- | --- |
| `GET` | `/api/inbox/{conversationId}/memory` | Memoria del contacto de una conversación |
| `PUT` | `/api/inbox/{conversationId}/memory/{id}` | Corregir un recuerdo `{ text }` |
| `DELETE` | `/api/inbox/{conversationId}/memory/{id?}` | Borrar uno o todos |
| `GET` / `DELETE` | `/api/agents/principal/memory/{id?}` | Memoria propia en el asistente |
| `PUT` | `/api/settings/preferences` | `memoryDays` (`0` desactiva) y `privacyNotice` |

El diseño está en [ADR-010](../adr/ADR-010-agent-memory.md).

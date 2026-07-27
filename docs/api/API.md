# OCAP API v0.4.0 (API Gateway Foundation)

## Descripción

El API Gateway es el punto de entrada principal para la plataforma OCAP (Open Conversational Automation Platform). Expone los servicios de la capa de aplicación hacia el exterior siguiendo los principios de Clean Architecture y REST. No contiene lógica de negocio; su única responsabilidad es orquestar las peticiones HTTP y devolver respuestas estandarizadas.

## Endpoints

### 1. Health Check
Verifica que el servicio esté en ejecución.

- **URL:** `/api/health`
- **Método:** `GET`
- **Respuesta Exitosa:** `200 OK`
  ```json
  {
      "status": "Healthy",
      "timestamp": "2023-10-25T14:48:00Z"
  }
  ```

### 2. Recibir Mensaje
Punto de entrada para los mensajes de los distintos canales (WhatsApp, Telegram, web, etc.).

- **URL:** `/api/messages`
- **Método:** `POST`
- **Cuerpo de la Petición:**
  ```json
  {
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "messageContent": "Hola, necesito ayuda",
      "provider": "WhatsApp"
  }
  ```
- **Respuesta Exitosa:** `200 OK`
  ```json
  {
      "success": true,
      "message": "Mensaje procesado con éxito",
      "data": null
  }
  ```

### 3. Obtener Historial de Conversación
Permite obtener los detalles de una conversación activa o pasada.

- **URL:** `/api/conversations/{id}`
- **Método:** `GET`
- **Respuesta Exitosa:** `200 OK`
  ```json
  {
      "success": true,
      "message": "Conversación obtenida con éxito",
      "data": {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "userId": "123e4567-e89b-12d3-a456-426614174000",
          "status": 0,
          "messages": [
              {
                  "content": "Hola, necesito ayuda",
                  "sender": 1,
                  "timestamp": "2023-10-25T14:48:00Z"
              }
          ]
      }
  }
  ```

## Estructura de Respuesta

Todas las respuestas (excepto Health Check) siguen un formato envoltorio estandarizado:

```json
{
    "success": true|false,
    "message": "Mensaje descriptivo",
    "data": { ... } // Payload opcional
}
```

## Manejo de Errores

Si se produce una excepción no controlada, el middleware global devolverá un código de estado HTTP 500:

```json
{
    "success": false,
    "message": "Se ha producido un error interno en el servidor.",
    "error": "Detalle de la excepción"
}
```

## Referencias

- OCAP.Application
- OCAP.Infrastructure
- Swagger / OpenAPI (disponible en entorno de desarrollo en `/swagger`)

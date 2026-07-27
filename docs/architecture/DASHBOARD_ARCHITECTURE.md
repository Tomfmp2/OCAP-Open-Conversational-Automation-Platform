# OCAP — Decisión de Arquitectura del Dashboard

## Decisión Técnica: Blazor WebAssembly vs Blazor Server

Para el desarrollo del panel de administración de OCAP se evaluaron dos tecnologías principales dentro del ecosistema Microsoft:

1. **Blazor Server**: Mantendría conexiones WebSocket activas (SignalR) para renderizar el UI desde el servidor.
2. **Blazor WebAssembly (Elegida)**: Compila la aplicación como una Single Page Application (SPA) estática en WebAssembly consumiendo APIs REST puras.

---

## Justificación de la Elección de Blazor WebAssembly

### 1. Garantía de Arquitectura Hexagonal y Desacoplamiento
Al ejecutarse el código Blazor 100% en el navegador del cliente:
- Es **físicamente imposible** acceder directamente a PostgreSQL, EF Core o librerías de infraestructura backend.
- Toda la comunicación debe transitar estrictamente por la **API Gateway (`OCAP.Api`)** mediante JSON/HTTP.

### 2. Autohospedaje (*Self-Hosted*) de Bajo Consumo
- El Dashboard compilado se sirve como archivos estáticos a través de **Nginx** o CDN.
- Cero estado de UI almacenado en memoria del servidor backend, garantizando bajísimo consumo de recursos de infraestructura.

### 3. Distribución Open Source Escalable
- Permite desplegar la API y el Dashboard en contenedores Docker totalmente independientes (`ocap-api` y `ocap-dashboard`).
- Permite colocar balanceadores de carga y proxy inversos Nginx sin requerir *sticky sessions* de WebSockets.

---

## Diagrama de Comunicación

```
[ Navegador del Usuario ]
         │
    Blazor WebAssembly SPA (Nginx port 8081)
         │
         │ (HTTP REST / JSON)
         ▼
    [ Nginx Reverse Proxy / API Gateway ] (port 80)
         │
         ▼
    [ OCAP.Api Controllers ]
         │
         ▼
    [ OCAP Application & Domain Layers ]
```

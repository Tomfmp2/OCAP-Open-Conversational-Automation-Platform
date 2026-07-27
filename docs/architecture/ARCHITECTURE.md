# ARCHITECTURE

## Arquitectura Hexagonal y Modular Monolith
La arquitectura de OCAP ha sido diseñada para perdurar y escalar. Para lograrlo, combinamos **Arquitectura Hexagonal (Ports & Adapters)** con un enfoque de **Modular Monolith**.

### Separación del Dominio
En OCAP, el `Core` es sagrado. El dominio no tiene dependencias hacia ningún framework de persistencia (como Entity Framework), infraestructura (como Docker o PostgreSQL), ni SDKs externos (como WhatsApp o OpenAI). El dominio define las reglas puras del negocio conversacional, gestiona el estado de las conversaciones y orquesta la intención del usuario. Todo se comunica a través de **Puertos** (Interfaces).

### Modular Monolith
En lugar de fragmentar prematuramente la plataforma en microservicios, OCAP se organiza por **capacidades de negocio**.
Los módulos (ej. `Modules.Calendar`, `Modules.Email`, `Modules.Tasks`) contienen sus propios casos de uso y lógica específica, interactuando con el Core. Esta estructura permite un mantenimiento claro, límites arquitectónicos definidos (Bounded Contexts) y facilita la extracción a microservicios en el futuro si la escala lo demanda.

### Canales (Channels)
Los canales son adaptadores primarios (o *Driving Adapters*). Escuchan eventos del mundo exterior y los traducen al lenguaje del dominio.
- `WhatsApp`, `Telegram`, `Slack`, `Discord`.
El `Core` nunca sabe de dónde proviene un mensaje; solo entiende entidades genéricas como `Conversation`, `Message` o `Event`.

### Proveedores (Providers)
Los proveedores son adaptadores secundarios (o *Driven Adapters*). Son las implementaciones concretas de los puertos que el dominio necesita para interactuar con el exterior.
- **IA:** `OpenAI`, `Gemini`, `Claude`, `Ollama`.
- **Almacenamiento:** `Local`, `S3`, `GoogleDrive`.
- **Servicios Externos:** `Google`, `Microsoft365`.
Cualquier proveedor puede ser reemplazado o agregado sin que el dominio deba sufrir modificación alguna.

### Dashboard Web Administrativo
El Dashboard es un cliente más de la plataforma. Nunca interactúa directamente con la base de datos PostgreSQL. Todo su tráfico fluye a través de la API, quien a su vez delega en el Application Layer y este en el Domain. Esto garantiza que las reglas de negocio, la auditoría y la seguridad sean centralizadas.
Sirve como:
- Sistema de administración.
- Monitor en tiempo real.
- Configurador de instancias y canales.

### Deployment Manager
El Deployment Manager no es un instalador tradicional, es el orquestador de despliegue. Diseñado como un componente independiente, asiste a usuarios técnicos y no técnicos en el despliegue de su instancia de OCAP. Su responsabilidad abarca:
- Validar requisitos del sistema.
- Generar archivos de configuración (Docker, variables de entorno).
- Ejecutar migraciones iniciales.
- Validar el funcionamiento end-to-end antes del paso a producción.

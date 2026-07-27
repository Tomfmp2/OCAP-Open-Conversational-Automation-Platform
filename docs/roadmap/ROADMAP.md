# ROADMAP OCAP

Este es el plan de evolución trazado para el desarrollo y consolidación de la Open Conversational Automation Platform.

## Fase 1: Arquitectura Base
- [x] Definición de la estructura del proyecto.
- [x] Establecimiento de la Arquitectura Hexagonal y Modular Monolith.
- [x] Creación de proyectos .NET e interconexión de referencias.
- [x] Documentación base (Visión, Arquitectura, Principios).
- [ ] Configuración inicial de pipelines CI/CD y plantillas de GitHub.

## Fase 2: Core Conversacional
- Diseño e implementación de las entidades del Domain (Conversation, Message, User, Session).
- Definición de los Puertos (Ports) de entrada y salida.
- Implementación de los flujos principales (Application Use Cases) independientes de infraestructura.

## Fase 3: Canales Iniciales
- Desarrollo del adaptador de canal para Telegram.
- Desarrollo del adaptador de canal para WhatsApp (vía Evolution API o Meta Cloud API).
- Pruebas End-to-End validando la independencia del canal respecto al dominio.

## Fase 4: Dashboard
- Diseño de la API administrativa.
- Desarrollo de la aplicación frontend (Dashboard Web) para monitorización en tiempo real.
- Gestión básica de configuraciones y revisión de conversaciones.

## Fase 5: Deployment Manager
- Creación de la herramienta CLI para validación de entornos.
- Automatización de la generación de configuraciones (`docker-compose.yml`, `.env`).
- Automatización del seed de la base de datos y migraciones de PostgreSQL.

## Fase 6: Sistema de Plugins
- Refinamiento de la arquitectura Modular Monolith para permitir la carga dinámica de DLLs / módulos en tiempo de ejecución.
- Marketplace comunitario y documentación para terceros.

## Fase 7: Escalabilidad Empresarial
- Implementación de caché distribuida (Redis).
- Colas de mensajería (RabbitMQ / Kafka) para procesamiento asíncrono y masivo.
- Preparación para arquitecturas de alta disponibilidad (High Availability) y orquestación con Kubernetes.

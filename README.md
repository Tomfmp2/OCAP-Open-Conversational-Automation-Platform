# OCAP (Open Conversational Automation Platform)

## ¿Qué es OCAP?
OCAP es una plataforma Open Source de automatización conversacional. Su objetivo es permitir a usuarios y empresas crear sus propias instancias de asistentes inteligentes que operen de forma autónoma a través de múltiples canales. A diferencia de las soluciones cerradas (SaaS) habituales, OCAP es un sistema **Self-Hosted**. No dependes de un proveedor centralizado ni pagas tarifas por plataforma: eres el único propietario de tus interacciones, tu base de datos y tus reglas de negocio.

## El Problema que Resuelve
En la actualidad, las plataformas de automatización y bots limitan al usuario a ecosistemas propietarios, bloquean integraciones avanzadas y comprometen la privacidad corporativa. OCAP resuelve este problema devolviendo el control total a las empresas y desarrolladores. Puedes integrar tus propios modelos de IA (OpenAI, Gemini, Claude, locales), usar tus propios canales y manejar tu información con privacidad absoluta.

## Visión del Proyecto
Nuestra visión es establecer el estándar abierto para asistentes conversacionales corporativos y personales, creando una base estable donde una comunidad próspera construya y comparta módulos. Queremos que montar un asistente avanzado sea tan accesible, seguro y abierto como montar un WordPress.

## Características Principales
- **Canales Múltiples:** Arquitectura agnóstica de canal (preparada para WhatsApp, Telegram, Slack, Web, etc.).
- **Proveedores Flexibles:** Conecta fácilmente cualquier LLM, sistemas de almacenamiento o suites ofimáticas.
- **Privacidad y Propiedad:** Self-hosted by design. Tus tokens, tus datos, tus servidores.
- **Diseñado para Escalar:** Arquitectura en Modular Monolith, ideal para iniciar ligero y escalar a nivel empresarial.

## Arquitectura General
OCAP está estructurado bajo los principios de **Arquitectura Hexagonal (Ports & Adapters)** y **Modular Monolith**:
1. **Core (Domain & Application):** Corazón del negocio, 100% aislado de tecnología y frameworks externos.
2. **Modules:** Capacidades de negocio empaquetadas (Conversaciones, Calendarios, Correos).
3. **Channels & Providers:** Adaptadores tecnológicos reemplazables sin afectar el dominio central.
4. **Dashboard & API:** Las interfaces y puntos de entrada para la administración y operación.

## Filosofía Open Source
OCAP está construido "Open Source First". La arquitectura promueve la extensibilidad, permitiendo a cualquier desarrollador del mundo escribir adaptadores (Channels o Providers) sin tener que compilar ni entender todo el núcleo del sistema.

## Cómo crecerá la plataforma
La evolución está planificada en fases sucesivas: empezando por consolidar una base arquitectónica robusta, siguiendo con la implementación core conversacional y los adaptadores principales, hasta llegar a un completo sistema de plugins con escalabilidad empresarial e instaladores amigables (Deployment Manager).

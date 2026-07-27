# OCAP — Adaptador de Proveedor Ollama (Self-Hosted)

## Visión General
El adaptador `OllamaAiProvider` permite a OCAP ejecutar modelos de Inteligencia Artificial locales y self-hosted sin depender de la nube pública.

## Despliegue & Conexión
- **Localhost**: `http://localhost:11434`
- **Docker**: `http://ollama:11434`
- **Servidor Remoto**: Configurable mediante `BaseUrl`.
- **Model Discovery**: Consulta dinámica de modelos instalados a través del endpoint `/api/tags`.
- **Modelos Populares**: `llama3`, `mistral`, `phi3`, `codellama`.

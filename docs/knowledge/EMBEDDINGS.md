# Proveedores de Embeddings en OCAP

## Arquitectura Agnóstica
OCAP provee la abstracción `IEmbeddingProvider` e `IEmbeddingGenerator`, permitiendo intercambiar de proveedor mediante configuración sin cambiar código.

## Proveedores Soportados
- **OpenAI**: Modelos `text-embedding-3-small` (1536 dims) y `text-embedding-3-large` (3072 dims).
- **Google Gemini**: Modelo `text-embedding-004` (768 dims).
- **Ollama Self-Hosted**: Modelos locales como `nomic-embed-text` o `mxbai-embed-large`.

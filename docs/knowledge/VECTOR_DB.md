# Bases de Datos Vectoriales en OCAP

## Motores Soportados (`IVectorDatabase`)
- **PostgreSQL pgvector**: Integración nativa con PostgreSQL relacional mediante índices HNSW / IVFFlat.
- **Qdrant**: Base de datos vectorial de ultra alto rendimiento en Rust.
- **ChromaDB**: Almacenamiento vectorial ligero y de rápida respuesta.
- **Pinecone Cloud**: Servicio serverless administrado en la nube.

La selección se realiza dinámicamente mediante la configuración del sistema.

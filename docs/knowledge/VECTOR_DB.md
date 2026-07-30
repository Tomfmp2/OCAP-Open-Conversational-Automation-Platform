# Bases de Datos Vectoriales en OCAP

## Motores Soportados (`IVectorDatabase`)
- **PostgreSQL + PgVector**: implementación persistente integrada con PostgreSQL.
- **InMemory**: implementación no persistente para pruebas y entornos aislados.

Actualmente son las únicas implementaciones disponibles. Qdrant, ChromaDB y
Pinecone no están implementados en este repositorio.

La selección se configura mediante `Knowledge:UseInMemory` y
`Knowledge:VectorStore`.

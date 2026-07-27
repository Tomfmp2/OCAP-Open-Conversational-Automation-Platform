# Estrategias de Búsqueda y Recuperación

## Algoritmos Soportados (`IKnowledgeRetriever`)
1. **Similarity Search**: Distancia coseno sobre embeddings vectoriales.
2. **Keyword Search**: Búsqueda léxica BM25 / coincidencia de tokens exactos.
3. **Semantic Search**: Recuperación orientada a intención semántica.
4. **Hybrid Search (RRF)**: Combinación ponderada de búsqueda vectorial (0.7) y palabra clave (0.3) utilizando Reciprocal Rank Fusion.

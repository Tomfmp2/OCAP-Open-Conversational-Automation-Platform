# Retrieval-Augmented Generation (RAG) en OCAP

## Ciclo de Ingesta y Generación
1. **Ingesta de Documentos**: Subida de archivos multi-formato (PDF, DOCX, TXT, MD, CSV, JSON, HTML, XML).
2. **Parsing & Hash SHA256**: Extracción de texto, tablas, metadatos y cálculo de firma digital SHA256.
3. **Chunking Configurable**: Fragmentación del contenido según la estrategia seleccionada (Sentence, Paragraph, Semantic, Sliding Window).
4. **Generación de Embeddings**: Mapeo a vectores de alta dimensión mediante OpenAI, Gemini u Ollama.
5. **Upsert Vectorial**: Almacenamiento indexado en la base vectorial con etiqueta `TenantId`.
6. **Recuperación & Prompting**: Búsqueda RAG (Hybrid RRF / Cosine Similarity) e inyección directa en los prompts de los agentes y nodos de workflow con citas explícitas.

# Estrategias de Chunking en OCAP

## Estrategias Soportadas (`IChunker`)
- **Sentence Chunking**: División basada en límites de oraciones (`.!?`).
- **Paragraph Chunking**: División basada en párrafos y saltos de línea dobles.
- **Semantic Chunking**: División por encabezados `#` y secciones lógicas.
- **Sliding Window Chunking**: Ventana deslizante de caracteres con traslape configurable (`overlap`).

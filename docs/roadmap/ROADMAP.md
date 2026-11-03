# Roadmap de OCAP

## 1.0.0 (2026-11-03): primer lanzamiento público

Canales (WhatsApp por QR y Business, Telegram, WebChat), agentes con memoria, capacidades por canal, bandeja con
traspaso a personas, conocimiento con PDF, Word y Excel, modelos de IA de varios proveedores y configuración de la
organización desde el panel. El detalle está en [STATUS.md](../STATUS.md) y en el [CHANGELOG](../../CHANGELOG.md).

## Siguiente: reforma del conocimiento (1.1)

El objetivo es que el agente encuentre la información correcta con más precisión, responda datos exactos de tablas y
cite sus fuentes. Ordenado por impacto:

1. **Fundamentos y búsqueda híbrida real**
   - Guardar el archivo original (cifrado, con hash) para reprocesar y reindexar solo lo que cambió.
   - `tsvector` en español con GIN y pgvector con HNSW, fusionados con Reciprocal Rank Fusion (k≈60) en una sola
     consulta, filtrada por las colecciones del canal.
2. **Fragmentos con contexto** (Contextual Retrieval)
   - Una o dos frases generadas al indexar (documento, sección y de qué trata) antes de calcular el embedding y el
     índice por palabras.
   - En tablas, el contexto es determinista: hoja y encabezados.
3. **Datos estructurados**
   - Excel, CSV y Google Sheets importados como tablas tipadas.
   - Herramienta `consultar_datos` con un lenguaje JSON acotado (filtrar, ordenar, contar, sumar, promediar),
     validado y ejecutado en el servidor, sin SQL generado por el modelo.
4. **Búsqueda agéntica con citas**
   - `buscar_conocimiento` y `consultar_datos` como herramientas del agente, hasta 3 pasos por mensaje.
   - Cada respuesta guarda sus fuentes y se ven en la bandeja.
   - Si algo no está en las fuentes, el agente lo dice.
5. **Reordenamiento** de unos 30 candidatos a 5 (con el modelo de la organización, o Cohere/Jina si se configuran).
6. **Fuentes que se mantienen al día**: sincronización con Google Sheets y Drive, y revisión periódica de páginas
   web importadas.
7. **Calidad medible**
   - Lista de «preguntas sin respuesta» para convertirlas en respuestas escritas.
   - Conjunto de prueba por organización (Recall@5, MRR y fidelidad de las respuestas).
8. **OCR** de PDF escaneados con un modelo multimodal (opcional).

## Más adelante

- Interfaz para el motor de workflows.
- Canales: Slack, Microsoft Teams, Discord, Instagram.
- Correo saliente con conexión fijada a la IP validada.
- Medición del consumo de tokens por organización y por canal.

## Hitos internos previos

Antes de 1.0.0, el código base pasó por hitos internos (CAP-01 a CAP-20 y versiones 1.0.0–2.1.0 de julio de 2026):
núcleo hexagonal, persistencia, identidad empresarial (OIDC, MFA, SAML, SCIM/LDAP), bus de eventos distribuido,
motor de workflows y RAG con pgvector. No se publicaron como producto; el historial está en
[VERSIONING.md](../../VERSIONING.md).

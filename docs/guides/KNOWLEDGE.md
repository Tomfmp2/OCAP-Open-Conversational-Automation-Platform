# Conocimiento

El conocimiento son los documentos con los que responden los agentes: precios, horarios, políticas, catálogos y
preguntas frecuentes. Se organiza en **colecciones**, y cada canal elige cuáles consulta su agente
([CHANNELS.md](CHANNELS.md#qué-decide-cada-canal)).

> Indexar necesita un modelo de IA con embeddings (Gemini u OpenAI) configurado en **Modelos de IA**.

## Agregar información

| Cómo | Para qué |
| --- | --- |
| **Arrastrar o subir archivos** | PDF, Word (`.docx`), Excel (`.xlsx`), CSV, Markdown, JSON, HTML o texto, hasta 10 MB |
| **Escribir respuesta** | Una pregunta frecuente con su respuesta, o una nota que deban saber los agentes |
| **Importar página** | Descarga una página web pública una vez (HTML, texto o PDF). Si cambia, impórtala de nuevo |

Los formatos antiguos `.xls` y `.doc` se rechazan con un aviso para guardarlos como `.xlsx` o `.docx`.

## Cómo se lee cada formato

- **PDF**: texto de cada página en orden de lectura, con [PdfPig](https://github.com/UglyToad/PdfPig). Un PDF
  escaneado no trae texto; el documento lo indica en vez de quedar vacío.
- **Word**: párrafos y tablas del XML del `.docx`.
- **Excel y CSV**: hoja por hoja, y cada fila repite sus encabezados:

  ```
  Hoja Precios. Producto: Empanada; Precio: 2500
  Hoja Precios. Producto: Arepa; Precio: 3000
  ```

  Así un fragmento suelto sigue diciendo qué es cada valor. El CSV admite `;` o `,` como separador y valores entre
  comillas.

El texto se parte en fragmentos de hasta unas 300 palabras: las líneas cortas se agrupan y las páginas largas se
dividen. **Ver contenido** (el ojo en cada documento) muestra exactamente esos fragmentos, que es lo que usa el
agente.

## Cómo encuentra el agente la información

En cada mensaje se buscan los fragmentos más parecidos a la pregunta, solo en las colecciones del canal, con búsqueda
semántica (pgvector). Los mejores fragmentos llegan al agente con el nombre de su documento. Si la organización no
tiene nada indexado, no se gasta la llamada de embeddings.

La búsqueda por palabras que complementa a la semántica es todavía básica (coincidencia de términos). La reforma a
búsqueda híbrida real en PostgreSQL (`tsvector` + RRF), fragmentos con contexto, reordenamiento y consultas exactas
sobre tablas está en el [roadmap](../roadmap/ROADMAP.md).

**Probar**, en la página de Conocimiento, enseña qué encontraría el agente con una pregunta. Úsalo para comprobar
que un documento responde lo que esperas.

## Límites y seguridad

- El tipo de archivo se comprueba por su contenido, no solo por la extensión.
- Los archivos de Office se leen sin DTD (sin XXE) y con un tope de descompresión real (100 MB por documento),
  máximo de hojas, filas y columnas, y tope de texto. Un archivo manipulado no agota la memoria.
- Importar una página solo llega a direcciones públicas, revisadas al conectar y en cada redirección.
- Cada organización ve solo su conocimiento: filtros por organización y *row-level security* en PostgreSQL.

## Reprocesar

**Volver a procesar** recalcula los embeddings de los fragmentos existentes (por ejemplo, al cambiar de modelo). No
vuelve a leer el archivo original: si un documento se leyó con una versión anterior de los lectores, elimínalo y
súbelo otra vez.

## API

| Método | Ruta | Uso |
| --- | --- | --- |
| `GET` | `/api/knowledge` | Colecciones |
| `POST` | `/api/knowledge` | Crear colección `{ name, description }` |
| `GET` | `/api/knowledge/{id}/documents` | Documentos de una colección |
| `POST` | `/api/knowledge/upload` | Subir archivo (multipart: `file`, `knowledgeBaseId`, `category`) |
| `POST` | `/api/knowledge/text` | Respuesta escrita `{ knowledgeBaseId, title, content }` |
| `POST` | `/api/knowledge/url` | Importar página `{ knowledgeBaseId, url }` |
| `GET` | `/api/knowledge/documents/{id}/content` | Fragmentos de un documento |
| `GET` | `/api/knowledge/search?query=` | Probar una búsqueda |
| `DELETE` | `/api/knowledge/{documentId}` | Eliminar documento |

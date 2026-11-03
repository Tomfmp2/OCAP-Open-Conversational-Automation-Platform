# ADR-010: Memoria persistente de los agentes con plazo configurable

## Estado
Aceptado (v1.0.0)

## Contexto
Los agentes solo veían los últimos mensajes de la conversación abierta: olvidaban al cliente al día siguiente y el
historial crecía sin control en conversaciones largas. Hacía falta continuidad entre conversaciones sin disparar
el costo por mensaje y respetando la privacidad (plazo de conservación, derecho a pedir el borrado).

## Decisión
Tres capas, todas en PostgreSQL bajo *row-level security*:

1. **Resumen acumulado** (`Conversations.Summary`, `SummarizedUntil`): cuando una conversación supera 12 mensajes,
   un job resume lo antiguo. El agente recibe resumen y últimos 12 mensajes, con un costo estable.
2. **Hechos con clave** (`ContactMemories`, `Kind = Fact`): extraídos en una sola llamada al modelo cuando la
   conversación lleva 10 minutos inactiva. Un hecho con la misma clave **reemplaza** al anterior (`SupersededAt`)
   en vez de borrarlo; uno repetido renueva su vigencia.
3. **Episodios** (`Kind = Episode`): una o dos frases por conversación terminada.

- **Plazo**: `ExpiresAt = ConfirmedAt + plazo de la organización` (90 días por defecto, 0 la desactiva). El job
  diario de retención borra lo vencido.
- **Privacidad**: «olvídame» borra todo sin pasar por el modelo y marca las conversaciones para no volver a extraer
  lo dicho antes. Un filtro determinista descarta tarjetas, contraseñas y documentos aunque el modelo los proponga.
- **Seguridad**: la memoria llega al prompt delimitada como datos; no puede dar instrucciones ni autorizar
  herramientas. Las conversaciones con adjuntos no alimentan la memoria.
- **Frontera de E/S**: `IMemoryModel` encapsula las llamadas al modelo (y es el punto de prueba).

## Alternativas consideradas
- **Que el modelo decida ADD/UPDATE/DELETE** (Mem0 original): un estudio de 2026 mostró que deja vivos datos
  contradictorios. Reemplazar por clave es predecible y conserva el historial.
- **Búsqueda vectorial de recuerdos**: innecesaria con pocos hechos por persona; se ordenan por recencia. pgvector
  queda disponible si un contacto llegara a acumular muchos.
- **Extraer en cada mensaje**: multiplica el costo. Se extrae una vez por conversación terminada.

## Consecuencias
- Continuidad entre conversaciones con una llamada extra al modelo por conversación terminada.
- Un fallo del proveedor solo retrasa la memoria (se reintenta hasta 1 día).
- Guía de uso: [docs/guides/MEMORY.md](../guides/MEMORY.md).

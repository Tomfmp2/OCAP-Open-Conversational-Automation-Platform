# Arquitectura de Knowledge Base & RAG en OCAP (v1.5.0)

## Visión General
El módulo **OCAP Knowledge** extiende OCAP (Open Conversational Automation Platform) con capacidades de **Retrieval-Augmented Generation (RAG)** de nivel empresarial.

Está diseñado respetando estrictamente los principios de:
- **Clean Architecture**
- **Domain-Driven Design (DDD)**
- **Arquitectura Hexagonal (Puertos y Adaptadores)**
- **Modular Monolith**
- **Aislamiento Estricto Multi-Tenant**

## Estructura de Capas
```
src/Knowledge/
├── OCAP.Knowledge.Domain/          # Entidades puras, agregados, Value Objects y Enums (DDD)
├── OCAP.Knowledge.Abstractions/    # Contratos, interfaces de repositorios, parsers, chunkers, embeddings y vector DBs
├── OCAP.Knowledge.Application/     # Estrategias de chunking, parsers de 8 formatos, retriever e ingestión
└── OCAP.Knowledge.Infrastructure/  # Adaptadores de proveedores de embeddings (OpenAI, Gemini, Ollama) y Vector DBs (PgVector, Qdrant, Chroma, Pinecone)
```

## Aislamiento Multi-Tenant
Cada organización o inquilino (Tenant) posee un espacio vectorial y relacional completamente aislado.
Todas las consultas vectoriales y relacionales requieren y validan el `TenantId`, impidiendo la fuga de información entre clientes.

# Resumen de Benchmark (PR-03)

## Benchmark BenchmarkDotNet (Serialización y Outbox Processing)

| Operación | Media | Error | Desviación Estándar | Allocations |
|---|---|---|---|---|
| `JsonEventSerializer.Serialize` | 142 ns | 1.8 ns | 1.6 ns | 240 B |
| `JsonEventSerializer.Deserialize` | 185 ns | 2.1 ns | 1.9 ns | 320 B |
| `EfOutboxStore.SaveAsync` | 1.2 ms | 0.05 ms | 0.04 ms | 1.8 KB |
| `EfInboxStore.HasBeenProcessedAsync` | 0.8 ms | 0.02 ms | 0.02 ms | 1.1 KB |

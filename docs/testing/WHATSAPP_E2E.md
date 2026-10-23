# WhatsApp: prueba manual con un teléfono real

La integración del webhook se prueba en CI con un Evolution simulado. Lo que solo puede comprobar una persona es el
escaneo real del QR. Usa un número de prueba: al vincularlo, el asistente responderá a quien le escriba.

## Preparación

```bash
./scripts/ocap-up.sh
docker compose ps evolution-api   # healthy
```

Entra al panel con un administrador y abre **Canales**.

## Checklist

| # | Paso | Resultado esperado |
|---|---|---|
| 1 | Conectar canal → WhatsApp | "Generar QR" (o "WhatsApp vinculado" si ya lo estaba). |
| 2 | Generar QR | Aparece el QR en unos segundos. |
| 3 | Esperar 1 minuto sin escanear | El QR cambia solo (Evolution lo rota). |
| 4 | Escanear desde WhatsApp → Dispositivos vinculados | En menos de un minuto: "WhatsApp vinculado · +número". |
| 5 | Desde otro teléfono, escribir "Hola" al número | Llega una respuesta del asistente (o la de respaldo si no hay IA configurada). |
| 6 | Escribir tres mensajes seguidos | Una respuesta por mensaje, ninguna duplicada. |
| 7 | Escribir en un grupo donde esté el número | Sin respuesta. |
| 8 | `docker compose restart evolution-api ocap-api` | El canal sigue vinculado sin volver a escanear y responde. |
| 9 | Desvincular desde el panel | El dispositivo desaparece de "Dispositivos vinculados" del teléfono; el panel vuelve a "Generar QR". |
| 10 | Vincular otra vez y cerrar la sesión desde el teléfono | Al reabrir el modal, el estado es "Generar QR". |

## Diagnóstico

```bash
docker compose logs ocap-api | grep -i whatsapp      # "firma inválida" = secreto cambiado: pulsa Generar QR
docker compose logs evolution-api | tail -50
curl -s http://127.0.0.1:8088/instance/fetchInstances -H "apikey: $(grep ^EVOLUTION_API_KEY= .env | cut -d= -f2)"
```

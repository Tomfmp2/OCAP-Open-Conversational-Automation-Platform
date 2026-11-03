# ADR-011: El canal decide el conocimiento y las acciones de su agente

## Estado
Aceptado (v1.0.0)

## Contexto
Los agentes guardaban sus propias herramientas y colecciones de conocimiento. Para atender distinto en cada canal
(por ejemplo, el mismo agente de ventas con el catálogo mayorista en un bot y el minorista en la web) había que
duplicar agentes. Además, el validador de permisos solo miraba la lista del agente.

## Decisión
- El **agente** aporta instrucciones, personalidad y memoria. El **canal** define qué colecciones consulta y qué
  acciones puede ejecutar, en la metadata de la conexión (`KnowledgeBaseIds`, `Tools`).
- `PUT /api/channels/connections/{id}/capabilities` valida que las colecciones sean de la organización y que las
  acciones estén en `ChannelAgentRunner.ContactSafeTools`, nunca correo del negocio.
- Durante la petición, `ChannelToolGrants` (scoped) registra lo que el canal concedió y
  `AgentToolPermissionValidator` lo respeta: lo que el canal no concede no se ejecuta aunque el agente lo tuviera.
- **Compatibilidad**: un canal sin las claves usa lo configurado en el agente, así que nada cambia hasta guardar.

## Alternativas consideradas
- **Cambiar `IActionDispatcher` para recibir las herramientas concedidas**: tocaba 13 usos y los mocks de las
  pruebas. Un contexto por petición logra lo mismo sin cambiar el contrato.
- **Mantener herramientas en el agente**: obligaba a duplicar agentes por canal.

## Consecuencias
- Un mismo agente sirve a varios canales con capacidades distintas.
- El formulario del agente ya no elige conocimiento ni acciones; se hace en **Canales → Configurar**.
- Guía: [docs/guides/CHANNELS.md](../guides/CHANNELS.md).

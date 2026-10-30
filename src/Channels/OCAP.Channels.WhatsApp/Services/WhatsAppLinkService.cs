using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.Evolution;
using OCAP.Security.Domain.Entities;

namespace OCAP.Channels.WhatsApp.Services;

/// <summary>Estado de vinculación que ve el panel.</summary>
public static class WhatsAppLinkStates
{
    /// <summary>No hay WhatsApp vinculado: el panel ofrece "Vincular".</summary>
    public const string Unlinked = "unlinked";
    /// <summary>Esperando que se escanee el QR (o reconectando).</summary>
    public const string Pending = "pending";
    /// <summary>El número está vinculado y el asistente responde.</summary>
    public const string Linked = "linked";
}

public sealed record WhatsAppLinkStatus(
    string State,
    string? QrBase64 = null,
    string? PairingCode = null,
    string? PhoneNumber = null,
    string? ProfileName = null);

/// <summary>Evolution no está configurado en el servidor (URL, API key o URL del webhook).</summary>
public sealed class WhatsAppNotConfiguredException(string message) : Exception(message);

/// <summary>
/// Vincula el WhatsApp de una organización con Evolution API: una instancia por organización, creada con su webhook
/// firmado, y la conexión del canal en OCAP con el estado y el número vinculado.
/// </summary>
public sealed class WhatsAppLinkService
{
    private const string Provider = "WhatsApp";
    /// <summary>Evolution guarda la sesión; OCAP no tiene credenciales propias que cifrar para esta conexión.</summary>
    private const string ManagedCredentials = "evolution-managed";

    private readonly EvolutionApiClient _evolution;
    private readonly IChannelConnectionManager _connections;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppLinkService> _logger;

    public WhatsAppLinkService(
        EvolutionApiClient evolution,
        IChannelConnectionManager connections,
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppLinkService> logger)
    {
        _evolution = evolution;
        _connections = connections;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>Crea o reutiliza la instancia y devuelve el QR, o el estado vinculado si ya lo estaba.</summary>
    public async Task<WhatsAppLinkStatus> LinkAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var instance = EvolutionInstanceIdentity.InstanceNameFor(tenantId);
        var webhook = new EvolutionWebhookConfig(
            _settings.WebhookUrl,
            new Dictionary<string, string>
            {
                [EvolutionInstanceIdentity.SignatureHeader] = EvolutionInstanceIdentity.SignatureFor(instance, _settings.EffectiveWebhookSecret)
            });

        var state = await _evolution.GetConnectionStateAsync(instance, cancellationToken);
        if (state == EvolutionStates.Missing)
        {
            await _evolution.CreateInstanceAsync(instance, webhook, cancellationToken);
            _logger.LogInformation("Instancia de WhatsApp {Instance} creada para el tenant {TenantId}.", instance, tenantId);
        }
        else
        {
            // Mantiene la URL y la firma al día aunque hayan cambiado desde la primera vinculación.
            await _evolution.SetWebhookAsync(instance, webhook, cancellationToken);
        }

        var connection = await FindConnectionAsync(tenantId, cancellationToken);
        if (state == EvolutionStates.Open && connection is not null)
            return LinkedFrom(connection);

        await SaveAsync(tenantId, connection, instance, WhatsAppLinkStates.Pending, phone: null, profile: null, cancellationToken);
        var connect = await _evolution.ConnectAsync(instance, cancellationToken);
        return connect.State == EvolutionStates.Open
            ? new WhatsAppLinkStatus(WhatsAppLinkStates.Linked)
            : new WhatsAppLinkStatus(WhatsAppLinkStates.Pending, connect.Qr?.Base64, connect.Qr?.PairingCode);
    }

    /// <summary>Estado actual; con <paramref name="includeQr"/> devuelve también el QR vigente si está pendiente.</summary>
    public async Task<WhatsAppLinkStatus> GetStatusAsync(Guid tenantId, bool includeQr, CancellationToken cancellationToken)
    {
        var connection = await FindConnectionAsync(tenantId, cancellationToken);
        if (connection is null || !IsConfigured) return new WhatsAppLinkStatus(WhatsAppLinkStates.Unlinked);

        var instance = EvolutionInstanceIdentity.InstanceNameFor(tenantId);
        var state = await _evolution.GetConnectionStateAsync(instance, cancellationToken);
        if (state == EvolutionStates.Open) return LinkedFrom(connection);
        if (state == EvolutionStates.Missing) return new WhatsAppLinkStatus(WhatsAppLinkStates.Unlinked);
        if (!includeQr) return new WhatsAppLinkStatus(WhatsAppLinkStates.Pending);

        var connect = await _evolution.ConnectAsync(instance, cancellationToken);
        return new WhatsAppLinkStatus(WhatsAppLinkStates.Pending, connect.Qr?.Base64, connect.Qr?.PairingCode);
    }

    /// <summary>Cierra la sesión de WhatsApp, borra la instancia y la conexión del canal.</summary>
    public async Task UnlinkAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var instance = EvolutionInstanceIdentity.InstanceNameFor(tenantId);
        if (IsConfigured)
        {
            await _evolution.LogoutAsync(instance, cancellationToken);
            await _evolution.DeleteInstanceAsync(instance, cancellationToken);
        }

        var connection = await FindConnectionAsync(tenantId, cancellationToken);
        if (connection is not null)
            await _connections.RemoveConnectionAsync(tenantId, connection.Id, cancellationToken);
        _logger.LogInformation("WhatsApp desvinculado para el tenant {TenantId}.", tenantId);
    }

    /// <summary>Aplica un <c>connection.update</c> del webhook y devuelve el estado que debe ver el panel.</summary>
    public async Task<WhatsAppLinkStatus?> ApplyConnectionUpdateAsync(
        Guid tenantId,
        EvolutionConnectionUpdatedEvent update,
        CancellationToken cancellationToken)
    {
        var connection = await FindConnectionAsync(tenantId, cancellationToken);
        if (connection is null) return null;

        var state = update.State switch
        {
            EvolutionStates.Open => WhatsAppLinkStates.Linked,
            EvolutionStates.Connecting => WhatsAppLinkStates.Pending,
            _ => WhatsAppLinkStates.Unlinked
        };
        var phone = state == WhatsAppLinkStates.Linked ? update.PhoneNumber : null;
        var profile = state == WhatsAppLinkStates.Linked ? update.ProfileName : null;
        await SaveAsync(tenantId, connection, EvolutionInstanceIdentity.InstanceNameFor(tenantId), state, phone, profile, cancellationToken);
        return new WhatsAppLinkStatus(state, PhoneNumber: phone, ProfileName: profile);
    }

    /// <summary>La conexión WhatsApp de la organización, si existe y la gestiona Evolution.</summary>
    public async Task<ChannelConnection?> FindConnectionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var connections = await _connections.GetTenantConnectionsAsync(tenantId, cancellationToken);
        return connections.FirstOrDefault(c =>
            string.Equals(c.Provider, Provider, StringComparison.OrdinalIgnoreCase)
            && c.ConfigurationMetadata.TryGetValue("ConnectionMode", out var mode)
            && string.Equals(mode, "evolution", StringComparison.OrdinalIgnoreCase));
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.BaseUrl)
        && !string.IsNullOrWhiteSpace(_settings.ApiKey)
        && !string.IsNullOrWhiteSpace(_settings.WebhookUrl);

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new WhatsAppNotConfiguredException(
                "WhatsApp no está configurado en el servidor. Ejecuta ./scripts/ocap-setup.sh y vuelve a levantar OCAP.");
    }

    private static WhatsAppLinkStatus LinkedFrom(ChannelConnection connection) => new(
        WhatsAppLinkStates.Linked,
        PhoneNumber: connection.ConfigurationMetadata.GetValueOrDefault("PhoneNumber"),
        ProfileName: connection.ConfigurationMetadata.GetValueOrDefault("ProfileName"));

    private async Task SaveAsync(
        Guid tenantId,
        ChannelConnection? connection,
        string instance,
        string state,
        string? phone,
        string? profile,
        CancellationToken cancellationToken)
    {
        // "Instance" es solo informativo: la instancia siempre se deriva del tenant, nunca de la metadata.
        var metadata = new Dictionary<string, string>
        {
            ["ConnectionMode"] = "evolution",
            ["Instance"] = instance,
            ["Status"] = state
        };
        if (!string.IsNullOrWhiteSpace(phone)) metadata["PhoneNumber"] = phone;
        if (!string.IsNullOrWhiteSpace(profile)) metadata["ProfileName"] = profile;
        // El agente y la IA asignados desde el panel sobreviven a los cambios de estado de la vinculación.
        foreach (var key in new[] { "AgentId", "AiProviderConfigId", "CustomDisplayName" })
        {
            if (connection?.ConfigurationMetadata.TryGetValue(key, out var assigned) == true) metadata[key] = assigned;
        }
        var displayName = string.IsNullOrWhiteSpace(phone) ? "WhatsApp" : $"WhatsApp +{phone}";
        // Si el usuario le puso nombre al canal, se conserva.
        if (connection is not null && metadata.ContainsKey("CustomDisplayName")) displayName = connection.DisplayName;

        if (connection is null)
            await _connections.CreateConnectionAsync(tenantId, Provider, displayName, ManagedCredentials, metadata, cancellationToken);
        else
            await _connections.UpdateConfigurationAsync(tenantId, connection.Id, displayName, ManagedCredentials, metadata, cancellationToken);
    }
}

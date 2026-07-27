namespace OCAP.Channels.Abstractions.Configuration;

// Configuración individual para un canal de comunicación específico.
public class ChannelConfig
{
    // Indica si el canal está habilitado para ser inicializado y procesar tráfico.
    public bool Enabled { get; set; } = false;

    // Diccionario de ajustes arbitrarios específicos de cada proveedor (ej. API Key, Webhook URL).
    // Evita almacenar secretos duros en código o esquemas rígidos.
    public Dictionary<string, string> Settings { get; set; } = new();
}

// Estructura de configuración global para la sección "Channels" en appsettings.json.
public class ChannelsSettings
{
    public const string SectionName = "Channels";

    // Configuración para el canal de WhatsApp.
    public ChannelConfig WhatsApp { get; set; } = new();

    // Configuración para el canal de Telegram.
    public ChannelConfig Telegram { get; set; } = new();

    // Configuración para el canal de pruebas Mock.
    public ChannelConfig Mock { get; set; } = new();
}

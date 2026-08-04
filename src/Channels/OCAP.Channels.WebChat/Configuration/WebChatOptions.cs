namespace OCAP.Channels.WebChat.Configuration;

public class WebChatOptions
{
    public const string SectionName = "Channels:WebChat";

    public bool Enabled { get; set; } = true;
    public string DefaultWidgetTitle { get; set; } = "Asistente OCAP";
}

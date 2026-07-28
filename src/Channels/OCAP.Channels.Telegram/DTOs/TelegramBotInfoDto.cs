namespace OCAP.Channels.Telegram.DTOs;

public class TelegramBotInfoDto
{
    public long Id { get; set; }
    public bool IsBot { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public bool CanJoinGroups { get; set; }
    public bool CanReadExtraMessages { get; set; }
}

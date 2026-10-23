using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Services;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Channels.Telegram.Services;

namespace OCAP.Api.Tests.Endpoints;

public sealed class TelegramFactory : OcapApiFactory
{
    public FakeTelegramApi Telegram { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.AddHttpClient<TelegramApiClient>().ConfigurePrimaryHttpMessageHandler(() => Telegram));
    }
}

/// <summary>Cada clase tiene su propia base: una organización solo puede tener un bot de Telegram.</summary>
public abstract class TelegramBotTestsBase : IClassFixture<TelegramFactory>
{
    protected readonly TelegramFactory _factory;

    protected TelegramBotTestsBase(TelegramFactory factory) => _factory = factory;

    protected async Task<HttpResponseMessage> PostUpdateAsync(string path, object update, string secret)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(update) };
        request.Headers.Add("X-Telegram-Bot-Api-Secret-Token", secret);
        return await _factory.CreateClient().SendAsync(request);
    }

    protected async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@ocap.io", password = "ChangeMe_Admin_2026!" });
        var session = await login.Content.ReadFromJsonAsync<Session>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        return client;
    }

    protected static readonly Guid SeedTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private sealed record Session(string AccessToken);
    protected sealed record ConversationsPage(ConversationsData Data);
    protected sealed record ConversationsData(List<ConversationItem> Items);
    protected sealed record ConversationItem(Guid UserId);
}

public class TelegramWebhookBotTests : TelegramBotTestsBase
{
    public TelegramWebhookBotTests(TelegramFactory factory) : base(factory) { }

    [Fact]
    public async Task WebhookBot_RepliesOnce_AndRejectsForgedCalls()
    {
        var admin = await AdminClientAsync();
        const string token = "111:webhook-bot-token";

        var created = await admin.PostAsJsonAsync("/api/channels/telegram/bots", new
        {
            displayName = "Soporte",
            botToken = token,
            connectionMode = "webhook",
            webhookBaseUrl = "https://ocap.example.com"
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        // El webhook queda registrado con una URL propia de la conexión y un secreto derivado del token.
        var (url, secret) = _factory.Telegram.Webhooks[token];
        url.Should().MatchRegex(@"^https://ocap\.example\.com/api/channels/telegram/webhook/[0-9a-f-]{36}$");
        var path = new Uri(url).AbsolutePath;

        var update = FakeTelegramApi.PrivateMessageUpdate(5001, 7001, "Hola, ¿abren hoy?");
        (await PostUpdateAsync(path, update, secret)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PostUpdateAsync(path, update, secret)).StatusCode.Should().Be(HttpStatusCode.OK);

        var replies = _factory.Telegram.Sent.Where(s => s.ChatId == "7001").ToList();
        replies.Should().ContainSingle();
        replies[0].Token.Should().Be(token);
        replies[0].Text.Should().NotBeNullOrWhiteSpace();
        replies[0].ParseMode.Should().BeNull();

        (await PostUpdateAsync(path, FakeTelegramApi.PrivateMessageUpdate(5002, 7002, "hola"), "secreto-falso"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostUpdateAsync($"/api/channels/telegram/webhook/{Guid.NewGuid()}", update, secret))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await PostUpdateAsync(path, FakeTelegramApi.PrivateMessageUpdate(5003, -7003, "en un grupo", "group"), secret))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.Telegram.Sent.Should().NotContain(s => s.ChatId == "-7003");
    }
}

public class TelegramPollingBotTests : TelegramBotTestsBase
{
    public TelegramPollingBotTests(TelegramFactory factory) : base(factory) { }

    [Fact]
    public async Task PollingBot_IsPolledWithItsOwnToken_AndReplies()
    {
        var admin = await AdminClientAsync();
        const string token = "222:polling-bot-token";

        var created = await admin.PostAsJsonAsync("/api/channels/telegram/bots", new
        {
            displayName = "Ventas",
            botToken = token,
            connectionMode = "polling"
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        _factory.Telegram.Webhooks.Should().NotContainKey(token);

        _factory.Telegram.EnqueuePrivateMessage(token, 8001, "¿Tienen envíos?");

        // El servicio de polling corre en el host de pruebas cada 2 s.
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline && !_factory.Telegram.Sent.Any(s => s.ChatId == "8001"))
            await Task.Delay(250);

        _factory.Telegram.Sent.Should().ContainSingle(s => s.ChatId == "8001" && s.Token == token);

        // El mensaje quedó en la conversación del contacto dentro de la organización del bot.
        var contact = ChannelConversationService.ContactUserId("Telegram", SeedTenantId, "8001");
        var conversations = await admin.GetFromJsonAsync<ConversationsPage>($"/api/conversations?search={contact}");
        conversations!.Data.Items.Should().ContainSingle(c => c.UserId == contact);
    }
}

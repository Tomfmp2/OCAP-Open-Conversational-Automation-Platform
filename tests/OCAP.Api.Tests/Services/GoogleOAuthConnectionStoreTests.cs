using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OCAP.Api.Services;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Api.Tests.Services;

public class GoogleOAuthConnectionStoreTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task SaveAsync_StoresTheTokensEncrypted()
    {
        var (store, db, _) = Create();

        await store.SaveAsync(TenantId, UserId, new GoogleTokenGrant("ya29.acceso", "1//refresco", DateTime.UtcNow.AddHours(1), null), "gmail", default);

        var row = await db.OAuthConnections.AsNoTracking().SingleAsync();
        row.AccessToken.Should().StartWith("vault:").And.NotContain("ya29.acceso");
        row.RefreshToken.Should().StartWith("vault:").And.NotContain("1//refresco");
        (await store.GetAccessTokenAsync(TenantId, default)).Should().Be("ya29.acceso");
    }

    [Fact]
    public async Task GetAccessTokenAsync_RenewsAnExpiringTokenWithTheRefreshToken()
    {
        var (store, db, google) = Create();
        await store.SaveAsync(TenantId, UserId, new GoogleTokenGrant("viejo", "1//refresco", DateTime.UtcNow.AddSeconds(30), null), "gmail", default);
        google.Respond("""{ "access_token": "nuevo", "expires_in": 3599 }""");

        (await store.GetAccessTokenAsync(TenantId, default)).Should().Be("nuevo");

        google.LastForm.Should().Contain("grant_type=refresh_token").And.Contain("refresh_token=1%2F%2Frefresco");
        var row = await db.OAuthConnections.AsNoTracking().SingleAsync();
        row.TokenExpiration.Should().BeAfter(DateTime.UtcNow.AddMinutes(50));
        // Google no reenvía el refresh token al renovar: se conserva el anterior.
        (await store.GetAccessTokenAsync(TenantId, default, forceRefresh: true)).Should().Be("nuevo");
        google.LastForm.Should().Contain("refresh_token=1%2F%2Frefresco");
    }

    [Fact]
    public async Task GetAccessTokenAsync_EncryptsAConnectionSavedInPlainTextBefore()
    {
        var (store, db, _) = Create();
        db.OAuthConnections.Add(new OAuthConnection(Guid.NewGuid(), UserId, "Google", "plano-acceso", "plano-refresco",
            DateTime.UtcNow.AddHours(1), "gmail", TenantId));
        await db.SaveChangesAsync();

        (await store.GetAccessTokenAsync(TenantId, default)).Should().Be("plano-acceso");

        var row = await db.OAuthConnections.AsNoTracking().SingleAsync();
        row.AccessToken.Should().StartWith("vault:");
        row.RefreshToken.Should().StartWith("vault:");
    }

    [Fact]
    public async Task GetAccessTokenAsync_NeverReturnsAnotherOrganizationsToken()
    {
        var (store, _, _) = Create();
        await store.SaveAsync(TenantId, UserId, new GoogleTokenGrant("de-otra", null, DateTime.UtcNow.AddHours(1), null), "gmail", default);

        (await store.GetAccessTokenAsync(Guid.NewGuid(), default)).Should().BeNull();
    }

    private static (GoogleOAuthConnectionStore Store, OCAPDbContext Db, FakeGoogle Google) Create()
    {
        var db = new OCAPDbContext(new DbContextOptionsBuilder<OCAPDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var vault = new AesDbCredentialVault(NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");
        var google = new FakeGoogle();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Google:ClientId"] = "client-id",
            ["Google:ClientSecret"] = "client-secret"
        }).Build();
        var client = new GoogleOAuthClient(new HttpClient(google), configuration, new OrganizationGoogleSettings(db, vault));
        return (new GoogleOAuthConnectionStore(db, vault, client, NullLogger<GoogleOAuthConnectionStore>.Instance), db, google);
    }

    private sealed class FakeGoogle : HttpMessageHandler
    {
        private string _body = "{}";
        public string LastForm { get; private set; } = string.Empty;

        public void Respond(string body) => _body = body;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastForm = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
        }
    }
}

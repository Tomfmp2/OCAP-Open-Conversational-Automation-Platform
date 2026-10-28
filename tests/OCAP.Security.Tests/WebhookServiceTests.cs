using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCAP.Core.Events;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Infrastructure.Services;
using Xunit;

namespace OCAP.Security.Tests;

public class WebhookServiceTests
{
    private readonly HmacSha256WebhookSigner _signer = new();
    private readonly Mock<ISecurityAuditService> _auditMock = new();
    private readonly AesDbCredentialVault _vault =
        new(Microsoft.Extensions.Logging.Abstractions.NullLogger<AesDbCredentialVault>.Instance, "OCAP_TESTING_VAULT_MASTER_KEY_32CHARS_MIN!");

    private static OCAPDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OCAPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OCAPDbContext(options);
    }

    [Fact]
    public void SignPayload_ComputesValidHmacSha256Signature()
    {
        // Arrange
        var payload = "{\"event\":\"WorkflowCompleted\",\"executionId\":\"12345\"}";
        var secret = "super-secret-key-99";

        // Act
        var signature = _signer.SignPayload(payload, secret);
        var isValid = _signer.VerifySignature(payload, secret, signature);

        // Assert
        signature.Should().StartWith("sha256=");
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task WebhookManagement_SupportsCreateUpdateDeleteAndTenantIsolation()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = new WebhookService(db, _signer, _auditMock.Object, _vault);
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();

        // Act - Create
        var sub1 = await service.CreateSubscriptionAsync(tenantId1, "Main Endpoint", "https://93.184.215.14/webhook", "sec1", new[] { "WorkflowCompleted" });
        var sub2 = await service.CreateSubscriptionAsync(tenantId2, "Tenant 2 Endpoint", "https://93.184.215.15/webhook", "sec2", new[] { "*" });

        // Assert - Tenant Isolation
        var tenant1Subs = await service.GetSubscriptionsForTenantAsync(tenantId1);
        tenant1Subs.Should().HaveCount(1);
        tenant1Subs[0].Id.Should().Be(sub1.Id);

        // Act - Update
        var updated = await service.UpdateSubscriptionAsync(sub1.Id, tenantId1, "Updated Endpoint", "https://93.184.215.14/v2", null, new[] { "WorkflowCompleted", "NodeExecuted" }, true);
        updated.Should().NotBeNull();
        updated!.TargetUrl.Should().Be("https://93.184.215.14/v2");
        updated.SubscribedEvents.Should().Contain("NodeExecuted");

        // Act - Delete
        var deleted = await service.DeleteSubscriptionAsync(sub1.Id, tenantId1);
        deleted.Should().BeTrue();

        var tenant1SubsAfterDelete = await service.GetSubscriptionsForTenantAsync(tenantId1);
        tenant1SubsAfterDelete.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventWebhooksAsync_SendsSignedHttpPayloadAndRecordsHistory()
    {
        // Arrange
        using var db = CreateDbContext();
        var handlerMock = new MockHttpMessageHandler(HttpStatusCode.OK, "{\"received\":true}");
        var httpClient = new HttpClient(handlerMock);
        var service = new WebhookService(db, _signer, _auditMock.Object, _vault, httpClient);

        var tenantId = Guid.NewGuid();
        var sub = await service.CreateSubscriptionAsync(tenantId, "Prod Webhook", "https://93.184.215.16/webhook", "whsec_123", new[] { "WorkflowCompletedEvent" });

        var evt = new WorkflowCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), tenantId, "{\"result\":\"ok\"}", 250.0);

        // Act
        await service.DispatchEventWebhooksAsync(evt);

        // Assert
        handlerMock.RequestCount.Should().Be(1);
        handlerMock.LastRequestHeaders.Should().NotBeNull();
        handlerMock.LastRequestHeaders!.Contains("X-OCAP-Signature").Should().BeTrue();
        handlerMock.LastRequestHeaders.Contains("X-OCAP-Event").Should().BeTrue();
        handlerMock.LastRequestHeaders.GetValues("X-OCAP-Event").FirstOrDefault().Should().Be("WorkflowCompletedEvent");
        // La base guarda el secreto cifrado, pero la firma se calcula con el secreto original.
        (await db.WebhookSubscriptions.AsNoTracking().SingleAsync()).Secret.Should().StartWith("vault:").And.NotContain("whsec_123");
        handlerMock.LastRequestHeaders.GetValues("X-OCAP-Signature").Single()
            .Should().Be(_signer.SignPayload(handlerMock.LastRequestBody!, "whsec_123"));

        var history = await service.GetDeliveryHistoryAsync(sub.Id, tenantId);
        history.Should().HaveCount(1);
        history[0].Success.Should().BeTrue();
        history[0].StatusCode.Should().Be(200);
        history[0].AttemptCount.Should().Be(1);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;

        public int RequestCount { get; private set; }
        public HttpRequestHeaders? LastRequestHeaders { get; private set; }
        public string? LastRequestBody { get; private set; }

        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestHeaders = request.Headers;
            LastRequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            };

            return response;
        }
    }
}

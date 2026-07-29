using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using OCAP.Workflow.Application.Nodes;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;
using Xunit;

namespace OCAP.Workflow.Tests;

public class HttpRequestNodeExecutorTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public HttpRequestNodeExecutorTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task ExecuteAsync_SupportsAllHttpMethods(string method)
    {
        // Arrange
        var executor = new HttpRequestNodeExecutor(_httpClientFactoryMock.Object, NullLogger<HttpRequestNodeExecutor>.Instance);
        var config = new HttpRequestNodeConfiguration
        {
            Url = "https://api.example.com/items",
            Method = method,
            Body = method != "GET" ? "{\"name\": \"Test\"}" : null
        };
        var step = new WorkflowStep(Guid.NewGuid(), "step_http", "HTTP Step", WorkflowNodeType.ApiRequest, JsonSerializer.Serialize(config));
        var context = new WorkflowContext { TenantId = Guid.NewGuid() };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method.Method == method),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"ok\"}")
            });

        // Act
        var result = await executor.ExecuteAsync(step, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.NextStepId.Should().Be("next");
        result.OutputJson.Should().Contain("\"statusCode\":200");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_ReplacesVariablesInUrlHeadersAndBody()
    {
        // Arrange
        var executor = new HttpRequestNodeExecutor(_httpClientFactoryMock.Object, NullLogger<HttpRequestNodeExecutor>.Instance);
        var config = new HttpRequestNodeConfiguration
        {
            Url = "https://api.example.com/users/{{userId}}",
            Method = "POST",
            Headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer {{authToken}}" },
                { "X-Tenant-Id", "{{tenantId}}" }
            },
            Body = "{\"name\": \"{{userName}}\"}"
        };
        var step = new WorkflowStep(Guid.NewGuid(), "step_http", "HTTP Step", WorkflowNodeType.ApiRequest, JsonSerializer.Serialize(config));

        var tenantId = Guid.NewGuid();
        var context = new WorkflowContext
        {
            TenantId = tenantId,
            Variables = new Dictionary<string, object>
            {
                { "userId", "12345" },
                { "authToken", "secret-token-abc" },
                { "tenantId", tenantId.ToString() },
                { "userName", "Juan Pérez" }
            }
        };

        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                capturedRequest = req;
                if (req.Content != null)
                {
                    capturedBody = await req.Content.ReadAsStringAsync(ct);
                }
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent("{\"id\":\"12345\",\"created\":true}")
            });

        // Act
        var result = await executor.ExecuteAsync(step, context);

        // Assert
        result.Success.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().Be("https://api.example.com/users/12345");
        capturedRequest.Headers.GetValues("Authorization").Should().Contain("Bearer secret-token-abc");
        capturedRequest.Headers.GetValues("X-Tenant-Id").Should().Contain(tenantId.ToString());
        capturedBody.Should().Be("{\"name\": \"Juan Pérez\"}");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenResponseIsError_AndFailOnErrorCodeIsTrue()
    {
        // Arrange
        var executor = new HttpRequestNodeExecutor(_httpClientFactoryMock.Object, NullLogger<HttpRequestNodeExecutor>.Instance);
        var config = new HttpRequestNodeConfiguration
        {
            Url = "https://api.example.com/notfound",
            Method = "GET",
            FailOnErrorCode = true
        };
        var step = new WorkflowStep(Guid.NewGuid(), "step_http", "HTTP Step", WorkflowNodeType.ApiRequest, JsonSerializer.Serialize(config));
        var context = new WorkflowContext();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\":\"Not Found\"}")
            });

        // Act
        var result = await executor.ExecuteAsync(step, context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("404");
        result.OutputJson.Should().Contain("\"statusCode\":404");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess_WhenResponseIsError_AndFailOnErrorCodeIsFalse()
    {
        // Arrange
        var executor = new HttpRequestNodeExecutor(_httpClientFactoryMock.Object, NullLogger<HttpRequestNodeExecutor>.Instance);
        var config = new HttpRequestNodeConfiguration
        {
            Url = "https://api.example.com/notfound",
            Method = "GET",
            FailOnErrorCode = false
        };
        var step = new WorkflowStep(Guid.NewGuid(), "step_http", "HTTP Step", WorkflowNodeType.ApiRequest, JsonSerializer.Serialize(config));
        var context = new WorkflowContext();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\":\"Not Found\"}")
            });

        // Act
        var result = await executor.ExecuteAsync(step, context);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputJson.Should().Contain("\"statusCode\":404");
    }

    [Fact]
    public async Task ExecuteAsync_HandlesUnSupportedMethod_Gracefully()
    {
        // Arrange
        var executor = new HttpRequestNodeExecutor(_httpClientFactoryMock.Object, NullLogger<HttpRequestNodeExecutor>.Instance);
        var config = new HttpRequestNodeConfiguration
        {
            Url = "https://api.example.com/test",
            Method = "INVALID_METHOD"
        };
        var step = new WorkflowStep(Guid.NewGuid(), "step_http", "HTTP Step", WorkflowNodeType.ApiRequest, JsonSerializer.Serialize(config));

        // Act
        var result = await executor.ExecuteAsync(step, new WorkflowContext());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("no soportado");
    }
}

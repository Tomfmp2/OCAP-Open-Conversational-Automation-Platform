using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class WorkflowsEndpointTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;

    public WorkflowsEndpointTests(OcapApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Crud_CreateListGetUpdateStatusAndDelete()
    {
        var client = await AdminClientAsync();

        var created = await client.PostAsJsonAsync("/api/workflows", new { name = "Bienvenida", description = "Saludo inicial" });
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var workflow = (await created.Content.ReadFromJsonAsync<Workflow>())!;
        workflow.Name.Should().Be("Bienvenida");

        (await client.GetFromJsonAsync<List<Workflow>>("/api/workflows"))!.Should().Contain(w => w.Id == workflow.Id);
        (await client.GetFromJsonAsync<Workflow>($"/api/workflows/{workflow.Id}"))!.Description.Should().Be("Saludo inicial");

        var updated = await client.PutAsJsonAsync($"/api/workflows/{workflow.Id}", new { name = "Bienvenida v2", description = "Nuevo" });
        (await updated.Content.ReadFromJsonAsync<Workflow>())!.Name.Should().Be("Bienvenida v2");

        var status = await client.GetFromJsonAsync<WorkflowStatus>($"/api/workflows/{workflow.Id}/status");
        status!.TotalExecutions.Should().Be(0);
        status.LastExecutedAtUtc.Should().BeNull();
        (await client.GetFromJsonAsync<List<object>>($"/api/workflows/{workflow.Id}/executions"))!.Should().BeEmpty();
        (await client.GetAsync($"/api/workflows/{workflow.Id}/designer")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.DeleteAsync($"/api/workflows/{workflow.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/workflows/{workflow.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.DeleteAsync($"/api/workflows/{workflow.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnknownWorkflow_IsNotFound()
    {
        var client = await AdminClientAsync();
        var id = Guid.NewGuid();

        (await client.GetAsync($"/api/workflows/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/workflows/{id}/status")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync($"/api/workflows/{id}", new { name = "x" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/workflows/{id}/designer")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutName_IsBadRequest()
    {
        var client = await AdminClientAsync();

        (await client.PostAsJsonAsync("/api/workflows", new { name = " " })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Executions_ListsTheSeededExecutionOfTheTenant()
    {
        var client = await AdminClientAsync();

        var executions = await client.GetFromJsonAsync<List<Execution>>("/api/workflows/executions");

        executions.Should().NotBeEmpty();
        executions!.Should().BeInDescendingOrder(e => e.StartedAtUtc);
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@ocap.io", password = "ChangeMe_Admin_2026!" });
        var session = await login.Content.ReadFromJsonAsync<Session>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        return client;
    }

    private sealed record Session(string AccessToken);
    private sealed record Workflow(Guid Id, string Name, string? Description);
    private sealed record WorkflowStatus(int TotalExecutions, DateTime? LastExecutedAtUtc);
    private sealed record Execution(Guid Id, DateTime StartedAtUtc);
}

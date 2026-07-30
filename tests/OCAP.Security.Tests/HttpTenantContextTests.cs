using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Security.Tests;

public class HttpTenantContextTests
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public void TenantId_WhenAuthenticated_UsesClaimAndIgnoresSpoofedHeader()
    {
        var claimedTenant = Guid.NewGuid();
        var spoofedTenant = Guid.NewGuid();
        var accessor = CreateAccessor(
            isAuthenticated: true,
            tenantClaim: claimedTenant,
            headerTenant: spoofedTenant);

        var context = new HttpTenantContext(accessor, new TestHostEnvironment(Environments.Production));

        context.TenantId.Should().Be(claimedTenant);
    }

    [Fact]
    public void TenantId_WhenAnonymousInProduction_IgnoresHeaderAndReturnsEmpty()
    {
        var spoofedTenant = Guid.NewGuid();
        var accessor = CreateAccessor(
            isAuthenticated: false,
            tenantClaim: null,
            headerTenant: spoofedTenant);

        var context = new HttpTenantContext(accessor, new TestHostEnvironment(Environments.Production));

        context.TenantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TenantId_WhenAuthenticatedWithoutClaim_ReturnsEmpty()
    {
        var accessor = CreateAccessor(
            isAuthenticated: true,
            tenantClaim: null,
            headerTenant: Guid.NewGuid());

        var context = new HttpTenantContext(accessor, new TestHostEnvironment(Environments.Production));

        context.TenantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TenantId_WhenAnonymousInTesting_AllowsHeader()
    {
        var headerTenant = Guid.NewGuid();
        var accessor = CreateAccessor(
            isAuthenticated: false,
            tenantClaim: null,
            headerTenant: headerTenant);

        var context = new HttpTenantContext(accessor, new TestHostEnvironment("Testing"));

        context.TenantId.Should().Be(headerTenant);
    }

    [Fact]
    public void BypassTenantFilters_WhenNoHttpContext_IsTrue()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var context = new HttpTenantContext(accessor, new TestHostEnvironment(Environments.Production));

        context.BypassTenantFilters.Should().BeTrue();
    }

    [Fact]
    public void BypassTenantFilters_WhenHttpContextPresent_IsFalse()
    {
        var accessor = CreateAccessor(isAuthenticated: true, tenantClaim: Guid.NewGuid(), headerTenant: null);
        var context = new HttpTenantContext(accessor, new TestHostEnvironment(Environments.Production));

        context.BypassTenantFilters.Should().BeFalse();
    }

    private static IHttpContextAccessor CreateAccessor(bool isAuthenticated, Guid? tenantClaim, Guid? headerTenant)
    {
        var identity = isAuthenticated
            ? new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim("tenant_id", tenantClaim?.ToString() ?? string.Empty)
            }, authenticationType: "Bearer")
            : new ClaimsIdentity();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        if (headerTenant.HasValue)
        {
            httpContext.Request.Headers["X-Tenant-ID"] = headerTenant.Value.ToString();
        }

        return new HttpContextAccessor { HttpContext = httpContext };
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "OCAP.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

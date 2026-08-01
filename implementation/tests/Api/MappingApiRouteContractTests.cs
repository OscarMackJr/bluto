using System.Security.Claims;
using Bluto.Api.Mapping;
using Bluto.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bluto.Api.Tests;

public sealed class MappingApiRouteContractTests
{
    [Fact]
    [Trait("Category", "Api")]
    [Trait("Category", "Contract")]
    public void Api_project_is_executable_aspnet_core_boundary()
    {
        var project = File.ReadAllText(Path.Combine(RepositoryRoot(), "implementation", "src", "Bluto.Api", "Bluto.Api.csproj"));

        Assert.Contains("Microsoft.NET.Sdk.Web", project);
    }

    [Fact]
    [Trait("Category", "Api")]
    [Trait("Category", "Contract")]
    public void Resolve_current_party_route_is_registered_on_versioned_http_boundary()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddBlutoMappingApi();
        var app = builder.Build();

        app.MapBlutoMappingApi();

        var route = ((IEndpointRouteBuilder)app).DataSources.SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == "/v1/mappings/{source_system}/{source_key}");

        Assert.Equal("GET", route.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Single());
        Assert.NotNull(route.Metadata.GetMetadata<IAuthorizeData>());
    }

    [Fact]
    [Trait("Category", "Api")]
    [Trait("Category", "Contract")]
    public void Authorization_policy_preserves_request_and_correlation_headers()
    {
        var requestId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var correlationId = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var tenantId = Guid.Parse("33333333-3333-4333-8333-333333333333");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("tenant_id", tenantId.ToString("D")) }, "test"))
        };
        httpContext.Request.Headers["x-request-id"] = requestId.ToString("D");
        httpContext.Request.Headers["x-correlation-id"] = correlationId.ToString("D");

        var context = new ClaimsBlutoAuthorizationPolicy().CreateRequestContext(httpContext, "resolve_current_party", "mapping/crm/key");

        Assert.True(context.IsAuthenticated);
        Assert.Equal(requestId, context.RequestId);
        Assert.Equal(correlationId, context.CorrelationId);
        Assert.Equal(tenantId, context.TenantId);
    }
    [Fact]
    [Trait("Category", "Api")]
    [Trait("Category", "Contract")]
    public void Mapping_api_registers_query_handler_for_dependency_injection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentMappingRepository>(new StubCurrentMappingRepository());

        services.AddBlutoMappingApi();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<CurrentMappingQueryService>());
    }
    private static string RepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory, "implementation")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        return directory ?? throw new InvalidOperationException("Repository root not found.");
    }
    private sealed class StubCurrentMappingRepository : ICurrentMappingRepository
    {
        public Task<CurrentMapping?> ResolveCurrentAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken) =>
            Task.FromResult<CurrentMapping?>(null);
    }
}


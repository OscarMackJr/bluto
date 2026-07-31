using Bluto.Api.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
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

    private static string RepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory, "implementation")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        return directory ?? throw new InvalidOperationException("Repository root not found.");
    }
}


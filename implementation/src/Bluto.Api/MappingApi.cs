using System.Text.Json.Serialization;
using Bluto.Application.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bluto.Api.Mapping;

public sealed record MappingRequestContext(
    bool IsAuthenticated,
    Guid? TenantId,
    IReadOnlySet<Guid> AuthorizedTenantIds,
    Guid CorrelationId);

public sealed record MappingHttpResponse(
    int StatusCode,
    IReadOnlyDictionary<string, string> Headers,
    object? Body);

public sealed record MappingApiResponse(
    [property: JsonPropertyName("party_id")] Guid PartyId,
    [property: JsonPropertyName("tenant_id")] Guid TenantId,
    [property: JsonPropertyName("source_system")] string SourceSystem,
    [property: JsonPropertyName("source_key")] string SourceKey,
    [property: JsonPropertyName("effective_from")] DateTimeOffset EffectiveFrom,
    [property: JsonPropertyName("effective_to")] DateTimeOffset? EffectiveTo,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("rule_version")] string RuleVersion,
    [property: JsonPropertyName("provenance_reference")] string ProvenanceReference);

public sealed record MappingApiError(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("correlation_id")] Guid CorrelationId);

public interface IBlutoAuthorizationPolicy
{
    MappingRequestContext CreateRequestContext(HttpContext httpContext, string operation, string resource);
}

public sealed class ClaimsBlutoAuthorizationPolicy : IBlutoAuthorizationPolicy
{
    public MappingRequestContext CreateRequestContext(HttpContext httpContext, string operation, string resource)
    {
        var correlationId = ResolveCorrelationId(httpContext);
        var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
        var authorizedTenantClaims = httpContext.User.FindAll("tenant_id").Select(claim => claim.Value)
            .Concat(httpContext.User.FindAll("authorized_tenant_id").Select(claim => claim.Value));
        var authorizedTenantIds = authorizedTenantClaims
            .Select(value => Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty)
            .Where(value => value != Guid.Empty)
            .ToHashSet();

        return new MappingRequestContext(
            httpContext.User.Identity?.IsAuthenticated == true,
            Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null,
            authorizedTenantIds,
            correlationId);
    }

    private static Guid ResolveCorrelationId(HttpContext httpContext) =>
        httpContext.Request.Headers.TryGetValue("x-correlation-id", out var values)
        && Guid.TryParse(values.FirstOrDefault(), out var parsed)
            ? parsed
            : Guid.NewGuid();
}

public static class BlutoMappingApiServiceCollectionExtensions
{
    public static IServiceCollection AddBlutoMappingApi(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("resolve-current-party", policy => policy.RequireAuthenticatedUser());
        services.AddSingleton<IBlutoAuthorizationPolicy, ClaimsBlutoAuthorizationPolicy>();
        return services;
    }
}

public static class BlutoMappingEndpoints
{
    public static IEndpointRouteBuilder MapBlutoMappingApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/v1/mappings/{source_system}/{source_key}", ResolveCurrentPartyEndpointAsync)
            .WithName("resolveCurrentParty")
            .RequireAuthorization("resolve-current-party");
        return endpoints;
    }

    private static async Task<IResult> ResolveCurrentPartyEndpointAsync(
        string source_system,
        string source_key,
        HttpContext httpContext,
        [FromServices] CurrentMappingQueryService service,
        [FromServices] IBlutoAuthorizationPolicy authorizationPolicy,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var context = authorizationPolicy.CreateRequestContext(httpContext, "resolve_current_party", $"mapping/{source_system}/{source_key}");
        var logger = loggerFactory.CreateLogger("Bluto.Api.Mapping");
        var response = await MappingApi.ResolveCurrentPartyAsync(
            source_system,
            source_key,
            context,
            service,
            message => logger.LogInformation("{BlutoMappingOutcome}", message),
            cancellationToken);

        foreach (var header in response.Headers)
        {
            httpContext.Response.Headers[header.Key] = header.Value;
        }

        return Results.Json(response.Body, statusCode: response.StatusCode);
    }
}

public static class MappingApi
{
    public static async Task<MappingHttpResponse> ResolveCurrentPartyAsync(
        string sourceSystem,
        string sourceKey,
        MappingRequestContext context,
        CurrentMappingQueryService service,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["x-correlation-id"] = context.CorrelationId.ToString("D")
        };

        if (!context.IsAuthenticated)
        {
            log($"resolve_current_party outcome=unauthenticated correlation_id={context.CorrelationId:D}");
            return Error(401, "unauthenticated", "Authentication is required.", context.CorrelationId, headers);
        }

        if (context.TenantId is null || context.TenantId == Guid.Empty)
        {
            log($"resolve_current_party outcome=tenant_scope_missing correlation_id={context.CorrelationId:D}");
            return Error(403, "tenant_scope_forbidden", "Tenant scope is not authorized.", context.CorrelationId, headers);
        }

        if (!context.AuthorizedTenantIds.Contains(context.TenantId.Value))
        {
            log($"resolve_current_party outcome=concealed correlation_id={context.CorrelationId:D}");
            return Error(404, "mapping_not_found", "Mapping was not found.", context.CorrelationId, headers);
        }

        try
        {
            var mapping = await service.ResolveAsync(
                new ResolveCurrentPartyQuery(
                    context.TenantId.Value,
                    context.AuthorizedTenantIds,
                    sourceSystem,
                    sourceKey,
                    context.CorrelationId),
                cancellationToken);

            if (mapping is null)
            {
                log($"resolve_current_party outcome=not_found correlation_id={context.CorrelationId:D}");
                return Error(404, "mapping_not_found", "Mapping was not found.", context.CorrelationId, headers);
            }

            log($"resolve_current_party outcome=found correlation_id={context.CorrelationId:D}");
            return new MappingHttpResponse(200, headers, From(mapping));
        }
        catch (UnauthorizedAccessException)
        {
            log($"resolve_current_party outcome=concealed correlation_id={context.CorrelationId:D}");
            return Error(404, "mapping_not_found", "Mapping was not found.", context.CorrelationId, headers);
        }
        catch (ArgumentException)
        {
            log($"resolve_current_party outcome=invalid_request correlation_id={context.CorrelationId:D}");
            return Error(400, "invalid_mapping_query", "Current mapping query is invalid.", context.CorrelationId, headers);
        }
        catch (InvalidOperationException)
        {
            log($"resolve_current_party outcome=dependency_degraded correlation_id={context.CorrelationId:D}");
            return Error(503, "mapping_dependency_degraded", "Mapping dependency is unavailable.", context.CorrelationId, headers);
        }
    }

    private static MappingApiResponse From(CurrentMapping mapping) =>
        new(
            mapping.PartyId,
            mapping.TenantId,
            mapping.SourceSystem,
            mapping.SourceKey,
            mapping.EffectiveFrom,
            mapping.EffectiveTo,
            mapping.Status,
            mapping.RuleVersion,
            mapping.ProvenanceReference);

    private static MappingHttpResponse Error(
        int statusCode,
        string code,
        string message,
        Guid correlationId,
        IReadOnlyDictionary<string, string> headers) =>
        new(statusCode, headers, new MappingApiError(code, message, correlationId));
}





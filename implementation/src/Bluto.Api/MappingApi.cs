using System.Text.Json.Serialization;
using Bluto.Application.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bluto.Api.Mapping;

public sealed record MappingRequestContext(
    bool IsAuthenticated,
    Guid? TenantId,
    IReadOnlySet<Guid> AuthorizedTenantIds,
    Guid CorrelationId,
    Guid RequestId = default);

public sealed record MappingHttpResponse(
    int StatusCode,
    IReadOnlyDictionary<string, string> Headers,
    object? Body);

public sealed record MappingApiMappingResult(
    [property: JsonPropertyName("party_id")] Guid PartyId,
    [property: JsonPropertyName("tenant_id")] Guid TenantId,
    [property: JsonPropertyName("source_system")] string SourceSystem,
    [property: JsonPropertyName("source_key")] string SourceKey,
    [property: JsonPropertyName("effective_from")] DateTimeOffset EffectiveFrom,
    [property: JsonPropertyName("effective_to")] DateTimeOffset? EffectiveTo,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("rule_version")] string RuleVersion,
    [property: JsonPropertyName("provenance_reference")] string ProvenanceReference);

public sealed record MappingApiResponse(
    [property: JsonPropertyName("request_id")] Guid RequestId,
    [property: JsonPropertyName("correlation_id")] Guid CorrelationId,
    [property: JsonPropertyName("tenant_id")] Guid TenantId,
    [property: JsonPropertyName("query_type")] string QueryType,
    [property: JsonPropertyName("schema_version")] string SchemaVersion,
    [property: JsonPropertyName("data_classification")] string DataClassification,
    [property: JsonPropertyName("result")] MappingApiMappingResult Result)
{
    [JsonIgnore]
    public string Status => Result.Status;
}
public sealed record MappingApiErrorEnvelope(
    [property: JsonPropertyName("request_id")] Guid RequestId,
    [property: JsonPropertyName("correlation_id")] Guid CorrelationId,
    [property: JsonPropertyName("tenant_id")] Guid TenantId,
    [property: JsonPropertyName("query_type")] string QueryType,
    [property: JsonPropertyName("schema_version")] string SchemaVersion,
    [property: JsonPropertyName("data_classification")] string DataClassification,
    [property: JsonPropertyName("error")] MappingApiError Error);

public sealed record MappingApiError(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("retryable")] bool Retryable,
    [property: JsonPropertyName("support_reference")] string SupportReference);

public interface IBlutoAuthorizationPolicy
{
    MappingRequestContext CreateRequestContext(HttpContext httpContext, string operation, string resource);
}

public sealed class ClaimsBlutoAuthorizationPolicy : IBlutoAuthorizationPolicy
{
    public MappingRequestContext CreateRequestContext(HttpContext httpContext, string operation, string resource)
    {
        var correlationId = ResolveGuidHeader(httpContext, "x-correlation-id");
        var requestId = ResolveGuidHeader(httpContext, "x-request-id");
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
            correlationId,
            requestId);
    }

    private static Guid ResolveGuidHeader(HttpContext httpContext, string headerName) =>
        httpContext.Request.Headers.TryGetValue(headerName, out var values)
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
        services.AddScoped<CurrentMappingQueryService>();
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
            ["x-correlation-id"] = context.CorrelationId.ToString("D"),
            ["x-request-id"] = EffectiveRequestId(context).ToString("D")
        };

        if (!context.IsAuthenticated)
        {
            log($"resolve_current_party outcome=unauthenticated correlation_id={context.CorrelationId:D}");
            return Error(401, "AUTHENTICATION_REQUIRED", "AUTHENTICATION_REQUIRED", "Authentication is required.", retryable: false, context, headers);
        }

        if (context.TenantId is null || context.TenantId == Guid.Empty)
        {
            log($"resolve_current_party outcome=tenant_scope_missing correlation_id={context.CorrelationId:D}");
            return Error(403, "SCOPE_FORBIDDEN_CONCEALED", "SCOPE_FORBIDDEN", "Tenant scope is not authorized.", retryable: false, context, headers);
        }

        if (!context.AuthorizedTenantIds.Contains(context.TenantId.Value))
        {
            log($"resolve_current_party outcome=concealed correlation_id={context.CorrelationId:D}");
            return Error(404, "SCOPE_FORBIDDEN_CONCEALED", "SCOPE_FORBIDDEN", "Mapping was not found.", retryable: false, context, headers);
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
                return Error(404, "MAPPING_NOT_FOUND", "RESOURCE_NOT_FOUND", "Mapping was not found.", retryable: false, context, headers);
            }

            log($"resolve_current_party outcome=found correlation_id={context.CorrelationId:D}");
            return new MappingHttpResponse(200, headers, Success(EffectiveRequestId(context), context.CorrelationId, mapping));
        }
        catch (UnauthorizedAccessException)
        {
            log($"resolve_current_party outcome=concealed correlation_id={context.CorrelationId:D}");
            return Error(404, "SCOPE_FORBIDDEN_CONCEALED", "SCOPE_FORBIDDEN", "Mapping was not found.", retryable: false, context, headers);
        }
        catch (ArgumentException)
        {
            log($"resolve_current_party outcome=invalid_request correlation_id={context.CorrelationId:D}");
            return Error(400, "REQUEST_INVALID", "REQUEST_INVALID", "Current mapping query is invalid.", retryable: false, context, headers);
        }
        catch (InvalidOperationException)
        {
            log($"resolve_current_party outcome=dependency_degraded correlation_id={context.CorrelationId:D}");
            return Error(503, "DEPENDENCY_UNAVAILABLE", "DEPENDENCY_UNAVAILABLE", "Mapping dependency is unavailable.", retryable: true, context, headers);
        }
    }

    private static MappingApiResponse Success(Guid requestId, Guid correlationId, CurrentMapping mapping) =>
        new(
            requestId,
            correlationId,
            mapping.TenantId,
            "ResolveCurrentParty",
            "1.0.0",
            "Restricted",
            From(mapping));

    private static MappingApiMappingResult From(CurrentMapping mapping) =>
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

    private static Guid EffectiveRequestId(MappingRequestContext context) =>
        context.RequestId == Guid.Empty ? context.CorrelationId : context.RequestId;
    private static MappingHttpResponse Error(
        int statusCode,
        string code,
        string category,
        string message,
        bool retryable,
        MappingRequestContext context,
        IReadOnlyDictionary<string, string> headers) =>
        new(
            statusCode,
            headers,
            new MappingApiErrorEnvelope(
                EffectiveRequestId(context),
                context.CorrelationId,
                context.TenantId ?? Guid.Empty,
                "ResolveCurrentParty",
                "1.0.0",
                "Restricted",
                new MappingApiError(code, category, message, retryable, $"support-{context.CorrelationId:D}")));
}




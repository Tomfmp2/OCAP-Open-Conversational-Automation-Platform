using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OCAP.Api.Swagger;

/// <summary>
/// Enriquece operaciones OpenAPI con respuestas RFC7807 y cabeceras de correlación.
/// </summary>
public sealed class ProblemDetailsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.TryAdd("400", CreateProblem("Solicitud inválida / validación"));
        operation.Responses.TryAdd("401", CreateProblem("No autenticado"));
        operation.Responses.TryAdd("403", CreateProblem("Prohibido"));
        operation.Responses.TryAdd("404", CreateProblem("No encontrado"));
        operation.Responses.TryAdd("422", CreateProblem("Error de negocio"));
        operation.Responses.TryAdd("500", CreateProblem("Error interno"));

        operation.Parameters ??= new List<OpenApiParameter>();
        if (operation.Parameters.All(p => p.Name != "X-Correlation-Id"))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Correlation-Id",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
                Description = "Identificador de correlación de extremo a extremo."
            });
        }

        if (operation.Parameters.All(p => p.Name != "X-Api-Version"))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Api-Version",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema { Type = "string", Example = new OpenApiString("1.0") },
                Description = "Versión de API (por defecto 1.0)."
            });
        }
    }

    private static OpenApiResponse CreateProblem(string description) => new()
    {
        Description = description,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/problem+json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(ProblemDetails)
                    }
                },
                Example = new OpenApiObject
                {
                    ["type"] = new OpenApiString("https://httpstatuses.com/400"),
                    ["title"] = new OpenApiString("Validación fallida"),
                    ["status"] = new OpenApiInteger(400),
                    ["detail"] = new OpenApiString("Uno o más campos no son válidos."),
                    ["instance"] = new OpenApiString("/api/example"),
                    ["correlationId"] = new OpenApiString("abc123"),
                    ["requestId"] = new OpenApiString("req-1")
                }
            }
        }
    };
}

public sealed class TagDescriptionsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new() { Name = "Auth", Description = "Login, refresh y logout JWT" },
            new() { Name = "Health", Description = "Salud y diagnóstico" },
            new() { Name = "Knowledge", Description = "RAG / knowledge base" },
            new() { Name = "Workflows", Description = "Definiciones y ejecuciones" },
            new() { Name = "Security", Description = "RBAC, MFA, vault" }
        };
    }
}

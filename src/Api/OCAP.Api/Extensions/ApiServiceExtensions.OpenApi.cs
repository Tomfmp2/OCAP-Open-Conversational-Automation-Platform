using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace OCAP.Api.Extensions;

public static partial class ApiServiceExtensions
{
    /// <summary>Documento OpenAPI con esquema Bearer y ProblemDetails.</summary>
    private static void AddOpenApiDocumentation(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "OCAP API",
                Version = "v1",
                Description = "Open Conversational Automation Platform — API Gateway (RFC 7807 ProblemDetails, API v1.0)"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            c.MapType<ProblemDetails>(() => new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["type"] = new OpenApiSchema { Type = "string" },
                    ["title"] = new OpenApiSchema { Type = "string" },
                    ["status"] = new OpenApiSchema { Type = "integer" },
                    ["detail"] = new OpenApiSchema { Type = "string" },
                    ["instance"] = new OpenApiSchema { Type = "string" },
                    ["correlationId"] = new OpenApiSchema { Type = "string" },
                    ["requestId"] = new OpenApiSchema { Type = "string" }
                }
            });

            c.OperationFilter<OCAP.Api.Swagger.ProblemDetailsOperationFilter>();
            c.DocumentFilter<OCAP.Api.Swagger.TagDescriptionsDocumentFilter>();

            var xmlPath = Path.Combine(AppContext.BaseDirectory, "OCAP.Api.xml");
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        });
    }
}

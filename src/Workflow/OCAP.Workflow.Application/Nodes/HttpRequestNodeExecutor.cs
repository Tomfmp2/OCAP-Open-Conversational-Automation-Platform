using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Application.Nodes;

public class HttpRequestNodeExecutor : IWorkflowNodeExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpRequestNodeExecutor> _logger;

    private static readonly string[] AllowedMethods = { "GET", "POST", "PUT", "PATCH", "DELETE" };

    public HttpRequestNodeExecutor(IHttpClientFactory httpClientFactory, ILogger<HttpRequestNodeExecutor> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.ApiRequest;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var config = JsonSerializer.Deserialize<HttpRequestNodeConfiguration>(step.ConfigurationJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                         ?? new HttpRequestNodeConfiguration();

            string url = ReplaceVariables(config.Url ?? string.Empty, context.Variables);
            string method = (config.Method ?? "GET").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(url))
            {
                var errJson = JsonSerializer.Serialize(new { error = "URL vacía o no configurada." });
                return new NodeExecutionResult(false, string.Empty, errJson, "URL de la petición HTTP no especificada.");
            }

            if (!AllowedMethods.Contains(method))
            {
                var errJson = JsonSerializer.Serialize(new { error = $"Método HTTP no soportado: {method}. Permitidos: GET, POST, PUT, PATCH, DELETE." });
                return new NodeExecutionResult(false, string.Empty, errJson, $"Método HTTP {method} no soportado.");
            }

            string body = ReplaceVariables(config.Body ?? string.Empty, context.Variables);

            _logger.LogInformation("Ejecutando petición HTTP [{Method}] a {Url} (TenantId: {TenantId})", method, url, context.TenantId);

            using var request = new HttpRequestMessage(new HttpMethod(method), url);

            if (!string.IsNullOrWhiteSpace(body) && method != "GET")
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            if (config.Headers != null && config.Headers.Count > 0)
            {
                foreach (var header in config.Headers)
                {
                    var headerValue = ReplaceVariables(header.Value, context.Variables);

                    if (IsSensitiveHeader(header.Key))
                    {
                        _logger.LogInformation("Header sensible configurado: {HeaderKey} = ***", header.Key);
                    }
                    else
                    {
                        _logger.LogInformation("Header configurado: {HeaderKey} = {HeaderValue}", header.Key, headerValue);
                    }

                    if (!request.Headers.TryAddWithoutValidation(header.Key, headerValue))
                    {
                        request.Content?.Headers.TryAddWithoutValidation(header.Key, headerValue);
                    }
                }
            }

            var client = _httpClientFactory.CreateClient("HttpRequestNode");
            var timeoutSeconds = config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 30;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("La petición HTTP a {Url} excedió el timeout de {Timeout} segundos.", url, timeoutSeconds);
                var timeoutError = new
                {
                    error = $"Excedido tiempo de espera (Timeout: {timeoutSeconds}s)",
                    errorType = "Timeout",
                    url = url,
                    method = method
                };
                return new NodeExecutionResult(false, string.Empty, JsonSerializer.Serialize(timeoutError), $"Timeout de {timeoutSeconds}s alcanzado.");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            _logger.LogInformation("Respuesta HTTP {StatusCode} recibida de {Url}", (int)response.StatusCode, url);

            var outputObj = new
            {
                statusCode = (int)response.StatusCode,
                isSuccessStatusCode = response.IsSuccessStatusCode,
                body = responseContent,
                headers = responseHeaders
            };

            var outputJson = JsonSerializer.Serialize(outputObj);

            if (!response.IsSuccessStatusCode && config.FailOnErrorCode)
            {
                return new NodeExecutionResult(false, string.Empty, outputJson, $"HTTP request failed with status code {(int)response.StatusCode} ({response.StatusCode}).");
            }

            return new NodeExecutionResult(true, "next", outputJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar HttpRequestNodeExecutor para el paso {StepId}", step.StepId);
            var errorOutput = JsonSerializer.Serialize(new
            {
                error = ex.Message,
                errorType = ex.GetType().Name
            });
            return new NodeExecutionResult(false, string.Empty, errorOutput, ex.Message);
        }
    }

    private string ReplaceVariables(string template, IDictionary<string, object>? variables)
    {
        if (string.IsNullOrWhiteSpace(template) || variables == null || variables.Count == 0)
            return template;

        return Regex.Replace(template, @"\{\{(.+?)\}\}", match =>
        {
            var varPath = match.Groups[1].Value.Trim();
            var resolved = ResolveVariableValue(varPath, variables);
            return resolved ?? match.Value;
        });
    }

    private string? ResolveVariableValue(string path, IDictionary<string, object> variables)
    {
        if (variables.TryGetValue(path, out var directVal))
        {
            return FormatValue(directVal);
        }

        var parts = path.Split('.');
        if (parts.Length > 1 && variables.TryGetValue(parts[0], out var rootVal))
        {
            object? current = rootVal;
            for (int i = 1; i < parts.Length; i++)
            {
                if (current == null) return null;

                string part = parts[i];
                if (current is JsonElement elem)
                {
                    if (elem.ValueKind == JsonValueKind.Object && elem.TryGetProperty(part, out var childElem))
                    {
                        current = childElem;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (current is IDictionary<string, object> dict)
                {
                    if (dict.TryGetValue(part, out var nextVal))
                    {
                        current = nextVal;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            return FormatValue(current);
        }

        return null;
    }

    private string? FormatValue(object? val)
    {
        if (val == null) return string.Empty;
        if (val is JsonElement jsonElem)
        {
            return jsonElem.ValueKind switch
            {
                JsonValueKind.String => jsonElem.GetString(),
                JsonValueKind.Null => string.Empty,
                _ => jsonElem.GetRawText()
            };
        }
        return val.ToString();
    }

    private bool IsSensitiveHeader(string headerKey)
    {
        var key = headerKey.ToLowerInvariant();
        return key.Contains("authorization") || key.Contains("secret") || key.Contains("key") || key.Contains("token") || key.Contains("password");
    }
}

public class HttpRequestNodeConfiguration
{
    public string? Url { get; set; }
    public string? Method { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool FailOnErrorCode { get; set; } = true;
}


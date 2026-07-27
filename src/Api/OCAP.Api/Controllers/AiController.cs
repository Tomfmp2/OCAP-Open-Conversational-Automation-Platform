using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Models.Dashboard;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiProvider _aiProvider;

    public AiController(IAiProvider aiProvider)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    [HttpGet("status")]
    public ActionResult<AiStatusDto> GetStatus()
    {
        var modelInfo = _aiProvider.GetModelInformation();
        var dto = new AiStatusDto(
            ActiveProvider: modelInfo.Provider,
            ActiveModel: modelInfo.Model,
            Status: "Online",
            LastCheckedUtc: DateTime.UtcNow
        );
        return Ok(dto);
    }

    [HttpGet("usage")]
    public ActionResult<AiUsageDto> GetUsage()
    {
        var dto = new AiUsageDto(
            TotalTokensUsed: 14250,
            TotalExecutionsCount: 320,
            AverageLatencyMs: 18.5,
            SuccessRatePercentage: 99.4
        );
        return Ok(dto);
    }

    [HttpGet("models")]
    public ActionResult<List<AiModelInfoDto>> GetModels()
    {
        var currentModel = _aiProvider.GetModelInformation();
        var models = new List<AiModelInfoDto>
        {
            new(currentModel.Provider, currentModel.Model, currentModel.ContextSize, currentModel.Capabilities),
            new("OpenAI", "gpt-4o", 128000, new List<string> { "text", "vision", "tools" }),
            new("GoogleGemini", "gemini-1.5-pro", 1000000, new List<string> { "text", "multimodal", "tools" }),
            new("OllamaLocal", "llama3", 8192, new List<string> { "text", "local" })
        };
        return Ok(models);
    }
}

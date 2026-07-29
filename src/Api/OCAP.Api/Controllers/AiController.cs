using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Api.Models.Dashboard;
using OCAP.Intelligence.Abstractions;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiProvider _aiProvider;
    private readonly IAiProviderSelector _selector;
    private readonly IAiProviderRegistry _registry;
    private readonly OCAPDbContext _dbContext;

    public AiController(
        IAiProvider aiProvider, 
        IAiProviderSelector selector,
        IAiProviderRegistry registry,
        OCAPDbContext dbContext)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("status")]
    public ActionResult<AiStatusDto> GetStatus()
    {
        var activeName = _selector.ActiveProviderName;
        var activeProvider = _registry.GetProvider(activeName);
        var modelInfo = activeProvider?.GetModelInformation() ?? _aiProvider.GetModelInformation();
        
        var dto = new AiStatusDto(
            ActiveProvider: modelInfo.Provider,
            ActiveModel: modelInfo.Model,
            Status: "Online",
            LastCheckedUtc: DateTime.UtcNow
        );
        return Ok(dto);
    }

    [HttpGet("usage")]
    public async Task<ActionResult<AiUsageDto>> GetUsage(CancellationToken cancellationToken)
    {
        var logs = await _dbContext.AiExecutionLogs.ToListAsync(cancellationToken);
        
        var totalTokens = logs.Sum(l => l.Tokens);
        var totalExecutions = logs.Count;
        var avgLatency = logs.Any() ? logs.Average(l => l.ExecutionTimeMs) : 0.0;
        var successCount = logs.Count(l => l.Success);
        var successRate = logs.Any() ? (successCount / (double)totalExecutions) * 100.0 : 100.0;

        var dto = new AiUsageDto(
            TotalTokensUsed: totalTokens,
            TotalExecutionsCount: totalExecutions,
            AverageLatencyMs: avgLatency,
            SuccessRatePercentage: successRate
        );
        return Ok(dto);
    }

    [HttpGet("models")]
    public ActionResult<List<AiModelInfoDto>> GetModels()
    {
        var models = new List<AiModelInfoDto>();
        var names = _registry.GetRegisteredProviderNames();
        
        foreach (var name in names)
        {
            var provider = _registry.GetProvider(name);
            if (provider != null)
            {
                var info = provider.GetModelInformation();
                models.Add(new AiModelInfoDto(info.Provider, info.Model, info.ContextSize, info.Capabilities));
            }
        }

        if (!models.Any())
        {
            var currentModel = _aiProvider.GetModelInformation();
            models.Add(new AiModelInfoDto(currentModel.Provider, currentModel.Model, currentModel.ContextSize, currentModel.Capabilities));
        }

        return Ok(models);
    }
}

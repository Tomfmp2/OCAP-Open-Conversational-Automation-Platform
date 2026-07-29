namespace OCAP.Core.Ports.Billing;

public record SubscriptionPlan(string Id, string Name, decimal PriceMonthly, decimal PriceYearly, Dictionary<string, int> Quotas, List<string> Features);

public record CustomerSubscription(Guid TenantId, string SubscriptionId, string PlanId, string Status, DateTime CurrentPeriodStart, DateTime CurrentPeriodEnd, bool CancelAtPeriodEnd);

public record UsageMetric(Guid TenantId, string MetricName, long Quantity, DateTime Timestamp);

public record EntitlementCheckResult(bool IsAllowed, string MetricName, long CurrentUsage, long Limit);

public interface IBillingProvider
{
    Task<string> CreateCustomerAsync(Guid tenantId, string email, string name, CancellationToken cancellationToken = default);
    Task<CustomerSubscription> CreateSubscriptionAsync(Guid tenantId, string planId, string paymentMethodId, CancellationToken cancellationToken = default);
    Task<CustomerSubscription> CancelSubscriptionAsync(Guid tenantId, string subscriptionId, CancellationToken cancellationToken = default);
}

public interface ISubscriptionService
{
    Task<CustomerSubscription?> GetSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionPlan>> GetAvailablePlansAsync(CancellationToken cancellationToken = default);
    Task<bool> UpgradePlanAsync(Guid tenantId, string newPlanId, CancellationToken cancellationToken = default);
}

public interface IInvoiceService
{
    Task<IReadOnlyList<object>> GetInvoicesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadInvoicePdfAsync(Guid tenantId, string invoiceId, CancellationToken cancellationToken = default);
}

public interface IUsageMeteringService
{
    Task RecordUsageAsync(Guid tenantId, string metricName, long quantity, CancellationToken cancellationToken = default);
    Task<long> GetCurrentUsageAsync(Guid tenantId, string metricName, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);
}

public interface IEntitlementService
{
    Task<EntitlementCheckResult> CheckEntitlementAsync(Guid tenantId, string featureOrMetric, long requestedQuantity = 1, CancellationToken cancellationToken = default);
}

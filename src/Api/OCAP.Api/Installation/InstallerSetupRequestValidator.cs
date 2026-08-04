using FluentValidation;

namespace OCAP.Api.Installation;

public sealed class InstallerSetupRequestValidator : AbstractValidator<InstallerSetupRequest>
{
    public InstallerSetupRequestValidator()
    {
        RuleFor(x => x.Target)
            .Must(IsKnownTarget)
            .WithMessage("Target debe ser Dev, Local o Web.");

        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(10);
        RuleFor(x => x.TenantName).NotEmpty();
        RuleFor(x => x.TenantSlug).NotEmpty().Matches("^[a-z0-9-]+$")
            .WithMessage("TenantSlug solo admite minúsculas, números y guiones.");

        RuleFor(x => x.AiProvider).NotEmpty();
        RuleFor(x => x.AiModelName).NotEmpty();

        // Dev: API key opcional (puede usar Mock / clave ya en .env).
        // Local/Web: obligatoria salvo Ollama.
        When(x => !IsDev(x.Target) &&
                  !string.Equals(x.AiProvider, "Ollama", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.AiApiKey).NotEmpty()
                .WithMessage("AiApiKey es obligatorio salvo para Ollama o target Dev.");
        });

        When(x => string.Equals(x.Target, "Web", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.PublicApiUrl).NotEmpty().Must(BeAbsoluteHttpUrl)
                .WithMessage("PublicApiUrl debe ser una URL http(s) absoluta.");
            RuleFor(x => x.PublicPanelUrl).NotEmpty().Must(BeAbsoluteHttpUrl)
                .WithMessage("PublicPanelUrl debe ser una URL http(s) absoluta.");
            RuleFor(x => x.PostgresHost).NotEmpty();
            RuleFor(x => x.PostgresDbName).NotEmpty();
            RuleFor(x => x.PostgresUsername).NotEmpty();
            RuleFor(x => x.PostgresPassword).NotEmpty().MinimumLength(8);
            RuleFor(x => x.PostgresPort).InclusiveBetween(1, 65535);
        });

        When(x => x.EnableGoogleWorkspace, () =>
        {
            RuleFor(x => x.GoogleClientId).NotEmpty();
            RuleFor(x => x.GoogleClientSecret).NotEmpty();
        });

        When(x => x.EnableWhatsApp, () =>
        {
            RuleFor(x => x.EvolutionApiUrl).NotEmpty();
            RuleFor(x => x.EvolutionApiKey).NotEmpty();
        });

        When(x => x.EnableTelegram, () =>
        {
            RuleFor(x => x.TelegramBotToken).NotEmpty();
        });
    }

    private static bool IsKnownTarget(string? t) =>
        string.Equals(t, "Dev", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(t, "Local", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(t, "Web", StringComparison.OrdinalIgnoreCase);

    private static bool IsDev(string? t) =>
        string.Equals(t, "Dev", StringComparison.OrdinalIgnoreCase);

    private static bool BeAbsoluteHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

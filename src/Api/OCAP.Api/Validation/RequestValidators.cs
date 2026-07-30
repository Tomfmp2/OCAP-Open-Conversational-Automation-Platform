using FluentValidation;
using OCAP.Api.Controllers;
using OCAP.Api.DTOs.Requests;
using OCAP.Api.Models.Common;
using OCAP.Api.Models.Security;

namespace OCAP.Api.Validation;

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}

public sealed class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MinimumLength(16).MaximumLength(2048);
    }
}

public sealed class CreateTenantRequestDtoValidator : AbstractValidator<CreateTenantRequestDto>
{
    public CreateTenantRequestDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug debe ser kebab-case (a-z, 0-9, guiones).");
    }
}

public sealed class IncomingMessageRequestValidator : AbstractValidator<IncomingMessageRequest>
{
    public IncomingMessageRequestValidator()
    {
        RuleFor(x => x.UserId).NotNull().Must(id => id != Guid.Empty)
            .WithMessage("El ID de usuario es obligatorio.");
        RuleFor(x => x.MessageContent).NotEmpty().MaximumLength(16_000);
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(100);
    }
}

public sealed class ConnectIntegrationRequestDtoValidator : AbstractValidator<ConnectIntegrationRequestDto>
{
    public ConnectIntegrationRequestDtoValidator()
    {
        RuleFor(x => x.AuthCode).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.RedirectUri).MaximumLength(2048).When(x => !string.IsNullOrEmpty(x.RedirectUri));
        RuleFor(x => x.Scopes).MaximumLength(2048).When(x => !string.IsNullOrEmpty(x.Scopes));
    }
}

public sealed class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    public PagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.SortDirection)
            .Must(d => d is "asc" or "desc" or "ASC" or "DESC")
            .When(x => !string.IsNullOrWhiteSpace(x.SortDirection));
        RuleFor(x => x.Search).MaximumLength(500).When(x => x.Search is not null);
        RuleFor(x => x.SortBy).MaximumLength(100).When(x => x.SortBy is not null);
    }
}

public sealed class CreateChannelConnectionRequestValidator : AbstractValidator<CreateChannelConnectionRequest>
{
    public CreateChannelConnectionRequestValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Credentials).NotEmpty().MaximumLength(16_384);
    }
}

public sealed class CreateWebhookRequestDtoValidator : AbstractValidator<CreateWebhookRequestDto>
{
    public CreateWebhookRequestDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TargetUrl).NotEmpty().Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            .WithMessage("TargetUrl debe ser una URL absoluta http(s).");
        RuleFor(x => x.Secret).NotEmpty().MinimumLength(16).MaximumLength(512);
        RuleFor(x => x.SubscribedEvents).NotNull();
    }
}

public sealed class CreateApiKeyRequestDtoValidator : AbstractValidator<CreateApiKeyRequestDto>
{
    public CreateApiKeyRequestDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

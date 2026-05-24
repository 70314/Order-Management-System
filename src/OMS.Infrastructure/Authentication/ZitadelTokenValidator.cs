using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OMS.Application.Interfaces;

namespace OMS.Infrastructure.Authentication;

public class ZitadelTokenValidator : IZitadelTokenValidator {
    private readonly ZitadelOptions _options;
    private readonly ILogger<ZitadelTokenValidator> _logger;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;

    public ZitadelTokenValidator(IOptions<ZitadelOptions> options, ILogger<ZitadelTokenValidator> logger) {
        _options = options.Value;
        _logger = logger;

        var authority = _options.Authority.TrimEnd('/');
        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{authority}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
    }

    public async Task<ZitadelTokenValidationResult> ValidateTokenAsync(string accessToken) {
        try {
            var config = await _configManager.GetConfigurationAsync(CancellationToken.None);

            var validationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                ValidIssuer = _options.Authority.TrimEnd('/'),
                ValidateAudience = _options.ValidateAudience,
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(accessToken, validationParameters, out var validatedToken);

            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal.FindFirst("email")?.Value;
            var name = principal.FindFirst(ClaimTypes.Name)?.Value
                       ?? principal.FindFirst("name")?.Value
                       ?? principal.FindFirst("preferred_username")?.Value;
            var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? principal.FindFirst("sub")?.Value;

            _logger.LogInformation("Zitadel token validated for subject: {Subject}, email: {Email}", subject, email);

            return new ZitadelTokenValidationResult {
                IsValid = true,
                Email = email,
                Name = name,
                Subject = subject,
                Claims = principal.Claims
            };
        } catch (SecurityTokenExpiredException ex) {
            _logger.LogWarning("Zitadel token expired: {Message}", ex.Message);
            return new ZitadelTokenValidationResult {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        } catch (SecurityTokenInvalidSignatureException ex) {
            _logger.LogWarning("Zitadel token has invalid signature: {Message}", ex.Message);
            return new ZitadelTokenValidationResult {
                IsValid = false,
                ErrorMessage = "Invalid token signature"
            };
        } catch (Exception ex) {
            _logger.LogError(ex, "Zitadel token validation failed");
            return new ZitadelTokenValidationResult {
                IsValid = false,
                ErrorMessage = $"Token validation failed: {ex.Message}"
            };
        }
    }
}

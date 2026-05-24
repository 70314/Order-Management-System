using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OMS.Domain.Interfaces;

namespace OMS.API.Middleware;

public class TokenValidationMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenValidationMiddleware> _logger;
    private readonly bool _useZitadelAuth;

    public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger, IConfiguration configuration) {
        _next = next;
        _logger = logger;
        _useZitadelAuth = configuration.GetValue<bool>("Authentication:UseZitadelAuth"); // TODO: Will move these flags in feature section
    }

    public async Task InvokeAsync(HttpContext context) {
        var authHeader = context.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ")) {
            var tokenString = authHeader.Substring(7);
            var handler = new JwtSecurityTokenHandler();

            try {
                if (handler.CanReadToken(tokenString)) {
                    var jwtToken = handler.ReadJwtToken(tokenString);

                    if (!_useZitadelAuth) {
                        // In-app mode: Verify expiration manually (belt-and-suspenders with JWT middleware)
                        if (jwtToken.ValidTo < DateTime.UtcNow) {
                            _logger.LogWarning("Token validation failed: Token expired at {Exp}", jwtToken.ValidTo);
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new { error = "Token has expired", statusCode = 401 });
                            return;
                        }
                    }
                    // In Zitadel mode, expiration is already validated by the JWT Bearer middleware

                    // Get userEmail from token (handle both standard and Zitadel claim names)
                    var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;

                    if (string.IsNullOrEmpty(email)) {
                        _logger.LogWarning("Token validation failed: Email claim missing");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid token claims", statusCode = 401 });
                        return;
                    }

                    // Validate user exists in database
                    var uow = context.RequestServices.GetRequiredService<IUnitOfWork>();
                    var users = await uow.Users.FindAsync(u => u.Email == email);

                    if (!users.Any()) {
                        _logger.LogWarning("Token validation failed: User {Email} not found in database", email);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { error = "User account is invalid or no longer exists", statusCode = 401 });
                        return;
                    }

                    _logger.LogInformation("Token validated for user: {Email}", email);
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "Token validation failed with error");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid token format", statusCode = 401 });
                return;
            }
        }

        await _next(context);
    }
}

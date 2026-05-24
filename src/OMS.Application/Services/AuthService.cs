using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OMS.Application.DTOs;
using OMS.Application.Interfaces;
using OMS.Domain.Entities;
using OMS.Domain.Enums;
using OMS.Domain.Interfaces;

namespace OMS.Application.Services;

public class AuthService : IAuthService {
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly IZitadelTokenValidator? _zitadelValidator;

    public AuthService(IUnitOfWork uow, IConfiguration config, ILogger<AuthService> logger, IZitadelTokenValidator? zitadelValidator = null) {
        _uow = uow;
        _config = config;
        _logger = logger;
        _zitadelValidator = zitadelValidator;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request) {
        var users = await _uow.Users.FindAsync(u => u.Email == request.Email);
        var user = users.FirstOrDefault();

        if (user == null || user.PasswordHash == null || !VerifyPassword(request.Password, user.PasswordHash)) {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        _logger.LogInformation("User {Email} logged in successfully", request.Email);
        return GenerateToken(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request) {
        var existing = await _uow.Users.FindAsync(u => u.Email == request.Email);
        if (existing.Any())
            throw new InvalidOperationException("A user with this email already exists.");

        var user = new User {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = UserRole.User
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email}", request.Email);
        return GenerateToken(user);
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request) {
        // In production, validate the ID token with Google's API
        // For now, we decode the JWT claims (simplified)
        var handler = new JwtSecurityTokenHandler();

        try {
            var jwt = handler.ReadJwtToken(request.IdToken);
            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            var googleId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                throw new UnauthorizedAccessException("Invalid Google token.");

            var users = await _uow.Users.FindAsync(u => u.GoogleId == googleId || u.Email == email);
            var user = users.FirstOrDefault();

            if (user == null) {
                user = new User {
                    Email = email,
                    Name = name ?? email,
                    GoogleId = googleId,
                    Role = UserRole.User
                };
                await _uow.Users.AddAsync(user);
                await _uow.SaveChangesAsync();
                _logger.LogInformation("New Google user registered: {Email}", email);
            } else if (user.GoogleId == null) {
                user.GoogleId = googleId;
                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();
            }

            return GenerateToken(user);
        } catch (Exception ex) when (ex is not UnauthorizedAccessException) {
            _logger.LogError(ex, "Google login failed");
            throw new UnauthorizedAccessException("Invalid Google token.");
        }
    }

    /// <summary>
    /// Validate with Zitadel
    /// If user not exist in datase then add user
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public async Task<AuthResponse> ZitadelLoginAsync(ZitadelLoginRequest request) {
        if (_zitadelValidator == null)
            throw new InvalidOperationException("Zitadel authentication is not configured.");

        var validationResult = await _zitadelValidator.ValidateTokenAsync(request.AccessToken);

        if (!validationResult.IsValid)
            throw new UnauthorizedAccessException($"Invalid Zitadel token: {validationResult.ErrorMessage}");

        var email = validationResult.Email;
        var name = validationResult.Name;
        var zitadelId = validationResult.Subject;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(zitadelId))
            throw new UnauthorizedAccessException("Zitadel token is missing required claims (email, sub).");

        // Find existing user by ZitadelId or email
        var users = await _uow.Users.FindAsync(u => u.ZitadelId == zitadelId || u.Email == email);
        var user = users.FirstOrDefault();

        if (user == null) {
            // Auto-provision new user from Zitadel claims
            user = new User {
                Email = email,
                Name = name ?? email,
                ZitadelId = zitadelId,
                Role = UserRole.User
            };
            await _uow.Users.AddAsync(user);
            await _uow.SaveChangesAsync();
            _logger.LogInformation("New Zitadel user provisioned: {Email} (ZitadelId: {ZitadelId})", email, zitadelId);
        } else if (user.ZitadelId == null) {
            // Link existing user to Zitadel account
            user.ZitadelId = zitadelId;
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Linked existing user {Email} to Zitadel account {ZitadelId}", email, zitadelId);
        }

        _logger.LogInformation("Zitadel user {Email} authenticated successfully", email);

        // Return the Zitadel token directly (no OMS token exchange)
        return new AuthResponse {
            Token = request.AccessToken,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            Expiration = DateTime.UtcNow.AddHours(8), // Zitadel token expiry is validated by middleware
            AuthProvider = "Zitadel"
        };
    }

    private AuthResponse GenerateToken(User user) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? "OMS_SuperSecretKey_2026_MustBe32Chars!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddHours(8);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "OMS",
            audience: _config["Jwt:Audience"] ?? "OMS",
            claims: claims,
            expires: expiration,
            signingCredentials: creds
        );

        return new AuthResponse {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            Expiration = expiration,
            AuthProvider = "InApp"
        };
    }

    private static string HashPassword(string password) {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "OMS_SALT_2026"));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash) {
        return HashPassword(password) == hash;
    }
}

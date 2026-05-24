using System.Security.Claims;

namespace OMS.Application.Interfaces;

public class ZitadelTokenValidationResult {
    public bool IsValid { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<Claim> Claims { get; set; } = Enumerable.Empty<Claim>();
}

public interface IZitadelTokenValidator {
    Task<ZitadelTokenValidationResult> ValidateTokenAsync(string accessToken);
}

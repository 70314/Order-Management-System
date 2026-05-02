using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace OMS.Web.Authentication;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _protectedLocalStorage;
    private const string TokenKey = "authToken";

    public CustomAuthenticationStateProvider(ProtectedLocalStorage protectedLocalStorage)
    {
        _protectedLocalStorage = protectedLocalStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // ProtectedLocalStorage gracefully handles pre-rendering by returning Success = false
            // when JavaScript interop is not yet available.
            var result = await _protectedLocalStorage.GetAsync<string>(TokenKey);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Value))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            return CreateAuthenticationState(result.Value);
        }
        catch (Exception)
        {
            // If anything goes wrong (e.g. during static rendering), return an unauthenticated state
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await _protectedLocalStorage.SetAsync(TokenKey, token);
        var authState = CreateAuthenticationState(token);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _protectedLocalStorage.DeleteAsync(TokenKey);
        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    private AuthenticationState CreateAuthenticationState(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt", System.Security.Claims.ClaimTypes.Name, System.Security.Claims.ClaimTypes.Role);
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
}

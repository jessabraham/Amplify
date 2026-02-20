using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Amplify.Web.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private string? _token;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public AuthStateProvider(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public string? Token => _token;

    public async Task SetTokenAsync(string token)
    {
        _token = token;
        await _sessionStorage.SetAsync("authToken", token);
        var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task ClearTokenAsync()
    {
        _token = null;
        await _sessionStorage.DeleteAsync("authToken");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            try
            {
                var result = await _sessionStorage.GetAsync<string>("authToken");
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    _token = result.Value;
                }
            }
            catch
            {
                // ProtectedSessionStorage not available during prerender
            }
        }

        if (string.IsNullOrEmpty(_token))
            return new AuthenticationState(_anonymous);

        var identity = new ClaimsIdentity(ParseClaims(_token), "jwt");
        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    private static IEnumerable<Claim> ParseClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims;
    }

    public async Task LogoutAsync()
    {
        _token = null;
        try
        {
            await _sessionStorage.DeleteAsync("authToken");
        }
        catch { }

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }
}
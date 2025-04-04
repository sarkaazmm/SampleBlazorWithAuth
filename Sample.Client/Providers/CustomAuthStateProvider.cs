using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Sample.Client.Providers;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private const string TOKEN_KEY = "authToken";

    public CustomAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(TOKEN_KEY);
        
        if (string.IsNullOrEmpty(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var jwtToken = _tokenHandler.ReadJwtToken(token);
        
        if (jwtToken.ValidTo < DateTime.UtcNow)
        {
            await _localStorage.RemoveItemAsync(TOKEN_KEY);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        
        var claims = jwtToken.Claims.ToList();
        
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        
        return new AuthenticationState(user);
    }

    public void NotifyUserAuthentication(string token)
    {
        var jwtToken = _tokenHandler.ReadJwtToken(token);
        var claims = jwtToken.Claims.ToList();
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        
        var authState = Task.FromResult(new AuthenticationState(user));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }
}

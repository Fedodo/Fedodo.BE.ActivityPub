using System.Security.Claims;
using ActivityPubServer.Interfaces;
using CommonExtensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ActivityPubServer.Controllers.OAuth;

[Route("oauth")]
public class AuthorizationController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IUserHandler _userHandler;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager, IUserHandler userHandler)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _userHandler = userHandler;
    }


    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the authorization code/refresh token.
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var userId = result.Principal?.Claims.FirstOrDefault(i => i.Type == "sub")?.Value;

            if (userId.IsNull()) return BadRequest("Sid is null");

            // Retrieve the user profile corresponding to the authorization code/refresh token.
            var user = await _userHandler.GetUser(new Guid(userId));

            if (user.IsNull())
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The token is no longer valid."
                    }));

            var identity = new ClaimsIdentity(result?.Principal?.Claims,
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name,
                Claims.Role);

            // Override the user claims present in the principal in case they
            // changed since the authorization code/refresh token was issued.
            identity.SetClaim(Claims.Subject, user.Id.ToString())
                .SetClaim(Claims.Email, "user Mail") // TODO
                .SetClaim(Claims.Name, user.UserName);

            identity.SetDestinations(GetDestinations);

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Permissions.Scopes.Profile))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Permissions.Scopes.Email))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;

                if (claim.Subject.HasScope(Permissions.Scopes.Roles))
                    yield return Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }

    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");


        // Create a new ClaimsPrincipal containing the claims that
        // will be used to create an id_token, a token or a code.
        var claims = new List<Claim>
        {
            new(Claims.Subject, "64148cd8-f948-488e-b90a-797f3a6d3587")
        };
        var identity = new ClaimsIdentity(claims, "OpenIddict");
        var principal = new ClaimsPrincipal(identity);

        // Create a new authentication ticket holding the user identity.
        var ticket = new AuthenticationTicket(principal,
            new AuthenticationProperties(),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
        return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
    }
}
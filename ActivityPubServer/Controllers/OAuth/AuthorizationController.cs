
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ActivityPubServer.Extensions;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.Authentication;
using ActivityPubServer.Model.OAuth;
using CommonExtensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
namespace ActivityPubServer.Controllers.OAuth;

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



    [HttpPost("~/oauth/token"), IgnoreAntiforgeryToken, Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the authorization code/refresh token.
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // // Retrieve the user profile corresponding to the authorization code/refresh token.
            // var user = await _userManager.FindByIdAsync(result.Principal.GetClaim(Claims.Subject));
            // if (user is null)
            // {
            //     return Forbid(
            //         authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            //         properties: new AuthenticationProperties(new Dictionary<string, string>
            //         {
            //             [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
            //             [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
            //                 "The token is no longer valid."
            //         }));
            // }
            //
            // // Ensure the user is still allowed to sign in.
            // if (!await _signInManager.CanSignInAsync(user))
            // {
            //     return Forbid(
            //         authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            //         properties: new AuthenticationProperties(new Dictionary<string, string>
            //         {
            //             [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
            //             [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
            //                 "The user is no longer allowed to sign in."
            //         }));
            // }

            var identity = new ClaimsIdentity(result?.Principal?.Claims,
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            // Override the user claims present in the principal in case they
            // changed since the authorization code/refresh token was issued.
            identity.SetClaim(Claims.Subject, "userid")
                .SetClaim(Claims.Email, "user Mail")
                .SetClaim(Claims.Name, "user name");

            identity.SetDestinations(GetDestinations);

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        
        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    //     var request = HttpContext.GetOpenIddictServerRequest() ??
    //         throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
    //
    //     if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
    //     {
    //         // Retrieve the claims principal stored in the authorization code/refresh token.
    //         var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    //
    //         // Retrieve the user profile corresponding to the authorization code/refresh token.
    //         var user = await _userManager.FindByIdAsync(result.Principal.GetClaim(OpenIddictConstants.Claims.Subject));
    //         if (user is null)
    //         {
    //             return Forbid(
    //                 authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
    //                 properties: new AuthenticationProperties(new Dictionary<string, string>
    //                 {
    //                     [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
    //                     [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
    //                 }));
    //         }
    //
    //         // Ensure the user is still allowed to sign in.
    //         if (!await _signInManager.CanSignInAsync(user))
    //         {
    //             return Forbid(
    //                 authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
    //                 properties: new AuthenticationProperties(new Dictionary<string, string>
    //                 {
    //                     [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
    //                     [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
    //                 }));
    //         }
    //
    //         var identity = new ClaimsIdentity(result.Principal.Claims,
    //             authenticationType: TokenValidationParameters.DefaultAuthenticationType,
    //             nameType: OpenIddictConstants.Claims.Name,
    //             roleType: OpenIddictConstants.Claims.Role);
    //
    //         // Override the user claims present in the principal in case they
    //         // changed since the authorization code/refresh token was issued.
    //         identity.SetClaim(OpenIddictConstants.Claims.Subject, await _userManager.GetUserIdAsync(user))
    //                 .SetClaim(OpenIddictConstants.Claims.Email, await _userManager.GetEmailAsync(user))
    //                 .SetClaim(OpenIddictConstants.Claims.Name, await _userManager.GetUserNameAsync(user))
    //                 .SetClaims(OpenIddictConstants.Claims.Role, (await _userManager.GetRolesAsync(user)).ToImmutableArray());
    //
    //         identity.SetDestinations(GetDestinations);
    //
    //         // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
    //         return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    //     }
    //
    //     throw new InvalidOperationException("The specified grant type is not supported.");
    // }
    //
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject.HasScope(OpenIddictConstants.Permissions.Scopes.Email))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject.HasScope(OpenIddictConstants.Permissions.Scopes.Roles))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
    
    [HttpGet("~/oauth/authorize")]
    [HttpPost("~/oauth/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Create a new ClaimsPrincipal containing the claims that
        // will be used to create an id_token, a token or a code.
        var claims = new List<Claim>();
        claims.Add(new Claim(OpenIddictConstants.Claims.Subject, "user-0001"));
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
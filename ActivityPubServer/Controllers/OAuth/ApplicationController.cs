using CommonExtensions;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace ActivityPubServer.Controllers.OAuth;

[Route("/api/v1/")]
public class ApplicationController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicationController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [HttpPost]
    [Route("apps")]
    public async Task<ActionResult<object>> RegisterClient(string clientName, Uri redirectUri)
    {
        if (clientName.IsNullOrEmpty())
        {
            return BadRequest($"Parameter {nameof(clientName)} is required!");
        }
        
        object obj = null;

        await using var scope = _serviceProvider.CreateAsyncScope();

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync(clientName) is null)
            obj = await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = clientName,
                ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                DisplayName = $"{clientName} client application",
                Type = OpenIddictConstants.ClientTypes.Public,
                PostLogoutRedirectUris =
                {
                    new Uri(
                        "https://localhost:44310/authentication/logout-callback") //                     new Uri("http://localhost/swagger/index.html")
                },
                RedirectUris =
                {
                    redirectUri
                },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Logout,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles
                },
                Requirements =
                {
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                }
            });

        return Ok(obj);
    }
}
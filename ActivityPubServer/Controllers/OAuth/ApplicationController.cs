using System.ComponentModel.DataAnnotations;
using CommonExtensions;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace ActivityPubServer.Controllers.OAuth;

[Route("/api/v1/")]
public class ApplicationController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApplicationController> _logger;

    public ApplicationController(IServiceProvider serviceProvider, ILogger<ApplicationController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [HttpPost]
    [Route("apps")]
    public async Task<ActionResult<object>> RegisterClient([FromBody] object obj)
    {
        // _logger.LogTrace($"Entered {nameof(RegisterClient)} in {nameof(ApplicationController)} with {nameof(redirectUri)}=\"{redirectUri}\", " +
        //                  $"{nameof(client_name)}=\"{client_name}\" and {nameof(clientName)}=\"{clientName}\"");
        //
        // if (client_name.IsNullOrEmpty() && clientName.IsNullOrEmpty())
        // {
        //     _logger.LogWarning($"Parameter {nameof(clientName)} is null or empty!");
        //     
        //     return BadRequest($"Parameter {nameof(clientName)} is null or empty!");
        // }
        //
        // var clientId = clientName.IsNotNull() ? clientName : client_name;
        //
        // object? obj = null;
        //
        // await using var scope = _serviceProvider.CreateAsyncScope();
        //
        // var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        //
        // if (await manager.FindByClientIdAsync(clientId) is null)
        //     obj = await manager.CreateAsync(new OpenIddictApplicationDescriptor
        //     {
        //         ClientId = clientId,
        //         ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
        //         DisplayName = $"{clientId} client application",
        //         Type = OpenIddictConstants.ClientTypes.Public,
        //         PostLogoutRedirectUris =
        //         {
        //             new Uri(
        //                 "https://localhost:44310/authentication/logout-callback") //                     new Uri("http://localhost/swagger/index.html")
        //         },
        //         RedirectUris =
        //         {
        //             redirectUri
        //         },
        //         Permissions =
        //         {
        //             OpenIddictConstants.Permissions.Endpoints.Authorization,
        //             OpenIddictConstants.Permissions.Endpoints.Logout,
        //             OpenIddictConstants.Permissions.Endpoints.Token,
        //             OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
        //             OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
        //             OpenIddictConstants.Permissions.ResponseTypes.Code,
        //             OpenIddictConstants.Permissions.Scopes.Email,
        //             OpenIddictConstants.Permissions.Scopes.Profile,
        //             OpenIddictConstants.Permissions.Scopes.Roles
        //         },
        //         Requirements =
        //         {
        //             OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
        //         }
        //     });

        return Ok(obj);
    }
}
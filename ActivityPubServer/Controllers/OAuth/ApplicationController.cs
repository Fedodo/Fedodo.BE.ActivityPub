using ActivityPubServer.Model.DTOs;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace ActivityPubServer.Controllers.OAuth;

[Route("/api/v1/")]
public class ApplicationController : ControllerBase
{
    private readonly ILogger<ApplicationController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ApplicationController(IServiceProvider serviceProvider, ILogger<ApplicationController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [HttpPost]
    [Route("apps")]
    public async Task<ActionResult<object>> RegisterClient([FromBody] RegisterClientDto clientDto)
    {
        _logger.LogTrace($"Entered {nameof(RegisterClient)} in {nameof(ApplicationController)}");

        object? obj = null;

        await using var scope = _serviceProvider.CreateAsyncScope();

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync(clientDto.ClientName) is null)
            obj = await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = clientDto.ClientName,
                ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                DisplayName = $"{clientDto.ClientName} client application",
                Type = OpenIddictConstants.ClientTypes.Public,
                PostLogoutRedirectUris =
                {
                    new Uri(
                        "https://localhost:44310/authentication/logout-callback") //                     new Uri("http://localhost/swagger/index.html")
                },
                RedirectUris =
                {
                    clientDto.RedirectUri
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
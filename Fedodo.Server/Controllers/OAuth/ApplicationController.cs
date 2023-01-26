using System.Security.Cryptography;
using CommonExtensions;
using Fedodo.Server.Model.DTOs;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace Fedodo.Server.Controllers.OAuth;

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
    public async Task<ActionResult<ReturnApplicationDto>> RegisterClient([FromBody] RegisterClientDto clientDto,
        bool isPublicClient = false)
    {
        _logger.LogTrace($"Entered {nameof(RegisterClient)} in {nameof(ApplicationController)}");

        await using var scope = _serviceProvider.CreateAsyncScope();

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (clientDto.ClientName.IsNull()) return BadRequest($"{nameof(clientDto.ClientName)} can not be null");

        if (await manager.FindByClientIdAsync(clientDto.ClientName) is null)
        {
            var returnAppDto = new ReturnApplicationDto();

            dynamic obj = null;

            if (isPublicClient)
            {
                obj = await manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = clientDto.ClientName,
                    ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                    DisplayName = $"{clientDto.ClientName} client application",
                    Type = OpenIddictConstants.ClientTypes.Public,
                    // ClientSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
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
                        OpenIddictConstants.Permissions.Scopes.Roles,
                        "scp:read",
                        "scp:write",
                        "scp:follow"
                    },
                    Requirements =
                    {
                        OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                    }
                });
            }
            else
            {
                var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

                returnAppDto.ClientSecret = secret;

                obj = await manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = clientDto.ClientName,
                    ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                    DisplayName = $"{clientDto.ClientName} client application",
                    Type = OpenIddictConstants.ClientTypes.Confidential,
                    ClientSecret = secret,
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
                    }
                });
            }

            returnAppDto.ClientId = obj.ClientId;
            returnAppDto.Name = obj.DisplayName;
            returnAppDto.RedirectUri = clientDto.RedirectUri;

            return Ok(returnAppDto);
        }

        return BadRequest("Client already exists");
    }
}
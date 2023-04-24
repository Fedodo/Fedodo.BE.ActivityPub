using System.Security.Cryptography;
using CommonExtensions;
using CommonExtensions.Cryptography;
using Fedodo.BE.ActivityPub.Model;
using Fedodo.BE.ActivityPub.Model.DTOs;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes.SubTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Fedodo.NuGet.Common.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers;

[Route("User")]
public class UserController : ControllerBase
{
    private readonly IAuthenticationHandler _authenticationHandler;
    private readonly ILogger<UserController> _logger;
    private readonly IMongoDbRepository _repository;

    public UserController(ILogger<UserController> logger, IMongoDbRepository repository,
        IAuthenticationHandler authenticationHandler)
    {
        _logger = logger;
        _repository = repository;
        _authenticationHandler = authenticationHandler;
    }

    [HttpPost]
    public async Task<ActionResult<Actor>> CreateUser(CreateActorDto actorDto)
    {
        // Create actor
        var userId = Guid.NewGuid();
        var domainName = Environment.GetEnvironmentVariable("DOMAINNAME");

        Actor actor = new()
        {
            // Client generated
            Summary = actorDto.Summary,
            PreferredUsername = actorDto.PreferredUsername,
            Name = actorDto.Name,
            Type = actorDto.Type,

            // Server generated
            Id = new Uri($"https://{domainName}/actor/{userId}"),
            Inbox = new Uri($"https://{domainName}/inbox/{userId}"),
            Outbox = new Uri($"https://{domainName}/outbox/{userId}"),
            Following = new Uri($"https://{domainName}/following/{userId}"),
            Followers = new Uri($"https://{domainName}/followers/{userId}"),

            // Hardcoded
            Context = new TripleSet<Object>
            {
                StringLinks = new[]
                {
                    "https://www.w3.org/ns/activitystreams",
                    "https://w3id.org/security/v1"
                }
            }
        };

        var rsa = RSA.Create();

        actor.PublicKey = new PublicKey
        {
            Id = new Uri($"{actor.Id}#main-key"),
            Owner = actor.Id,
            PublicKeyPem = rsa.ExtractRsaPublicKeyPem()
        };

        // Add Actor if it is not exiting
        var filterDefinitionBuilder = Builders<Actor>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i.PreferredUsername == actor.PreferredUsername);
        var exitingActor = await _repository.GetSpecificItem(filter, DatabaseLocations.Actors.Database,
            DatabaseLocations.Actors.Collection);
        if (exitingActor.IsNull())
        {
            await _repository.Create(actor, DatabaseLocations.Actors.Database, DatabaseLocations.Actors.Collection);
        }
        else
        {
            _logger.LogWarning("Wanted to create a User which already exists");

            return BadRequest("User already exists");
        }

        // Create Webfinger
        var webfinger = new Webfinger
        {
            Subject = $"acct:{actor.PreferredUsername}@{Environment.GetEnvironmentVariable("DOMAINNAME")}",
            Links = new List<WebLink>
            {
                new()
                {
                    Rel = "self",
                    Href = actor.Id,
                    Type = "application/activity+json"
                }
            }
        };

        await _repository.Create(webfinger, DatabaseLocations.Webfinger.Database,
            DatabaseLocations.Webfinger.Collection);

        // Create User
        User user = new();
        _authenticationHandler.CreatePasswordHash(actorDto.Password, out var passwordHash, out var passwordSalt);
        user.Id = userId;
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        user.UserName = actorDto.PreferredUsername;
        user.Role = "User";
        user.PrivateKeyActivityPub = rsa.ExtractRsaPrivateKeyPem();

        await _repository.Create(user, DatabaseLocations.Users.Database, DatabaseLocations.Users.Collection);

        return Ok();
    }
}
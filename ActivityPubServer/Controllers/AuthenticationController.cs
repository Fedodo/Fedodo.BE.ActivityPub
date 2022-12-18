using System.Security.Cryptography;
using ActivityPubServer.DTOs;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model;
using CommonExtensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ActivityPubServer.Controllers;

[Route("Authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IMongoDbRepository _repository;

    public AuthenticationController(ILogger<AuthenticationController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpPost]
    public async Task<ActionResult<Actor>> CreateUser(CreateActorDto actorDto)
    {
        // Create actor
        Actor actor = new()
        {
            // Client generated
            Summary = actorDto.Summary,
            PreferredUsername = actorDto.PreferredUsername,
            Name = actorDto.Name,
            Type = actorDto.Type,

            // Server generated
            Id = new Uri($"https://ap.lna-dev.net/actor/{Guid.NewGuid()}"),
            Inbox = new Uri("https://ap.lna-dev.net/inbox"), // TODO

            // Hardcoded
            Context = new[]
            {
                "https://www.w3.org/ns/activitystreams",
                "https://w3id.org/security/v1"
            }
        };

        var rsa = RSA.Create();

        actor.PublicKey = new PublicKeyAP
        {
            Id = new Uri($"{actor.Id}#main-key"), // TODO
            Owner = actor.Id,
            PublicKeyPem = ExtractPublicKey(rsa)
        };

        // Add Actor if it is not exiting
        var filterDefinitionBuilder = Builders<Actor>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i.Name == actor.Name);
        var exitingActor = await _repository.GetSpecific(filter, "ActivityPub", "Actors");
        if (exitingActor.IsNull())
        {
            await _repository.Create(actor, "ActivityPub", "Actors");
        }
        else
        {
            _logger.LogWarning("Wanted to create a User which already exists");

            return BadRequest("User already exists");
        }

        // Create Webfinger
        var webfinger = new Webfinger
        {
            Subject = $"acct:{actor.Name}@{Environment.GetEnvironmentVariable("DOMAINNAME")}",
            Links = new List<Link>
            {
                new()
                {
                    Rel = "self",
                    Href = actor.Id,
                    Type = "application/activity+json"
                }
            }
        };

        await _repository.Create(webfinger, "ActivityPub", "Webfingers");

        // Create User


        return Ok();
    }

    private static string ExtractPublicKey(RSA rsa)
    {
        // Public key export
        var beginRsaPublicKey = "-----BEGIN RSA PUBLIC KEY-----";
        var endRsaPublicKey = "-----END RSA PUBLIC KEY-----";
        var base64PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var publicKey = $"{beginRsaPublicKey}\n{base64PublicKey}\n{endRsaPublicKey}";
        return publicKey;
    }
}
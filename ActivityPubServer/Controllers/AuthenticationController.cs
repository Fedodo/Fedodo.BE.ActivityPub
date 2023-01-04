using System.Security.Cryptography;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.Authentication;
using ActivityPubServer.Model.DTOs;
using CommonExtensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ActivityPubServer.Controllers;

[Route("Authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationHandler _authenticationHandler;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IMongoDbRepository _repository;

    public AuthenticationController(ILogger<AuthenticationController> logger, IMongoDbRepository repository,
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
        var filter = filterDefinitionBuilder.Where(i => i.PreferredUsername == actor.PreferredUsername);
        var exitingActor = await _repository.GetSpecificItem(filter, "ActivityPub", "Actors");
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
            Subject = $"acct:{actor.PreferredUsername}@{Environment.GetEnvironmentVariable("DOMAINNAME")}",
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
        User user = new();
        _authenticationHandler.CreatePasswordHash(actorDto.Password, out var passwordHash, out var passwordSalt);
        user.Id = userId;
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        user.UserName = actorDto.PreferredUsername;
        user.Role = "User";
        user.PrivateKeyActivityPub = ExtractPrivateKey(rsa);

        await _repository.Create(user, "Authentication", "Users");

        return Ok();
    }

    [HttpPost("Login")]
    public async Task<ActionResult<string>> Login(LoginDto userDto)
    {
        //TODO OTIMIZE THAT
        var users = await _repository.GetAll<User>("Authentication", "Users");

        if (users.Where(i => i.UserName == userDto.UserName).Count() <= 0) return BadRequest("User not found");

        if (users.Where(i => i.UserName == userDto.UserName).Count() > 1)
            return BadRequest("Multible username error detected");

        var user = users.Where(i => i.UserName == userDto.UserName).First();


        if (!_authenticationHandler.VerifyPasswordHash(userDto.Password, user.PasswordHash, user.PasswordSalt))
            return BadRequest("Wrong password");

        var token = _authenticationHandler.CreateToken(user);
        return Ok(token);
    }

    private string ExtractPrivateKey(RSA rsa)
    {
        const string beginRsaPrivateKey = "-----BEGIN RSA PRIVATE KEY-----";
        const string endRsaPrivateKey = "-----END RSA PRIVATE KEY-----";
        var keyPrv = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var extractPrivateKey = $"{beginRsaPrivateKey}\n{keyPrv}\n{endRsaPrivateKey}";

        return extractPrivateKey;
    }

    private string ExtractPublicKey(RSA rsa)
    {
        // Public key export
        const string beginRsaPublicKey = "-----BEGIN RSA PUBLIC KEY-----";
        const string endRsaPublicKey = "-----END RSA PUBLIC KEY-----";
        var base64PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var publicKey = $"{beginRsaPublicKey}\n{base64PublicKey}\n{endRsaPublicKey}";
        return publicKey;
    }
}
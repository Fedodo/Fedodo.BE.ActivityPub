using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.Authentication;
using CommonExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ActivityPubServer.Controllers;

[Route("outbox")]
public class OutboxController : ControllerBase
{
    private readonly ILogger<OutboxController> _logger;
    private readonly IMongoDbRepository _repository;

    public OutboxController(ILogger<OutboxController> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    [HttpPost("{userId}")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult> CreatePost(Guid userId)//, IActivityChild activityChild) // TODO
    {
        // General
        var postId = Guid.NewGuid();
        var postIdUri = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/create/{postId}");
        var actorId = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}");
        
        // Verify user
        var activeUserClaims = HttpContext.User.Claims.ToList();
        var tokenUserId = activeUserClaims.Where(i => i.ValueType.IsNotNull() && i.Type == ClaimTypes.Sid)?.First().Value;
        
        if (tokenUserId != userId.ToString())
        {
            _logger.LogWarning($"Someone tried to post as {userId} but was authorized as {tokenUserId}");
            return Forbid($"You are not {userId}");
        }
        
        // Get User
        var filterUserDefinitionBuilder = Builders<User>.Filter;
        var filterUser = filterUserDefinitionBuilder.Eq(i => i.Id, userId);
        var user = await _repository.GetSpecific(filterUser, "Authentication", "Users");
        var filterActorDefinitionBuilder = Builders<Actor>.Filter;
        var filterActor = filterActorDefinitionBuilder.Eq(i => i.Id, 
            new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}"));
        var actor = await _repository.GetSpecific(filterActor, "ActivityPub", "Actors");
        
        // Create activity
        var activity = new Activity()
        {
            Actor = actorId,
            Id = postIdUri,
            Type = "Create", // TODO
            //Object = activityChild // TODO
            Object = new Post()
            {
                Id = postIdUri,
                Type = "Note",
                Published = DateTime.UtcNow, // TODO
                AttributedTo = actorId,
                InReplyTo = new Uri("https://mastodon.social/@Gargron/100254678717223630"), 
                Content = "<p>Hello world</p>",
                To = new Uri("https://www.w3.org/ns/activitystreams#Public")
            }
        };
        
        // Set Http Signature
        HttpClient http = new();
        var encoder = new UTF8Encoding();

        HttpRequestMessage requestMessage = new();
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(encoder.GetBytes(user.PrivateKeyActivityPub), out int bytesReadPrivate);
        rsa.ImportRSAPublicKey(encoder.GetBytes(actor.PublicKey.PublicKeyPem), out int bytesReadPublic);
        
        var date = DateTime.UtcNow.ToString();
        var signedString = $"(request-target): post /inbox\nhost: mastodon.social\ndate: {date}";
        var signature = rsa.SignData(encoder.GetBytes(signedString.ToCharArray()), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        
        requestMessage.Headers.Add("Host","mastodon.social"); // TODO
        requestMessage.Headers.Add("Date",$"{date}");
        requestMessage.Headers.Add("Signature", $"keyId=\"https://my-example.com/actor#main-key\",headers=\"(request-target) " +
                                                $"host date\",signature=\"{encoder.GetChars(signature)}\"");
        
        var httpResponse = await http.SendAsync(requestMessage);

        return Ok(activity);
    }
}
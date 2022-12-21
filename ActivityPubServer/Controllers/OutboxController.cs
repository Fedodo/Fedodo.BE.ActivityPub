using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    public async Task<ActionResult> CreatePost(Guid userId) //, IActivityChild activityChild) // TODO
    {
        // General
        var postId = Guid.NewGuid();
        var postIdUri = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/create/{postId}");
        var actorId = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}");

        // Verify user
        var activeUserClaims = HttpContext.User.Claims.ToList();
        var tokenUserId = activeUserClaims.Where(i => i.ValueType.IsNotNull() && i.Type == ClaimTypes.Sid)?.First()
            .Value;

        if (tokenUserId != userId.ToString())
        {
            _logger.LogWarning($"Someone tried to post as {userId} but was authorized as {tokenUserId}");
            return Forbid();
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
        var activity = new Activity
        {
            Actor = actorId,
            Id = postIdUri,
            Type = "Create", // TODO
            //Object = activityChild // TODO
            Object = new Post
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
        var jsonData = JsonSerializer.Serialize(activity);

        var rsa = RSA.Create();
        rsa.ImportFromPem(actor.PublicKey.PublicKeyPem.ToCharArray());
        rsa.ImportFromPem(user.PrivateKeyActivityPub.ToCharArray());

        var date = DateTime.UtcNow.ToString("R");
        var signedString = $"(request-target): post /inbox\nhost: mastodon.social\ndate: {date}";
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(signedString), HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        string signatureString = Convert.ToBase64String(signature);
        
        // Create HTTP request
        HttpClient http = new();
        http.DefaultRequestHeaders.Add("Host", "mastodon.social"); // TODO
        http.DefaultRequestHeaders.Add("Date", date);
        http.DefaultRequestHeaders.Add("Digest", $"sha-256={ComputeHash(jsonData)}");
        http.DefaultRequestHeaders.Add("Signature",
            $"keyId=\"{actor.PublicKey.Id}\",headers=\"(request-target) " +
            $"host date digest\",signature=\"{signatureString}\"");

        var contentData = new StringContent(jsonData, Encoding.UTF8, "application/ld+json");

        var httpResponse = await http.PostAsync(new Uri("https://mastodon.social/inbox"), contentData);
        
        if (!httpResponse.IsSuccessStatusCode)
        {
            string responseText = await httpResponse.Content.ReadAsStringAsync();
            
            return BadRequest(responseText);
        }
        
        return Ok(activity);
    }

    private string? ComputeHash(string jsonData)
    {
        SHA256 sha = SHA256.Create(); // Create a SHA256 hash from string   
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // Computing Hash - returns here byte array
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(jsonData));

            var hashedString = Convert.ToBase64String(bytes);

            return hashedString;
        }
    }
}
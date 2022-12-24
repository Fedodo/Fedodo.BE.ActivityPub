using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.Authentication;
using ActivityPubServer.Model.DTOs;
using CommonExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;

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

    [HttpGet("{userId}")]
    public async Task<ActionResult<OrderedCollection>> GetAllPublicPosts(Guid userId)
    {
        var filterDefinitionBuilder = Builders<Post>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i.To == "https://www.w3.org/ns/activitystreams#Public"
                                                        || i.To == "as:Public" || i.To == "public");

        var posts = await _repository.GetSpecificItems(filter, "Posts", userId.ToString());

        var orderedCollection = new OrderedCollection
        {
            Summary = $"Posts of {userId}",
            OrderedItems = posts
        };

        return Ok(orderedCollection);
    }

    [HttpPost("{userId}")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<Activity>> CreatePost(Guid userId, [FromBody] CreatePostDto postDto)
    {
        if (!VerifyUser(userId)) return Forbid();
        if (postDto.IsNull()) return BadRequest("Post can not be null");

        // Build props
        var user = await GetUser(userId);
        var actor = await GetActor(userId);
        var activity = await CreateActivity(userId, postDto);

        // Send activities
        var targets = new List<string>();

        targets.Add("mastodon.social"); // TODO
        targets.Add("mastodon.online");

        foreach (var target in targets) await SendActivity(activity, user, target, actor);

        return Ok(activity);
    }

    private bool VerifyUser(Guid userId)
    {
        var activeUserClaims = HttpContext.User.Claims.ToList();
        var tokenUserId = activeUserClaims.Where(i => i.ValueType.IsNotNull() && i.Type == ClaimTypes.Sid)?.First()
            .Value;

        if (tokenUserId == userId.ToString()) return true;

        _logger.LogWarning($"Someone tried to post as {userId} but was authorized as {tokenUserId}");
        return false;
    }

    private async Task<Actor> GetActor(Guid userId)
    {
        var filterActorDefinitionBuilder = Builders<Actor>.Filter;
        var filterActor = filterActorDefinitionBuilder.Eq(i => i.Id,
            new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}"));
        var actor = await _repository.GetSpecificItem(filterActor, "ActivityPub", "Actors");
        return actor;
    }

    private async Task<User> GetUser(Guid userId)
    {
        var filterUserDefinitionBuilder = Builders<User>.Filter;
        var filterUser = filterUserDefinitionBuilder.Eq(i => i.Id, userId);
        var user = await _repository.GetSpecificItem(filterUser, "Authentication", "Users");
        return user;
    }

    private async Task<Activity> CreateActivity(Guid userId, CreatePostDto postDto)
    {
        var postId = Guid.NewGuid();
        var actorId = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}");

        var post = new Post
        {
            To = postDto.To,
            Name = postDto.Name,
            Summary = postDto.Summary,
            Sensitive = postDto.Sensitive,
            InReplyTo = postDto.InReplyTo,
            Content = postDto.Content,
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/posts/{postId}"),
            Type = postDto.Type,
            Published = postDto.Published,
            AttributedTo = actorId
        };

        await _repository.Create(post, "Posts", userId.ToString());

        var activity = new Activity
        {
            Actor = actorId,
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/activitys/{postId}"),
            Type = "Create", // TODO
            Object = post
        };
        return activity;
    }

    private async Task<bool> SendActivity(Activity activity, User user, string targetServerName, Actor actor)
    {
        // Set Http Signature
        var jsonData = JsonConvert.SerializeObject(activity);
        var digest = ComputeHash(jsonData);

        var rsa = RSA.Create();
        rsa.ImportFromPem(user.PrivateKeyActivityPub.ToCharArray());

        var date = DateTime.UtcNow.ToString("R");
        var signedString =
            $"(request-target): post /inbox\nhost: {targetServerName}\ndate: {date}\ndigest: sha-256={digest}";
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(signedString), HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var signatureString = Convert.ToBase64String(signature);

        // Create HTTP request
        HttpClient http = new();
        http.DefaultRequestHeaders.Add("Host", targetServerName);
        http.DefaultRequestHeaders.Add("Date", date);
        http.DefaultRequestHeaders.Add("Digest", $"sha-256={digest}");
        http.DefaultRequestHeaders.Add("Signature",
            $"keyId=\"{actor.PublicKey.Id}\",headers=\"(request-target) " +
            $"host date digest\",signature=\"{signatureString}\"");

        var contentData = new StringContent(jsonData, Encoding.UTF8, "application/ld+json");

        var httpResponse = await http.PostAsync(new Uri($"https://{targetServerName}/inbox"), contentData);

        if (httpResponse.IsSuccessStatusCode) return true;

        var responseText = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogWarning($"An error occured sending an activity: {responseText}");

        return false;
    }

    private string ComputeHash(string jsonData)
    {
        var sha = SHA256.Create(); // Create a SHA256 hash from string   
        using var sha256Hash = SHA256.Create();
        // Computing Hash - returns here byte array
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(jsonData));

        var hashedString = Convert.ToBase64String(bytes);

        return hashedString;
    }
}
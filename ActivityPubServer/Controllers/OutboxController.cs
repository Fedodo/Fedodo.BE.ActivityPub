using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ActivityPubServer.Extensions;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.Authentication;
using ActivityPubServer.Model.DTOs;
using ActivityPubServer.Model.Helpers;
using CommonExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ActivityPubServer.Controllers;

[Route("Outbox")]
public class OutboxController : ControllerBase
{
    private readonly IKnownServersHandler _knownServersHandler;
    private readonly ILogger<OutboxController> _logger;
    private readonly IMongoDbRepository _repository;

    public OutboxController(ILogger<OutboxController> logger, IMongoDbRepository repository,
        IKnownServersHandler knownServersHandler)
    {
        _logger = logger;
        _repository = repository;
        _knownServersHandler = knownServersHandler;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<OrderedCollection<Post>>> GetAllPublicPosts(Guid userId)
    {
        var filterDefinitionBuilder = Builders<Post>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i.IsPostPublic());
        var posts = await _repository.GetSpecificItems(filter, "Posts", userId.ToString());

        var orderedCollection = new OrderedCollection<Post>
        {
            Summary = $"Posts of {userId}",
            OrderedItems = posts
        };

        return Ok(orderedCollection);
    }

    [HttpPost("{userId}")]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<Activity>> CreatePost(Guid userId, [FromBody] CreateActivityDto activityDto)
    {
        if (!VerifyUser(userId)) return Forbid();
        if (activityDto.IsNull()) return BadRequest("Activity can not be null");

        var user = await GetUser(userId);
        var actor = await GetActor(userId);
        var activity = await CreateActivity(userId, activityDto);

        await SendActivities(activity, user, actor);

        return Ok(activity);
    }

    private async Task SendActivities(Activity activity, User user, Actor actor)
    {
        var targets = new List<ServerNameInboxPair>();

        if (activity.IsActivityPublic() && activity.Type == "Create")
        {
            var post = activity.ExtractItemFromObject<Post>();

            if (post.InReplyTo.IsNotNull()) await _knownServersHandler.Add(post.InReplyTo.ToString());

            var servers = await _knownServersHandler.GetAll();

            foreach (var item in servers)
                targets.Add(new ServerNameInboxPair
                {
                    ServerName = item.ServerDomainName,
                    Inbox = item.DefaultInbox
                });
        }
        else if (activity.IsActivityPublic())
        {
            var servers = await _knownServersHandler.GetAll();

            foreach (var item in servers)
                targets.Add(new ServerNameInboxPair
                {
                    ServerName = item.ServerDomainName,
                    Inbox = item.DefaultInbox
                });
        }
        else
        {
            var serverNameInboxPair = new ServerNameInboxPair
            {
                ServerName = activity.To.ExtractServerName(),
                Inbox = new Uri(activity.To)
            };

            targets.Add(serverNameInboxPair);

            await _knownServersHandler.Add(activity.To);
        }

        foreach (var target in targets) await SendActivity(activity, user, target, actor); // TODO Error Handling
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

    private async Task<Activity> CreateActivity(Guid userId, CreateActivityDto activityDto)
    {
        var postId = Guid.NewGuid();
        var actorId = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{userId}");
        object? obj = null;

        switch (activityDto.Type)
        {
            case "Create":
            {
                var createPostDto = activityDto.ExtractCreatePostDtoFromObject();

                var post = new Post
                {
                    To = createPostDto.To,
                    Name = createPostDto.Name,
                    Summary = createPostDto.Summary,
                    Sensitive = createPostDto.Sensitive,
                    InReplyTo = createPostDto.InReplyTo,
                    Content = createPostDto.Content,
                    Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/posts/{postId}"),
                    Type = createPostDto.Type,
                    Published = createPostDto.Published,
                    AttributedTo = actorId
                };

                await _repository.Create(post, "Posts", userId.ToString());

                obj = post;
                break;
            }
            case "Like" or "Follow":
            {
                obj = activityDto.ExtractStringFromObject();
            }
                break;
        }

        var activity = new Activity
        {
            Actor = actorId,
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/activitys/{postId}"),
            Type = activityDto.Type,
            To = activityDto.To,
            Object = obj
        };

        await _repository.Create(activity, "Activities", userId.ToString());

        return activity;
    }

    private async Task<bool> SendActivity(Activity activity, User user, ServerNameInboxPair serverInboxPair,
        Actor actor)
    {
        // Set Http Signature
        var jsonData = JsonSerializer.Serialize(activity);
        var digest = ComputeHash(jsonData);

        var rsa = RSA.Create();
        rsa.ImportFromPem(user.PrivateKeyActivityPub.ToCharArray());

        var date = DateTime.UtcNow.ToString("R");
        var signedString =
            $"(request-target): post {serverInboxPair.Inbox.AbsolutePath}\nhost: {serverInboxPair.ServerName}\ndate: {date}\ndigest: sha-256={digest}";
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(signedString), HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var signatureString = Convert.ToBase64String(signature);

        // Create HTTP request
        HttpClient http = new();
        http.DefaultRequestHeaders.Add("Host", serverInboxPair.ServerName);
        http.DefaultRequestHeaders.Add("Date", date);
        http.DefaultRequestHeaders.Add("Digest", $"sha-256={digest}");
        http.DefaultRequestHeaders.Add("Signature",
            $"keyId=\"{actor.PublicKey.Id}\",headers=\"(request-target) " +
            $"host date digest\",signature=\"{signatureString}\"");

        var contentData = new StringContent(jsonData, Encoding.UTF8, "application/ld+json");

        var httpResponse = await http.PostAsync(serverInboxPair.Inbox, contentData);

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
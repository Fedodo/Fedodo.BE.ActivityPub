using CommonExtensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenIddict.Validation.AspNetCore;

namespace Fedido.Server.Controllers.ActivityPub;

[Route("Outbox")]
public class OutboxController : ControllerBase
{
    private readonly IActivityHandler _activityHandler;
    private readonly ILogger<OutboxController> _logger;
    private readonly IMongoDbRepository _repository;
    private readonly IUserHandler _userHandler;

    public OutboxController(ILogger<OutboxController> logger, IMongoDbRepository repository,
        IActivityHandler activityHandler, IUserHandler userHandler)
    {
        _logger = logger;
        _repository = repository;
        _activityHandler = activityHandler;
        _userHandler = userHandler;
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<OrderedCollection<Post>>> GetAllPublicPosts(Guid userId)
    {
        // This filter can not use the extensions method IsPostPublic
        var filterDefinitionBuilder = Builders<Post>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i.To.Any(item =>
            item == "https://www.w3.org/ns/activitystreams#Public"
            || item == "as:Public" || item == "public"));
        var posts = await _repository.GetSpecificItems(filter, "Posts", userId.ToString());

        var orderedCollection = new OrderedCollection<Post>
        {
            Summary = $"Posts of {userId}",
            OrderedItems = posts
        };

        return Ok(orderedCollection);
    }

    [HttpPost("{userId:guid}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Activity>> CreatePost(Guid userId, [FromBody] CreateActivityDto activityDto)
    {
        if (!_userHandler.VerifyUser(userId, HttpContext)) return Forbid();
        if (activityDto.IsNull()) return BadRequest("Activity can not be null");

        var user = await _userHandler.GetUserById(userId);
        var actor = await _activityHandler.GetActor(userId);
        var activity = await CreateActivity(userId, activityDto);

        await _activityHandler.SendActivities(activity, user, actor);

        return Ok(activity);
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
}
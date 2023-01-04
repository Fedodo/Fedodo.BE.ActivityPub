using ActivityPubServer.Extensions;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.DTOs;
using CommonExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ActivityPubServer.Controllers;

[Route("Outbox")]
public class OutboxController : ControllerBase
{
    private readonly IActivityHandler _activityHandler;
    private readonly ILogger<OutboxController> _logger;
    private readonly IMongoDbRepository _repository;
    private readonly IUserVerificationHandler _userVerification;

    public OutboxController(ILogger<OutboxController> logger, IMongoDbRepository repository,
        IUserVerificationHandler userVerification, IActivityHandler activityHandler)
    {
        _logger = logger;
        _repository = repository;
        _userVerification = userVerification;
        _activityHandler = activityHandler;
    }

    [HttpGet("{userId:guid}")]
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
        if (!_userVerification.VerifyUser(userId, HttpContext)) return Forbid();
        if (activityDto.IsNull()) return BadRequest("Activity can not be null");

        var user = await _activityHandler.GetUser(userId);
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
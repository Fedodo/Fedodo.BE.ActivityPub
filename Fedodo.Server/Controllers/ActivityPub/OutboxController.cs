using CommonExtensions;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Fedodo.Server.Model.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenIddict.Validation.AspNetCore;

namespace Fedodo.Server.Controllers.ActivityPub;

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
        var posts = await _repository.GetSpecificItems(filter, DatabaseLocations.OutboxNotes.Database,
            DatabaseLocations.OutboxNotes.Collection);

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

        var user = await _userHandler.GetUserByIdAsync(userId);
        var actor = await _activityHandler.GetActorAsync(userId, Environment.GetEnvironmentVariable("DOMAINNAME"));
        var activity =
            await _activityHandler.CreateActivity(userId, activityDto,
                Environment.GetEnvironmentVariable("DOMAINNAME"));

        if (activity.IsNull()) return BadRequest("Activity could not be created. Check if Activity Type is supported.");

        await _activityHandler.SendActivitiesAsync(activity, user, actor);

        return Created(activity.Id, activity);
    }
}
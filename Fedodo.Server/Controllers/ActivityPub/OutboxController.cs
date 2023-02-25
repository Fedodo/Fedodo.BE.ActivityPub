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
    public async Task<ActionResult<PagedOrderedCollection>> GetPublicPostsPageInformation(Guid userId)
    {
        // This filter can not use the extensions method IsPostPublic
        var filterDefinitionBuilder = Builders<Post>.Filter;
        // You have to do it like this because if you make everything in one call MongoDB does not like it anymore.
        var filter = filterDefinitionBuilder.Where(i => i.To.Any(item =>
            item == "https://www.w3.org/ns/activitystreams#Public") || i.To.Any(item =>
            item == "as:Public") || i.To.Any(item => item == "public")); 
        
        var postCount = await _repository.CountSpecific(DatabaseLocations.OutboxNotes.Database,
            DatabaseLocations.OutboxNotes.Collection, filter);

        var orderedCollection = new PagedOrderedCollection()
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{userId}"),
            TotalItems = postCount,
            First = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{userId}/page/0"),
            Last = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{userId}/page/{postCount / 20}"),
        };

        return Ok(orderedCollection);
    }

    [HttpGet("{userId:guid}/page/{pageId:int}")]
    public async Task<ActionResult<OrderedCollectionPage<Post>>> GetPublicPage(Guid userId, int pageId)
    {
        var builder = Builders<Post>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Post>();
        var filter = filterBuilder.Where(i => (i.To.Any(item =>
            item == "https://www.w3.org/ns/activitystreams#Public") || i.To.Any(item =>
            item == "as:Public") || i.To.Any(item => item == "public")) && i.InReplyTo == null); 
        
        var page = await _repository.GetSpecificPaged(DatabaseLocations.OutboxNotes.Database,
            DatabaseLocations.OutboxNotes.Collection, pageId: pageId, pageSize: 20, sortDefinition: sort, filter: filter);

        var previousPageId = pageId - 1;
        if (previousPageId < 0) previousPageId = 0;
        var nextPageId = pageId + 1;
        // TODO if (nextPageId > ) nextPageId = 
        
        var orderedCollectionPage = new OrderedCollectionPage<Post>()
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{userId}/page/{pageId}"),
            PartOf = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{userId}"),
            OrderedItems = page,
            Prev = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{userId}/page/{previousPageId}"),            
            Next = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{userId}/page/{nextPageId}"),
        };

        return Ok(orderedCollectionPage);
    }

    [HttpPost("{userId:guid}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Activity>> CreatePost(Guid userId, [FromBody] CreateActivityDto activityDto)
    {
        _logger.LogTrace($"Entered {nameof(CreatePost)} in {nameof(OutboxController)}");

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
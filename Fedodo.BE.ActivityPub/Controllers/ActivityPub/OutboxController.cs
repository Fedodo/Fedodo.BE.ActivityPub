using CommonExtensions;
using Fedodo.BE.ActivityPub.Interfaces;
using Fedodo.BE.ActivityPub.Model.DTOs;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

[Route("Outbox")]
[Produces("application/json")]
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

    [HttpGet("{actorId:guid}")]
    public async Task<ActionResult<OrderedCollection>> GetPublicPostsPageInformation(Guid actorId)
    {
        // This filter can not use the extensions method IsPostPublic
        var filterDefinitionBuilder = Builders<Activity>.Filter;
        // You have to do it like this because if you make everything in one call MongoDB does not like it anymore.
        var filter = filterDefinitionBuilder.Where(i => i.Actor.StringLinks.ToList()[0] == 
            $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{actorId}" && (i.To.StringLinks.Any(item =>
            item == "https://www.w3.org/ns/activitystreams#Public") || i.To.StringLinks.Any(item =>
            item == "as:Public") || i.To.StringLinks.Any(item => item == "public")));

        var postCount = await _repository.CountSpecific(DatabaseLocations.OutboxCreate.Database,
            DatabaseLocations.OutboxCreate.Collection, filter);

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{actorId}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{actorId}/page/0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{actorId}/page/{postCount / 20}"
                }
            },
            TotalItems = postCount
        };

        return Ok(orderedCollection);
    }

    [HttpGet("{actorId:guid}/page/{pageId:int}")]
    public async Task<ActionResult<OrderedCollectionPage>> GetPublicPage(Guid actorId, int pageId)
    {
        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Actor.StringLinks.ToList()[0] == 
            $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/actor/{actorId}" && (i.To.StringLinks.Any(item =>
                item == "https://www.w3.org/ns/activitystreams#Public") || i.To.StringLinks.Any(item =>
                item == "as:Public") || i.To.StringLinks.Any(item => item == "public")));

        var createPage = await _repository.GetSpecificPaged(DatabaseLocations.OutboxCreate.Database,
            DatabaseLocations.OutboxCreate.Collection, pageId, 20, sort, filter);
        var announcePage = await _repository.GetSpecificPaged(DatabaseLocations.OutboxAnnounce.Database,
            DatabaseLocations.OutboxAnnounce.Collection, pageId, 20, sort, filter);

        var page = createPage.ToList();
        page.AddRange(announcePage);
        page = page.OrderByDescending(i => i.Published).Take(20).ToList();

        var previousPageId = pageId - 1;
        if (previousPageId < 0) previousPageId = 0;
        var nextPageId = pageId + 1;
        // TODO if (nextPageId > ) nextPageId = 

        var orderedCollectionPage = new OrderedCollectionPage
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{actorId}/page/{pageId}"),
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{actorId}"
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{actorId}/page/{previousPageId}"
                }
            },
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/outbox/{actorId}/page/{nextPageId}"
                }
            },
            Items = new TripleSet<Object>
            {
                Objects = page
            },
            TotalItems = page.Count
        };

        return Ok(orderedCollectionPage);
    }

    [HttpPost("{actorId:guid}")]
    // [Authorize]
    public async Task<ActionResult<Activity>> CreatePost(Guid actorId, [FromBody] CreateActivityDto activityDto)
    {
        _logger.LogTrace($"Entered {nameof(CreatePost)} in {nameof(OutboxController)}");
        // if (!_userHandler.VerifyUser(userId, HttpContext)) return Forbid();

        if (activityDto.IsNull()) return BadRequest("Activity can not be null");

        var actorSecrets = await _activityHandler.GetActorSecretsAsync(actorId, Environment.GetEnvironmentVariable("DOMAINNAME")); 
        var actor = await _activityHandler.GetActorAsync(actorId, Environment.GetEnvironmentVariable("DOMAINNAME"));
        var activity =
            await _activityHandler.CreateActivity(actorId, activityDto,
                Environment.GetEnvironmentVariable("DOMAINNAME"));

        if (activity.IsNull()) return BadRequest("Activity could not be created. Check if Activity Type is supported.");

        await _activityHandler.SendActivitiesAsync(activity, actorSecrets, actor);

        return Created(activity.Id, activity);
    }
}
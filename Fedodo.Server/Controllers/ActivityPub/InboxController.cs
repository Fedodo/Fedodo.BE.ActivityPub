using CommonExtensions;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenIddict.Validation.AspNetCore;

namespace Fedodo.Server.Controllers.ActivityPub;

[Route("Inbox")]
public class InboxController : ControllerBase
{
    private readonly IActivityHandler _activityHandler;
    private readonly IHttpSignatureHandler _httpSignatureHandler;
    private readonly ILogger<InboxController> _logger;
    private readonly IMongoDbRepository _repository;
    private readonly IUserHandler _userHandler;

    public InboxController(ILogger<InboxController> logger, IHttpSignatureHandler httpSignatureHandler,
        IMongoDbRepository repository, IActivityHandler activityHandler, IUserHandler userHandler)
    {
        _logger = logger;
        _httpSignatureHandler = httpSignatureHandler;
        _repository = repository;
        _activityHandler = activityHandler;
        _userHandler = userHandler;
    }

    [HttpGet("{userId:guid}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<ActionResult<OrderedPagedCollection>> GetPageInformation(Guid userId)
    {
        if (!_userHandler.VerifyUser(userId, HttpContext)) return Forbid();

        var postCount = await _repository.CountAll<Activity>(DatabaseLocations.InboxCreate.Database,
            DatabaseLocations.InboxCreate.Collection);

        var orderedCollection = new OrderedPagedCollection
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}"),
            TotalItems = postCount,
            First = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/0"),
            Last = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/{postCount / 20}")
        };

        return Ok(orderedCollection);
    }

    [HttpGet("{userId:guid}/page/{pageId:int}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<ActionResult<OrderedCollectionPage<Activity>>> GetPageInInbox(Guid userId, int pageId)
    {
        if (!_userHandler.VerifyUser(userId, HttpContext)) return Forbid();

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var page = await _repository.GetAllPaged(DatabaseLocations.InboxCreate.Database,
            DatabaseLocations.InboxCreate.Collection, pageId, 20, sort);

        var previousPageId = pageId - 1;
        if (previousPageId < 0) previousPageId = 0;
        var nextPageId = pageId + 1;
        // TODO if (nextPageId > ) nextPageId = 

        var orderedCollectionPage = new OrderedCollectionPage<Activity>
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/{pageId}"),
            PartOf = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}"),
            OrderedItems = page,
            Prev = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/{previousPageId}"),
            Next = new Uri(
                $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/{nextPageId}")
        };

        return Ok(orderedCollectionPage);
    }

    [HttpPost]
    public async Task<ActionResult> SharedInbox([FromBody] Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(SharedInbox)} in {nameof(InboxController)}");

        if (!await _httpSignatureHandler.VerifySignature(HttpContext.Request.Headers, "/inbox"))
            return BadRequest("Invalid Signature");

        if (activity.IsNull())
        {
            _logger.LogWarning($"Activity is NULL in {nameof(SharedInbox)}");

            return BadRequest("Activity can not be null!");
        }

        return Ok();
    }

    [HttpPost("{userId:guid}")]
    public async Task<ActionResult> Inbox(Guid userId, [FromBody] Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(Inbox)} in {nameof(InboxController)}");

#if !DEBUG
        if (!await _httpSignatureHandler.VerifySignature(HttpContext.Request.Headers, $"/inbox/{userId}"))
            return BadRequest("Invalid Signature");
#endif

        if (activity.IsNull())
        {
            _logger.LogWarning($"Activity is NULL in {nameof(Inbox)}");

            return BadRequest("Activity can not be null!");
        }

        if (activity.Published.IsNull() || activity.Published <= DateTime.Parse("2000-01-01"))
            activity.Published = DateTime.Now;

        activity.Context = activity.Context.TrySystemJsonDeserialization<string>();

        switch (activity.Type)
        {
            case "Create":
            {
                _logger.LogDebug("Entered Create");

                activity.Object = activity.Object.TrySystemJsonDeserialization<Post>();

                var activityDefinitionBuilder = Builders<Activity>.Filter;
                var postFilter = activityDefinitionBuilder.Eq(i => i.Id, activity.Id);
                var fItem = await _repository.GetSpecificItems(postFilter, DatabaseLocations.InboxCreate.Database,
                    DatabaseLocations.InboxCreate.Collection);

                if (fItem.IsNotNullOrEmpty())
                    return BadRequest("Activity already exists");

                await _repository.Create(activity, DatabaseLocations.InboxCreate.Database,
                    DatabaseLocations.InboxCreate.Collection);

                _logger.LogDebug("Handling Reply Logic");
                if (((Post)activity.Object).InReplyTo.IsNotNull())
                {
                    _logger.LogDebug("InReply is not null");

                    if (((Post)activity.Object).InReplyTo?.Host == Environment.GetEnvironmentVariable("DOMAINNAME"))
                    {
                        var updateFilterBuilder = Builders<Activity>.Filter;
                        var updateFilter = updateFilterBuilder.Eq(i => i.Id, ((Post)activity.Object).InReplyTo);

                        var updateItem = await _repository.GetSpecificItem(updateFilter,
                            DatabaseLocations.OutboxCreate.Database,
                            DatabaseLocations.OutboxCreate.Collection);

                        if (updateItem.IsNull())
                        {
                            break;
                        }

                        if (((Post)updateItem.Object).Replies.Items.IsNull())
                        {
                            ((Post)updateItem.Object).Replies.Items = new List<Link>();
                        }

                        ((Post)updateItem.Object).Replies.Items.ToList().Add(new Link()
                        {
                            Href = activity.Id
                        });
                        await _repository.Update(updateItem, updateFilter, DatabaseLocations.OutboxCreate.Database,
                            DatabaseLocations.OutboxCreate.Collection);
                    }
                    else
                    {
                        _logger.LogDebug("Entering Outbox reply logic");

                        var updateFilterBuilder = Builders<Activity>.Filter;
                        var updateFilter = updateFilterBuilder.Eq(i => i.Id, ((Post)activity.Object).InReplyTo);

                        var updateItem = await _repository.GetSpecificItem(updateFilter,
                            DatabaseLocations.InboxCreate.Database,
                            DatabaseLocations.InboxCreate.Collection);

                        if (updateItem.IsNull())
                        {
                            _logger.LogWarning("Update Item is null");

                            break;
                        }

                        if (((Post)updateItem.Object).Replies.IsNull())
                        {
                            ((Post)updateItem.Object).Replies = new();
                        }

                        if (((Post)updateItem.Object).Replies.Items.IsNull())
                        {
                            ((Post)updateItem.Object).Replies.Items = new List<Link>();
                        }

                        var replies = ((Post)updateItem.Object).Replies;
                        var repliesItems = replies.Items.ToList();
                        repliesItems.Add(new Link()
                        {
                            Href = updateItem.Id
                        });
                        replies.Items = repliesItems;
                        ((Post)updateItem.Object).Replies = replies;

                        _logger.LogDebug("Sending Update to database");

                        await _repository.Update(updateItem, updateFilter, DatabaseLocations.InboxCreate.Database,
                            DatabaseLocations.InboxCreate.Collection);
                    }
                }
                else
                {
                    _logger.LogDebug("In Reply is null");
                }

                break;
            }
            case "Follow":
            {
                _logger.LogDebug(
                    $"Got follow for \"{activity.Object.TrySystemJsonDeserialization<string>()}\" from \"{activity.Actor}\"");

                activity.Object = activity.Object.TrySystemJsonDeserialization<string>();

                var definitionBuilder = Builders<Activity>.Filter;
                var helperFilter = definitionBuilder.Eq(i => i.Id, activity.Id);
                var fItem = await _repository.GetSpecificItems(helperFilter, DatabaseLocations.InboxFollow.Database,
                    DatabaseLocations.InboxFollow.Collection);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(activity, DatabaseLocations.InboxFollow.Database,
                        DatabaseLocations.InboxFollow.Collection);

                var domainName = Environment.GetEnvironmentVariable("DOMAINNAME");
                var user = await _userHandler.GetUserByIdAsync(userId);
                var actor = await _activityHandler.GetActorAsync(userId, domainName);

                var acceptActivity = new Activity
                {
                    Id = new Uri($"https://{domainName}/accepts/{Guid.NewGuid()}"),
                    Type = "Accept",
                    Actor = actor.Id,
                    Object = activity.Id,
                    To = new List<string>
                    {
                        "as:Public"
                    }
                };

                await _activityHandler.SendActivitiesAsync(acceptActivity, user, actor);

                break;
            }
            case "Accept":
            {
                _logger.LogTrace("Got an Accept activity");

                var acceptedActivity = activity.Object.TrySystemJsonDeserialization<Activity>();

                activity.Object = activity.Object.TrySystemJsonDeserialization<string>();

                var actorDefinitionBuilder = Builders<Activity>.Filter;
                var filter = actorDefinitionBuilder.Eq(i => i.Id, acceptedActivity.Id);
                var sendActivity = await _repository.GetSpecificItem(filter, DatabaseLocations.OutboxFollow.Database,
                    DatabaseLocations.OutboxFollow.Collection);

                if (sendActivity.IsNotNull())
                {
                    _logger.LogDebug("Found activity which was accepted");

                    await _repository.Create(activity, DatabaseLocations.InboxAccept.Database,
                        DatabaseLocations.InboxAccept.Collection);
                }
                else
                {
                    _logger.LogWarning("Not found activity which was accepted");
                }

                break;
            }
            case "Announce":
            {
                _logger.LogDebug("Got Announce");

                activity.Object = activity.Object.TrySystemJsonDeserialization<string>();

                var activityDefinitionBuilder = Builders<Activity>.Filter;
                var postFilter = activityDefinitionBuilder.Eq(i => i.Id, activity.Id);
                var fItem = await _repository.GetSpecificItems(postFilter, DatabaseLocations.InboxAnnounce.Database,
                    DatabaseLocations.InboxAnnounce.Collection);

                if (fItem.IsNotNullOrEmpty())
                    return BadRequest("Activity already exists");

                await _repository.Create(activity, DatabaseLocations.InboxAnnounce.Database,
                    DatabaseLocations.InboxAnnounce.Collection);

                break;
            }
            case "Like":
            {
                _logger.LogTrace("Got an Like Activity");

                activity.Object = activity.Object.TrySystemJsonDeserialization<string>();

                var definitionBuilder = Builders<Activity>.Filter;
                var filter = definitionBuilder.Eq(i => i.Id, activity.Id);
                var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.InboxLike.Database,
                    DatabaseLocations.InboxLike.Collection);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(activity, DatabaseLocations.InboxLike.Database,
                        DatabaseLocations.InboxLike.Collection);
                else
                    _logger.LogWarning("Got another like of the same actor.");

                break;
            }
            case "Update":
            {
                var postDefinitionBuilder = Builders<Activity>.Filter;
                var postFilter = postDefinitionBuilder.Eq(i => i.Id, activity.Id);

                await _repository.Update(activity, postFilter, DatabaseLocations.InboxCreate.Database,
                    DatabaseLocations.InboxCreate.Collection);

                break;
            }
            case "Undo":
            {
                var undoActivity = activity.Object.TrySystemJsonDeserialization<Activity>();
                var undoActivityObject = new Uri(undoActivity.Object.TrySystemJsonDeserialization<string>());

                switch (undoActivity.Type)
                {
                    case "Like":
                    {
                        _logger.LogTrace("Got undoActivity of type Like");

                        var definitionBuilder = Builders<Activity>.Filter;
                        var filter = definitionBuilder.Eq(i => i.Actor, activity.Actor);
                        var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.InboxLike.Database,
                            DatabaseLocations.InboxLike.Collection);

                        if (fItem.IsNotNullOrEmpty())
                            await _repository.Delete(filter, DatabaseLocations.InboxLike.Database,
                                DatabaseLocations.InboxLike.Collection);
                        else
                            _logger.LogWarning("Got no like of the same actor.");

                        break;
                    }
                    case "Announce":
                    {
                        _logger.LogTrace("Got an Undo Announce Activity");

                        var definitionBuilder = Builders<Activity>.Filter;
                        var filter = definitionBuilder.Eq(i => i.Id, activity.Id);
                        var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.InboxAnnounce.Database,
                            DatabaseLocations.InboxAnnounce.Collection);

                        if (fItem.IsNotNullOrEmpty())
                            await _repository.Delete(filter, DatabaseLocations.InboxAnnounce.Database,
                                DatabaseLocations.InboxAnnounce.Collection);
                        else
                            _logger.LogWarning("Found no share of this actor to undo.");

                        break;
                    }
                }

                break;
            }
            case "Delete":
            {
                var definitionBuilder = Builders<Activity>.Filter;
                var filter = definitionBuilder.Eq(i => i.Id, activity.Id);

                var post = await _repository.GetSpecificItem(filter, DatabaseLocations.InboxCreate.Database,
                    DatabaseLocations.InboxCreate.Collection);

                if (activity.Actor == activity.Actor)
                    await _repository.Delete(filter, DatabaseLocations.InboxCreate.Database,
                        DatabaseLocations.InboxCreate.Collection);

                break;
            }
        }

        return Ok();
    }
}
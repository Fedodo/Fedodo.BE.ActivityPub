using CommonExtensions;
using Fedodo.BE.ActivityPub.Interfaces;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Controllers.ActivityPub;

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
    [Authorize]
    public async Task<ActionResult<OrderedCollection>> GetPageInformation(Guid userId)
    {
        if (!_userHandler.VerifyUser(userId, HttpContext)) return Forbid();

        var postCount = await _repository.CountAll<Activity>(DatabaseLocations.InboxCreate.Database,
            DatabaseLocations.InboxCreate.Collection);

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/{postCount / 20}"
                }
            }
        };

        return Ok(orderedCollection);
    }

    [HttpGet("{userId:guid}/page/{pageId:int}")]
#if !DEBUG
    [Authorize]
#endif
    public async Task<ActionResult<OrderedCollectionPage>> GetPageInInbox(Guid userId, int pageId)
    {
#if !DEBUG
        if (!_userHandler.VerifyUser(userId, HttpContext)) return Forbid();
#endif

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var page = await _repository.GetAllPagedFromCollections(DatabaseLocations.InboxCreate.Database,
            DatabaseLocations.InboxCreate.Collection, pageId, 20, sort, DatabaseLocations.InboxAnnounce.Collection);

        var previousPageId = pageId - 1;
        if (previousPageId < 0) previousPageId = 0;
        var nextPageId = pageId + 1;
        // TODO if (nextPageId > ) nextPageId = 

        var orderedCollectionPage = new OrderedCollectionPage
        {
            Id = new Uri($"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/{pageId}"),
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}"
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/{previousPageId}"
                }
            },
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/inbox/{userId}/page/{nextPageId}"
                }
            }
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

        switch (activity.Type)
        {
            case "Create":
            {
                _logger.LogDebug("Entered Create");

                var activityDefinitionBuilder = Builders<Activity>.Filter;
                var postFilter = activityDefinitionBuilder.Eq(i => i.Id, activity.Id);
                var fItem = await _repository.GetSpecificItems(postFilter, DatabaseLocations.InboxCreate.Database,
                    DatabaseLocations.InboxCreate.Collection);

                if (fItem.IsNotNullOrEmpty())
                    return BadRequest("Activity already exists");

                await _repository.Create(activity, DatabaseLocations.InboxCreate.Database,
                    DatabaseLocations.InboxCreate.Collection);

                _logger.LogDebug("Handling Reply Logic");
                // TODO Extract this two if parts into one function
                if (activity.Object.Objects.First().InReplyTo.IsNotNull())
                {
                    _logger.LogDebug("InReply is not null");

                    if (new Uri(activity.Object.Objects.First().InReplyTo?.StringLinks.First()).Host ==
                        Environment.GetEnvironmentVariable("DOMAINNAME"))
                    {
                        var updateFilterBuilder = Builders<Activity>.Filter;
                        var updateFilter =
                            updateFilterBuilder.Eq(i => i.Object.Objects.First().Id,
                                new Uri(activity.Object.Objects.First().InReplyTo.StringLinks.First()));

                        var updateItem = await _repository.GetSpecificItem(updateFilter,
                            DatabaseLocations.OutboxCreate.Database,
                            DatabaseLocations.OutboxCreate.Collection);

                        if (updateItem.IsNull()) break;

                        if (updateItem.Object.Objects.First().Replies.Items.Links.IsNull())
                            updateItem.Object.Objects.First().Replies.Items = new TripleSet<Object>();

                        var tempLinks = updateItem.Object.Objects.First().Replies.Items.Links.ToList();

                        tempLinks.Add(new Link
                        {
                            Href = activity.Id
                        });

                        updateItem.Object.Objects.First().Replies.Items.Links = tempLinks;

                        await _repository.Update(updateItem, updateFilter, DatabaseLocations.OutboxCreate.Database,
                            DatabaseLocations.OutboxCreate.Collection);
                    }
                    else
                    {
                        _logger.LogDebug("Entering Outbox reply logic");

                        var updateFilterBuilder = Builders<Activity>.Filter;
                        var updateFilter =
                            updateFilterBuilder.Eq(i => i.Object.Objects.First().Id,
                                new Uri(activity.Object.Objects.First().InReplyTo.StringLinks.First()));

                        var updateItem = await _repository.GetSpecificItem(updateFilter,
                            DatabaseLocations.InboxCreate.Database,
                            DatabaseLocations.InboxCreate.Collection);

                        if (updateItem.IsNull())
                        {
                            _logger.LogWarning("Update Item is null");

                            break;
                        }

                        if (updateItem.Object.Objects.First().Replies.IsNull())
                            updateItem.Object.Objects.First().Replies = new CollectionPage();

                        if (updateItem.Object.Objects.First().Replies.Items.IsNull())
                            updateItem.Object.Objects.First().Replies.Items = new TripleSet<Object>();

                        var replies = updateItem.Object.Objects.First().Replies;
                        var repliesItems = replies.Items.Links.ToList();
                        repliesItems.Add(new Link
                        {
                            Href = activity.Id
                        });
                        replies.Items.Links = repliesItems;
                        updateItem.Object.Objects.First().Replies = replies;

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
                    $"Got follow for \"{activity.Object?.StringLinks?.FirstOrDefault()}\" from \"{activity.Actor}\"");

                var definitionBuilder = Builders<Activity>.Filter;
                var helperFilter = definitionBuilder.Eq(i => i.Id, activity.Id);
                var fItem = await _repository.GetSpecificItems(helperFilter, DatabaseLocations.InboxFollow.Database,
                    DatabaseLocations.InboxFollow.Collection);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(activity, DatabaseLocations.InboxFollow.Database,
                        DatabaseLocations.InboxFollow.Collection);

                var domainName = Environment.GetEnvironmentVariable("DOMAINNAME")!;
                var user = await _userHandler.GetUserByIdAsync(userId);
                var actor = await _activityHandler.GetActorAsync(userId, domainName);

                if (actor.IsNull() || actor.Id.IsNull())
                {
                    _logger.LogWarning($"{nameof(actor)} or the id of this actor was null in {nameof(InboxController)}");

                    return BadRequest("User not found");
                }
                
                var acceptActivity = new Activity
                {
                    Id = new Uri($"https://{domainName}/accepts/{Guid.NewGuid()}"),
                    Type = "Accept",
                    Actor = new TripleSet<Object>
                    {
                        StringLinks = new List<string>
                        {
                            actor.Id.ToString()
                        }
                    },
                    Object = new TripleSet<Object>
                    {
                        StringLinks = new[]
                        {
                            activity.Id!.ToString()
                        }
                    },
                    To = new TripleSet<Object>
                    {
                        StringLinks = new[]
                        {
                            "as:Public"
                        }
                    }
                };

                await _activityHandler.SendActivitiesAsync(acceptActivity, user, actor);
                
                _logger.LogDebug("Completed Follow activity");

                break;
            }
            case "Accept":
            {
                _logger.LogTrace("Got an Accept activity");
                
                var actorDefinitionBuilder = Builders<Activity>.Filter;
                var filter = actorDefinitionBuilder.Eq(i => i.Id, activity.Object?.Objects?.FirstOrDefault()?.Id);
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
                var undoActivity = (Activity?)activity.Object?.Objects?.FirstOrDefault();
                var undoActivityObject = new Uri(undoActivity?.Object?.StringLinks?.FirstOrDefault());

                switch (undoActivity?.Type)
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
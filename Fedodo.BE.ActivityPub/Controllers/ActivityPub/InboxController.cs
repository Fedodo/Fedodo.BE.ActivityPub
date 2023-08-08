using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Extensions;
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
[Produces("application/json")]
public class InboxController : ControllerBase
{
    private readonly IActivityHandler _activityHandler;
    private readonly IHttpSignatureHandler _httpSignatureHandler;
    private readonly ILogger<InboxController> _logger;
    private readonly IMongoDbRepository _repository;
    private readonly IUserHandler _userHandler;
    private readonly FollowingController _followingController;

    public InboxController(ILogger<InboxController> logger, IHttpSignatureHandler httpSignatureHandler,
        IMongoDbRepository repository, IActivityHandler activityHandler, IUserHandler userHandler,
        FollowingController followingController)
    {
        _logger = logger;
        _httpSignatureHandler = httpSignatureHandler;
        _repository = repository;
        _activityHandler = activityHandler;
        _userHandler = userHandler;
        _followingController = followingController;
    }

    [HttpGet("{actorGuid:guid}")]
    [Authorize]
    public async Task<ActionResult<OrderedCollection>> GetInboxPageInformation(Guid actorGuid)
    {
        if (!_userHandler.VerifyActorId(actorGuid, HttpContext)) return Forbid();

        var filterBuilder = Builders<Activity>.Filter;
        var filter = filterBuilder.Where(i => i.Type == "Create" || i.Type == "Announce");

        var followings = await _followingController.GetAllFollowings(actorGuid);

        var postCount =
            await _repository.CountSpecificFromCollections(DatabaseLocations.Activity.Database, followings, filter);

        var orderedCollection = new OrderedCollection
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/inbox/{actorGuid}"),
            First = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/0"
                }
            },
            Last = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/{postCount / 20}"
                }
            },
            TotalItems = postCount
        };

        return Ok(orderedCollection);
    }

    [HttpGet("{actorGuid:guid}/page/{pageId:int}")]
    [Authorize]
    public async Task<ActionResult<OrderedCollectionPage>> GetPageInInbox(Guid actorGuid, int pageId)
    {
        if (!_userHandler.VerifyActorId(actorGuid, HttpContext)) return Forbid();

        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = Builders<Activity>.Filter;
        var filter = filterBuilder.Where(i => i.To == "public" && (i.Type == "Create" || i.Type == "Announce"));

        var followings = await _followingController.GetAllFollowings(actorGuid);

        var page = (await _repository.GetSpecificPagedFromCollections(filter: filter,
            databaseName: DatabaseLocations.Activity.Database,
            collectionNames: followings, pageId: pageId, pageSize: 20, sortDefinition: sort)).ToList();

        var previousPageId = pageId - 1;
        if (previousPageId < 0) previousPageId = 0;
        var nextPageId = pageId + 1;
        // TODO if (nextPageId > ) nextPageId = 

        var orderedCollectionPage = new OrderedCollectionPage
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/{pageId}"),
            PartOf = new TripleSet<OrderedCollection>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}"
                }
            },
            Prev = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/{previousPageId}"
                }
            },
            Next = new TripleSet<OrderedCollectionPage>
            {
                StringLinks = new[]
                {
                    $"https://{GeneralConstants.DomainName}/inbox/{actorGuid}/page/{nextPageId}"
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

    [HttpPost("")]
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

    [HttpPost("{actorId:guid}")]
    public async Task<ActionResult> Inbox(Guid actorId, [FromBody] Activity activity)
    {
        _logger.LogTrace($"Entered {nameof(Inbox)} in {nameof(InboxController)}");

        if (!await _httpSignatureHandler.VerifySignature(HttpContext.Request.Headers, $"/inbox/{actorId}"))
            return BadRequest("Invalid Signature");

        if (activity.IsNull())
        {
            _logger.LogWarning($"Activity is NULL in {nameof(Inbox)}");

            return BadRequest("Activity can not be null!");
        }

        if (activity.Published.IsNull() || activity.Published <= DateTime.Parse("2000-01-01"))
            activity.Published = DateTime.Now;

        var activitySender = "";

        if (activity.Actor?.StringLinks?.FirstOrDefault().IsNotNull() ?? false)
        {
            activitySender = activity.Actor.StringLinks.FirstOrDefault()!;
            await _activityHandler.GetServerNameInboxPairAsync(new Uri(activity.Actor.StringLinks.First()), true);
        }
        else
        {
            return BadRequest("Actor can not be null");
        }

        switch (activity.Type)
        {
            case "Create":
            {
                _logger.LogDebug("Entered Create");

                var activityDefinitionBuilder = Builders<Activity>.Filter;
                var postFilter = activityDefinitionBuilder.Where(i => i.Type == "Create" && i.Id == activity.Id);
                var fItem = await _repository.GetSpecificItems(postFilter, DatabaseLocations.Activity.Database,
                    activitySender);

                if (fItem.IsNotNullOrEmpty())
                {
                    _logger.LogWarning("Returning BadRequest Activity already existed");

                    return BadRequest("Activity already exists");
                }

                await _repository.Create(activity, DatabaseLocations.Activity.Database, activitySender);

                _logger.LogDebug("Handling Reply Logic");
                if (activity.Object!.Objects!.First().InReplyTo.IsNotNull())
                {
                    _logger.LogDebug("InReply is not null");

                    if (new Uri(activity.Object.Objects?.First().InReplyTo?.StringLinks?.First() ?? "").Host ==
                        GeneralConstants.DomainName)
                    {
                        _logger.LogDebug("Entering Outbox reply logic");

                        await ReplyLogic(activity, DatabaseLocations.Activity.Database, activitySender);
                    }
                    else
                    {
                        _logger.LogDebug("Entering Inbox reply logic");

                        await ReplyLogic(activity, DatabaseLocations.Activity.Database, activitySender);
                    }
                }
                else
                {
                    _logger.LogDebug("InReplyTo is null");
                }

                break;
            }
            case "Follow":
            {
                _logger.LogDebug(
                    $"Got follow for \"{activity.Object?.StringLinks?.FirstOrDefault()}\" from \"{activity.Actor}\"");

                var definitionBuilder = Builders<Activity>.Filter;
                var helperFilter = definitionBuilder.Where(i => i.Type == "Follow" && i.Id == activity.Id);
                var fItem = await _repository.GetSpecificItems(helperFilter, DatabaseLocations.Activity.Database,
                    activitySender);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(activity, DatabaseLocations.Activity.Database, activitySender);

                var domainName = GeneralConstants.DomainName!;
                var actorSecrets = await _activityHandler.GetActorSecretsAsync(actorId, domainName);
                var actor = await _activityHandler.GetActorAsync(actorId, domainName);

                if (actor.IsNull() || actor.Id.IsNull())
                {
                    _logger.LogWarning(
                        $"{nameof(actor)} or the id of this actor was null in {nameof(InboxController)}");

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

                await _activityHandler.SendActivitiesAsync(acceptActivity, actorSecrets, actor);

                _logger.LogDebug("Completed Follow activity");

                break;
            }
            case "Accept":
            {
                _logger.LogTrace("Got an Accept activity");

                if (activity.Object?.Objects?.FirstOrDefault()?.Id.IsNull() ?? true)
                {
                    break;
                }

                var actorDefinitionBuilder = Builders<Activity>.Filter;
                var filter = actorDefinitionBuilder.Where(i =>
                    i.Type == "Accept" && i.Id == activity.Object.Objects.FirstOrDefault()!.Id);
                var sendActivity =
                    await _repository.GetSpecificItem(filter, DatabaseLocations.Activity.Database, activitySender);

                if (sendActivity.IsNotNull())
                {
                    _logger.LogDebug("Found activity which was accepted");

                    await _repository.Create(activity, DatabaseLocations.Activity.Database, activitySender);
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
                var postFilter = activityDefinitionBuilder.Where(i => i.Type == "Announce" && i.Id == activity.Id);
                var fItem = await _repository.GetSpecificItems(postFilter, DatabaseLocations.Activity.Database,
                    activitySender);

                if (fItem.IsNotNullOrEmpty())
                    return BadRequest("Activity already exists");

                await _repository.Create(activity, DatabaseLocations.Activity.Database, activitySender);

                break;
            }
            case "Like":
            {
                _logger.LogTrace("Got an Like Activity");

                var definitionBuilder = Builders<Activity>.Filter;
                var filter = definitionBuilder.Where(i => i.Type == "Like" && i.Id == activity.Id);
                var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.Activity.Database,
                    activitySender);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(activity, DatabaseLocations.Activity.Database, activitySender);
                else
                    _logger.LogWarning("Got another like of the same actor.");

                break;
            }
            case "Update":
            {
                var postDefinitionBuilder = Builders<Activity>.Filter;
                var postFilter = postDefinitionBuilder.Where(i => i.Type == "Create" && i.Id == activity.Id);

                await _repository.Update(activity, postFilter, DatabaseLocations.Activity.Database, activitySender);

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
                        var filter = definitionBuilder.Where(i => i.Type == "Like" && i.Actor == activity.Actor);
                        var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.Activity.Database,
                            activitySender);

                        if (fItem.IsNotNullOrEmpty())
                            await _repository.Delete(filter, DatabaseLocations.Activity.Database, activitySender);
                        else
                            _logger.LogWarning("Got no like of the same actor.");

                        break;
                    }
                    case "Announce":
                    {
                        _logger.LogTrace("Got an Undo Announce Activity");

                        var definitionBuilder = Builders<Activity>.Filter;
                        var filter = definitionBuilder.Where(i => i.Type == "Announce" && i.Id == activity.Id);
                        var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.Activity.Database,
                            activitySender);

                        if (fItem.IsNotNullOrEmpty())
                            await _repository.Delete(filter, DatabaseLocations.Activity.Database, activitySender);
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
                var filter = definitionBuilder.Where(i => i.Type == "Create" && i.Id == activity.Id);

                var specificItem =
                    await _repository.GetSpecificItem(filter, DatabaseLocations.Activity.Database, activitySender);

                if (activity.Actor == specificItem.Actor)
                    await _repository.Delete(filter, DatabaseLocations.Activity.Database, activitySender);

                break;
            }
        }

        return Ok();
    }

    private async Task ReplyLogic(Activity activity, string database, string collection)
    {
        var updateFilterBuilder = Builders<Activity>.Filter;
        var updateFilter =
            updateFilterBuilder.Where(i => i.Type == "Create" && i.Object!.Objects!.First().Id ==
                new Uri(activity.Object!.Objects!.First().InReplyTo!.StringLinks!.First()));

        var updateItem = await _repository.GetSpecificItem(updateFilter, database, collection);

        if (updateItem.IsNull())
        {
            _logger.LogWarning($"{nameof(updateItem)} is null");
            return;
        }

        if (updateItem.Object?.Objects?.First().Replies?.Items?.Links.IsNull() ?? true)
        {
            if (updateItem.Object.IsNull()) updateItem.Object = new TripleSet<Object>();

            if (updateItem.Object.Objects.IsNull()) updateItem.Object.Objects = new List<Object>();

            updateItem.Object.Objects.First().Replies = new Collection
            {
                Items = new TripleSet<Object>
                {
                    StringLinks = new List<string>()
                }
            };
        }

        if (activity.Id.IsNull())
        {
            _logger.LogWarning($"{nameof(activity.Id)} is null");
            return;
        }

        var tempLinks = updateItem.Object.Objects.First().Replies?.Items?.StringLinks?.ToList();

        tempLinks?.Add(activity.Id.ToString());

        if (updateItem.Object.Objects.First().Replies?.Items?.StringLinks.IsNotNull() ?? false)
        {
            updateItem.Object.Objects.First().Replies!.Items!.StringLinks = tempLinks;

            _logger.LogDebug($"Writing {nameof(updateItem)} into database");

            await _repository.Update(updateItem, updateFilter, database, collection);
        }
        else
        {
            _logger.LogWarning($"Can not assign to {nameof(tempLinks)}");
        }
    }
}
using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.BE.ActivityPub.Interfaces.Services;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Services;

public class ImportActivityService : IImportActivityService
{
    private readonly ILogger<ImportActivityService> _logger;
    private readonly IInboxRepository _inboxRepository;

    public ImportActivityService(ILogger<ImportActivityService> logger, IInboxRepository inboxRepository)
    {
        _logger = logger;
        _inboxRepository = inboxRepository;
    }

    public async Task Create(Activity activity, string activitySender)
    {
        _logger.LogTrace($"Entered {nameof(Create)}");
        
        await _inboxRepository.AddAsync(activity, activitySender);

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
    }

    public async Task Announce(Activity activity, string activitySender)
    {
        _logger.LogDebug("Got Announce");

        await _inboxRepository.AddAsync(activity, activitySender);
    }

    public async Task Follow(Activity activity, string activitySender)
    {
        _logger.LogDebug($"Got follow for \"{activity.Object?.StringLinks?.FirstOrDefault()}\" from \"{activity.Actor}\"");

        await _inboxRepository.AddAsync(activity, activitySender);

        var actorSecrets = await _activityHandler.GetActorSecretsAsync(actorId, GeneralConstants.DomainName);
        var actor = await _activityHandler.GetActorAsync(actorId, GeneralConstants.DomainName);

        if (actor.IsNull() || actor.Id.IsNull())
        {
            _logger.LogWarning(
                $"{nameof(actor)} or the id of this actor was null in {nameof(InboxController)}");

            return BadRequest("User not found");
        }

        var acceptActivity = new Activity
        {
            Id = new Uri($"https://{GeneralConstants.DomainName}/accepts/{Guid.NewGuid()}"),
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
    }

    public async Task Like(Activity activity, string activitySender)
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
    }

    public async Task Accept(Activity activity, string activitySender)
    {
        _logger.LogTrace("Got an Accept activity");

        if (activity.Object?.Objects?.FirstOrDefault()?.Id.IsNull() ?? true)
        {
            return;
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
    }
    
    public async Task Update(Activity activity, string activitySender)
    {
        var postDefinitionBuilder = Builders<Activity>.Filter;
        var postFilter = postDefinitionBuilder.Where(i => i.Type == "Create" && i.Id == activity.Id);

        await _repository.Update(activity, postFilter, DatabaseLocations.Activity.Database, activitySender);
    }

    public async Task Delete(Activity activity, string activitySender)
    {
        var definitionBuilder = Builders<Activity>.Filter;
        var filter = definitionBuilder.Where(i => i.Type == "Create" && i.Id == activity.Id);

        var specificItem =
            await _repository.GetSpecificItem(filter, DatabaseLocations.Activity.Database, activitySender);

        if (activity.Actor == specificItem.Actor)
            await _repository.Delete(filter, DatabaseLocations.Activity.Database, activitySender);
    }

    public async Task Undo(Activity activity, string activitySender)
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
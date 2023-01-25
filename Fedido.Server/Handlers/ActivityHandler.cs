using CommonExtensions;
using Fedido.Server.Extensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Authentication;
using Fedido.Server.Model.Helpers;
using MongoDB.Driver;

namespace Fedido.Server.Handlers;

public class ActivityHandler : IActivityHandler
{
    private readonly IActivityAPI _activityApi;
    private readonly IActorAPI _actorApi;
    private readonly ICollectionApi _collectionApi;
    private readonly ILogger<ActivityHandler> _logger;
    private readonly IMongoDbRepository _repository;
    private readonly IKnownSharedInboxHandler _sharedInboxHandler;

    public ActivityHandler(ILogger<ActivityHandler> logger, IMongoDbRepository repository, IActorAPI actorApi,
        IActivityAPI activityApi, IKnownSharedInboxHandler sharedInboxHandler, ICollectionApi collectionApi)
    {
        _logger = logger;
        _repository = repository;
        _actorApi = actorApi;
        _activityApi = activityApi;
        _sharedInboxHandler = sharedInboxHandler;
        _collectionApi = collectionApi;
    }

    public async Task<Actor> GetActorAsync(Guid userId, string domainName)
    {
        var filterActorDefinitionBuilder = Builders<Actor>.Filter;
        var filterActor = filterActorDefinitionBuilder.Eq(i => i.Id,
            new Uri($"https://{domainName}/actor/{userId}"));
        var actor = await _repository.GetSpecificItem(filterActor, DatabaseLocations.Actors.Database,
            DatabaseLocations.Actors.Collection);
        return actor;
    }

    public async Task<bool> SendActivitiesAsync(Activity activity, User user, Actor actor)
    {
        var everythingSuccessful = true;

        var targets = new HashSet<ServerNameInboxPair>();

        var receivers = new List<string>();

        if (activity.To.IsNotNullOrEmpty()) receivers.AddRange(activity.To);
        if (activity.Bcc.IsNotNullOrEmpty()) receivers.AddRange(activity.Bcc);
        if (activity.Audience.IsNotNullOrEmpty()) receivers.AddRange(activity.Audience);
        if (activity.Bto.IsNotNullOrEmpty()) receivers.AddRange(activity.Bto);
        if (activity.Cc.IsNotNullOrEmpty()) receivers.AddRange(activity.Cc);

        if (activity.IsActivityPublic()) // Public Post
        {
            // Send to all receivers and to all known SharedInboxes

            foreach (var item in receivers)
            {
                if (item is "https://www.w3.org/ns/activitystreams#Public" or "as:Public" or "public") continue;

                var serverNameInboxPair = await GetServerNameInboxPair(new Uri(item), true);
                if (serverNameInboxPair.IsNotNull())
                {
                    targets.Add(serverNameInboxPair);
                }
                else
                {
                    var serverNameInboxPairs = await GetServerNameInboxPairsAsync(new Uri(item), true);
                    foreach (var inboxPair in serverNameInboxPairs) targets.Add(inboxPair);
                }
            }

            foreach (var item in await _sharedInboxHandler.GetSharedInboxesAsync())
                targets.Add(new ServerNameInboxPair
                {
                    Inbox = item,
                    ServerName = item.Host
                });
        }
        else // Private Post
        {
            // Send to all receivers

            foreach (var item in receivers)
            {
                var serverNameInboxPair = await GetServerNameInboxPair(new Uri(item), false);
                if (serverNameInboxPair.IsNotNull())
                {
                    targets.Add(serverNameInboxPair);
                }
                else
                {
                    var serverNameInboxPairs = await GetServerNameInboxPairsAsync(
                        new Uri(item), false);
                    foreach (var inboxPair in serverNameInboxPairs) targets.Add(inboxPair);
                }
            }
        }

        // This List is only needed to make sure the HasSet works as expected
        // If you are sure it works you can remove it
        var inboxes = new List<Uri>();

        foreach (var target in targets)
        {
            if (inboxes.Contains(target.Inbox))
            {
                _logger.LogWarning($"Duplicate found in {nameof(inboxes)} / {nameof(targets)}");

                continue;
            }

            inboxes.Add(target.Inbox);

            for (var i = 0; i < 5; i++)
            {
                if (await _activityApi.SendActivity(activity, user, target, actor)) break;

                if (i == 4) everythingSuccessful = false;

                Thread.Sleep(10000);
            }
        }

        return everythingSuccessful;
    }

    public async Task<IEnumerable<ServerNameInboxPair>> GetServerNameInboxPairsAsync(Uri target, bool isPublic)
    {
        var serverNameInboxPairs = new List<ServerNameInboxPair>();

        var orderedCollection = await _collectionApi.GetOrderedCollection<Uri>(target);

        if (orderedCollection.IsNull())
        {
            var collection = await _collectionApi.GetCollection<Uri>(target);

            if (collection.IsNull())
                _logger.LogWarning($"Could not retrieve an object in {nameof(GetServerNameInboxPairsAsync)} -> " +
                                   $"{nameof(ActivityHandler)} with {nameof(target)}=\"{target}\"");
            else
                foreach (var item in collection.Items)
                {
                    var serverNameInboxPair = await GetServerNameInboxPair(item, isPublic);

                    if (serverNameInboxPair.IsNotNull())
                        serverNameInboxPairs.Add(serverNameInboxPair);
                    else
                        _logger.LogWarning("Someone hit second layer of collections");
                    // Here would start another layer of unwrapping collections
                    // This can be made but is not necessary
                }
        }
        else
        {
            foreach (var item in orderedCollection.OrderedItems)
            {
                var serverNameInboxPair = await GetServerNameInboxPair(item, isPublic);

                if (serverNameInboxPair.IsNotNull())
                    serverNameInboxPairs.Add(serverNameInboxPair);
                else
                    _logger.LogWarning("Someone hit second layer of collections");
                // Here would start another layer of unwrapping collections
                // This can be made but is not necessary
            }
        }

        return serverNameInboxPairs;
    }

    public async Task<ServerNameInboxPair?> GetServerNameInboxPair(Uri actorUri, bool isPublic)
    {
        var actor = await _actorApi.GetActor(actorUri);

        if (actor.IsNull()) return null;

        if (isPublic) // Public Activity
        {
            var sharedInbox = actor?.Endpoints?.SharedInbox;

            if (sharedInbox.IsNull())
            {
                if (actor.Inbox.IsNull()) return null;

                return new ServerNameInboxPair
                {
                    Inbox = actor?.Inbox,
                    ServerName = actor?.Inbox?.Host
                };
            }

            await _sharedInboxHandler.AddSharedInboxAsync(sharedInbox);

            return new ServerNameInboxPair
            {
                Inbox = sharedInbox,
                ServerName = sharedInbox?.Host
            };
        }

        // Private Activity
        return new ServerNameInboxPair
        {
            Inbox = actor?.Inbox,
            ServerName = actor?.Inbox?.Host
        };
    }
}
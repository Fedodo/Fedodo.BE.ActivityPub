using CommonExtensions;
using Fedodo.BE.ActivityPub.Extensions;
using Fedodo.BE.ActivityPub.Interfaces;
using Fedodo.BE.ActivityPub.Model.DTOs;
using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.ActivityPub.Model.ObjectTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Fedodo.NuGet.Common.Models;
using MongoDB.Driver;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Handlers;

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

    public async Task<Activity?> CreateActivity(Guid userId, CreateActivityDto activityDto, string domainName)
    {
        var activityId = Guid.NewGuid();
        var actorId = new Uri($"https://{domainName}/actor/{userId}");
        object? obj;

        var activity = new Activity
        {
            Actor = new TripleSet<Object>()
            {
                StringLinks = new[]
                {
                    actorId
                }
            },
            Id = new Uri($"https://{domainName}/outbox/{activityDto.Type}/{activityId}".ToLower()),
            Type = activityDto.Type,
            To = new TripleSet<Object>()
            {
                StringLinks = activityDto.To.Select(i => new Uri(i))
            },            
            Bto = new TripleSet<Object>()
            {
                StringLinks = activityDto.Bto.Select(i => new Uri(i))
            },            
            Cc = new TripleSet<Object>()
            {
                StringLinks = activityDto.Cc.Select(i => new Uri(i))
            },            
            Bcc = new TripleSet<Object>()
            {
                StringLinks = activityDto.Bcc.Select(i => new Uri(i))
            },            
            Audience = new TripleSet<Object>()
            {
                StringLinks = activityDto.Audience.Select(i => new Uri(i))
            },
            Published = DateTime.UtcNow
        };

        switch (activityDto.Type)
        {
            case "Create":
            {
                var createPostDto = activityDto.Object.TrySystemJsonDeserialization<Note>();

                activity.Object = new Note()
                {
                    To = createPostDto.To,
                    Name = createPostDto.Name,
                    Summary = createPostDto.Summary,
                    Sensitive = createPostDto.Sensitive,
                    InReplyTo = createPostDto.InReplyTo,
                    Content = createPostDto.Content,
                    Id = new Uri($"https://{domainName}/posts/{activityId}"),
                    Type = createPostDto.Type,
                    Published = createPostDto.Published,
                    AttributedTo = new()
                    {
                        StringLinks = new[]
                        {
                            actorId
                        }
                    },
                    Shares = new Uri($"https://{domainName}/shares/{activityId}"),
                    Likes = new Uri($"https://{domainName}/likes/{activityId}")
                };

                await _repository.Create(activity, DatabaseLocations.OutboxCreate.Database,
                    DatabaseLocations.OutboxCreate.Collection);

                break;
            }
            case "Like":
            {
                var definitionBuilder = Builders<Activity>.Filter;
                var filter = definitionBuilder.Eq(i => i.Object, activity.Object);
                var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.OutboxLike.Database,
                    DatabaseLocations.OutboxLike.Collection);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(activity, DatabaseLocations.OutboxLike.Database,
                        DatabaseLocations.OutboxLike.Collection);
                else
                    _logger.LogWarning("Got another like of the same actor.");

                break;
            }
            case "Follow":
            {
                var definitionBuilder = Builders<Activity>.Filter;
                var filter = definitionBuilder.Eq(i => i.Object, activity.Object);
                var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.OutboxFollow.Database,
                    DatabaseLocations.OutboxFollow.Collection);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(activity, DatabaseLocations.OutboxFollow.Database,
                        DatabaseLocations.OutboxFollow.Collection);
                else
                    _logger.LogWarning("Got another follow of the same actor.");

                break;
            }
            case "Announce":
            {
                var definitionBuilder = Builders<Activity>.Filter;
                var filter = definitionBuilder.Eq(i => i.Object, activity.Object);
                var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.OutboxAnnounce.Database,
                    DatabaseLocations.OutboxAnnounce.Collection);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(activity, DatabaseLocations.OutboxAnnounce.Database,
                        DatabaseLocations.OutboxAnnounce.Collection);
                else
                    _logger.LogWarning("Got another share of the same actor.");

                break;
            }
            default:
            {
                _logger.LogWarning($"Entered default case in {nameof(CreateActivity)} in {nameof(ActivityHandler)}");

                return null;
            }
        }

        return activity;
    }

    public async Task<bool> SendActivitiesAsync(Activity activity, User user, Actor actor)
    {
        var everythingSuccessful = true;

        var targets = new HashSet<ServerNameInboxPair>();

        var receivers = new List<Uri>();

        if (activity.To.StringLinks.IsNotNullOrEmpty()) receivers.AddRange(activity.To.StringLinks);
        if (activity.Bcc.StringLinks.IsNotNullOrEmpty()) receivers.AddRange(activity.Bcc.StringLinks);
        if (activity.Audience.StringLinks.IsNotNullOrEmpty()) receivers.AddRange(activity.Audience.StringLinks);
        if (activity.Bto.StringLinks.IsNotNullOrEmpty()) receivers.AddRange(activity.Bto.StringLinks);
        if (activity.Cc.StringLinks.IsNotNullOrEmpty()) receivers.AddRange(activity.Cc.StringLinks);

        if (activity.IsActivityPublic()) // Public Post
        {
            // Send to all receivers and to all known SharedInboxes

            foreach (var item in receivers)
            {
                if (item == new Uri("https://www.w3.org/ns/activitystreams#Public") || item == new Uri("as:Public") ||
                    item == new Uri("public")) continue;

                var serverNameInboxPair = await GetServerNameInboxPair(item, true);
                if (serverNameInboxPair.IsNotNull())
                {
                    targets.Add(serverNameInboxPair);
                }
                else
                {
                    var serverNameInboxPairs = await GetServerNameInboxPairsAsync(item, true);
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
                var serverNameInboxPair = await GetServerNameInboxPair(item, false);
                if (serverNameInboxPair.IsNotNull())
                {
                    targets.Add(serverNameInboxPair);
                }
                else
                {
                    var serverNameInboxPairs = await GetServerNameInboxPairsAsync(item, false);
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
                foreach (var item in collection.Items.StringLinks)
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
            foreach (var item in orderedCollection.Items.StringLinks)
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
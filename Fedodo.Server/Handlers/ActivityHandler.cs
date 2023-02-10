using CommonExtensions;
using Fedodo.Server.Extensions;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Fedodo.Server.Model.Authentication;
using Fedodo.Server.Model.DTOs;
using Fedodo.Server.Model.Helpers;
using MongoDB.Driver;

namespace Fedodo.Server.Handlers;

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
        var postId = Guid.NewGuid();
        var actorId = new Uri($"https://{domainName}/actor/{userId}");
        object? obj;

        switch (activityDto.Type)
        {
            case "Create":
            {
                var createPostDto = activityDto.Object.TrySystemJsonDeserialization<Post>();

                var post = new Post
                {
                    To = createPostDto.To,
                    Name = createPostDto.Name,
                    Summary = createPostDto.Summary,
                    Sensitive = createPostDto.Sensitive,
                    InReplyTo = createPostDto.InReplyTo,
                    Content = createPostDto.Content,
                    Id = new Uri($"https://{domainName}/posts/{postId}"),
                    Type = createPostDto.Type,
                    Published = createPostDto.Published,
                    AttributedTo = actorId,
                    Shares = new Uri($"https://{domainName}/shares/{postId}"),
                    Likes = new Uri($"https://{domainName}/likes/{postId}")
                };

                await _repository.Create(post, DatabaseLocations.OutboxNotes.Database,
                    DatabaseLocations.OutboxNotes.Collection);

                obj = post;
                break;
            }
            case "Like":
            {
                var uriString = activityDto.Object.TrySystemJsonDeserialization<string>();

                obj = uriString;

                var likeHelper = new LikeHelper
                {
                    Like = new Uri(actorId.ToString())
                };
                
                var definitionBuilder = Builders<LikeHelper>.Filter;
                var filter = definitionBuilder.Eq(i => i.Like, actorId);
                var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.Likes.Database, uriString);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(likeHelper, DatabaseLocations.Likes.Database, uriString);
                else
                    _logger.LogWarning("Got another like of the same actor.");
                
                break;
            }
            case "Follow":
            {
                // Follow does not need to be stored in the database. This happens only if the sever gets an accept.
                obj = activityDto.Object.TrySystemJsonDeserialization<string>();
                break;
            }
            case "Announce":
            {
                var uriString = activityDto.Object.TrySystemJsonDeserialization<string>();
                obj = uriString;

                var shareHelper = new ShareHelper
                {
                    Share = new Uri(actorId.ToString())
                };

                var definitionBuilder = Builders<ShareHelper>.Filter;
                var filter = definitionBuilder.Eq(i => i.Share, actorId);
                var fItem = await _repository.GetSpecificItems(filter, DatabaseLocations.Shares.Database, uriString);

                if (fItem.IsNullOrEmpty())
                    await _repository.Create(shareHelper, DatabaseLocations.Shares.Database, uriString);
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

        var activity = new Activity
        {
            Actor = actorId,
            Id = new Uri($"https://{domainName}/activitys/{postId}"),
            Type = activityDto.Type,
            To = activityDto.To,
            Bto = activityDto.Bto,
            Cc = activityDto.Cc,
            Bcc = activityDto.Bcc,
            Audience = activityDto.Audience,
            Object = obj
        };

        await _repository.Create(activity, DatabaseLocations.Activities.Database, userId.ToString());

        return activity;
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
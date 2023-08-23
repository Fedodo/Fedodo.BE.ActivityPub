using System.Text.Json;
using CommonExtensions;
using Fedodo.BE.ActivityPub.Constants;
using Fedodo.BE.ActivityPub.Extensions;
using Fedodo.BE.ActivityPub.Interfaces.APIs;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.BE.ActivityPub.Interfaces.Services;
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

namespace Fedodo.BE.ActivityPub.Services;

public class CreateActivityService : ICreateActivityService
{
    private readonly IActivityAPI _activityApi;
    private readonly IActorAPI _actorApi;
    private readonly ICollectionApi _collectionApi;
    private readonly ILogger<CreateActivityService> _logger;
    private readonly IMongoDbRepository _mongoDbRepository;
    private readonly IKnownSharedInboxService _sharedInboxService;

    public CreateActivityService(ILogger<CreateActivityService> logger, IActorAPI actorApi, IActivityAPI activityApi,
        IKnownSharedInboxService sharedInboxService, ICollectionApi collectionApi,IMongoDbRepository mongoDbRepository)
    {
        _logger = logger;
        _actorApi = actorApi;
        _activityApi = activityApi;
        _sharedInboxService = sharedInboxService;
        _collectionApi = collectionApi;
        _mongoDbRepository = mongoDbRepository;
    }

    public async Task<Activity?> CreateActivity(string actorId, CreateActivityDto activityDto)
    {
        var activityId = Guid.NewGuid();
        object? obj;

        var activity = new Activity
        {
            Actor = new TripleSet<Object>
            {
                StringLinks = new[]
                {
                    actorId
                }
            },
            Id = new Uri($"https://{GeneralConstants.DomainName}/outbox/{activityDto.Type}/{activityId}".ToLower()),
            Type = activityDto.Type,
            To = new TripleSet<Object>
            {
                StringLinks = activityDto.To?.Select(i => i)
            },
            Bto = new TripleSet<Object>
            {
                StringLinks = activityDto.Bto?.Select(i => i)
            },
            Cc = new TripleSet<Object>
            {
                StringLinks = activityDto.Cc?.Select(i => i)
            },
            Bcc = new TripleSet<Object>
            {
                StringLinks = activityDto.Bcc?.Select(i => i)
            },
            Audience = new TripleSet<Object>
            {
                StringLinks = activityDto.Audience?.Select(i => i)
            },
            Published = DateTime.UtcNow,
            Object = new TripleSet<Object>()
        };

        if (activityDto.Object.ValueKind == JsonValueKind.String)
        {
            var tempList = activity.Object.StringLinks?.ToList() ?? new List<string>();
            tempList.Add(activityDto.Object.GetString()!);
            activity.Object.StringLinks = tempList;
        }

        var definitionBuilder = Builders<Activity>.Filter;
        var filter = definitionBuilder.Where(i => i.Object == activity.Object && i.Actor == activity.Actor);

        switch (activityDto.Type)
        {
            case "Create":
            {
                var createPostDto = activityDto.Object.Deserialize<Note>();

                activity.Object = new TripleSet<Object>
                {
                    Objects = new[]
                    {
                        new Note
                        {
                            To = createPostDto?.To,
                            Name = createPostDto?.Name,
                            Summary = createPostDto?.Summary,
                            Sensitive = createPostDto?.Sensitive,
                            InReplyTo = createPostDto?.InReplyTo,
                            Content = createPostDto?.Content,
                            Id = new Uri($"https://{GeneralConstants.DomainName}/posts/{activityId}"),
                            Type = createPostDto?.Type,
                            Published = createPostDto?.Published,
                            AttributedTo = new TripleSet<Object>
                            {
                                StringLinks = new[]
                                {
                                    actorId
                                }
                            },
                            Shares = new Uri($"https://{GeneralConstants.DomainName}/shares/{activityId}"),
                            Likes = new Uri($"https://{GeneralConstants.DomainName}/likes/{activityId}")
                        }
                    }
                };

                await _mongoDbRepository.Create(activity, DatabaseLocations.Activity.Database, actorId);

                break;
            }
            case "Like":
            {
                var fItem = await _mongoDbRepository.GetSpecificItems(filter, DatabaseLocations.Activity.Database,
                    actorId);

                if (fItem.IsNullOrEmpty())
                    await _mongoDbRepository.Create(activity, DatabaseLocations.Activity.Database, actorId);
                else
                    _logger.LogWarning("Got another like of the same actor.");

                break;
            }
            case "Follow":
            {
                if (activity.Object.StringLinks?.FirstOrDefault().IsNotNullOrEmpty() ?? false)
                    await _sharedInboxService.AddSharedInboxFromActorAsync(
                        new Uri(activity.Object.StringLinks.FirstOrDefault()!));

                var fItem = await _mongoDbRepository.GetSpecificItems(filter, DatabaseLocations.Activity.Database,
                    actorId);

                if (fItem.IsNullOrEmpty())
                    await _mongoDbRepository.Create(activity, DatabaseLocations.Activity.Database, actorId);
                else
                    _logger.LogWarning("Got another follow of the same actor.");

                break;
            }
            case "Announce":
            {
                var fItem = await _mongoDbRepository.GetSpecificItems(filter, DatabaseLocations.Activity.Database,
                    actorId);

                if (fItem.IsNullOrEmpty())
                    await _mongoDbRepository.Create(activity, DatabaseLocations.Activity.Database, actorId);
                else
                    _logger.LogWarning("Got another share of the same actor.");

                break;
            }
            default:
            {
                _logger.LogWarning(
                    $"Entered default case in {nameof(CreateActivity)} in {nameof(CreateActivityService)}");

                return null;
            }
        }

        return activity;
    }

    public async Task<bool> SendActivitiesAsync(Activity activity, ActorSecrets actorSecrets, Actor actor)
    {
        _logger.LogTrace($"Entered {nameof(SendActivitiesAsync)} in {nameof(CreateActivityService)}");

        var everythingSuccessful = true;

        var targets = new HashSet<ServerNameInboxPair>();

        var receivers = new List<string>();

        if (activity.To?.StringLinks.IsNotNullOrEmpty() ?? false) receivers.AddRange(activity.To.StringLinks);
        if (activity.Bcc?.StringLinks.IsNotNullOrEmpty() ?? false) receivers.AddRange(activity.Bcc.StringLinks);
        if (activity.Audience?.StringLinks.IsNotNullOrEmpty() ?? false)
            receivers.AddRange(activity.Audience.StringLinks);
        if (activity.Bto?.StringLinks.IsNotNullOrEmpty() ?? false) receivers.AddRange(activity.Bto.StringLinks);
        if (activity.Cc?.StringLinks.IsNotNullOrEmpty() ?? false) receivers.AddRange(activity.Cc.StringLinks);

        if (activity.IsActivityPublic()) // Public Post
        {
            // Send to all receivers and to all known SharedInboxes

            _logger.LogDebug("Is public post");

            foreach (var item in receivers)
            {
                if (item is "https://www.w3.org/ns/activitystreams#Public" or "as:Public" or "public") continue;

                var serverNameInboxPair = await GetServerNameInboxPairAsync(new Uri(item), true);
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

            foreach (var item in await _sharedInboxService.GetSharedInboxesAsync())
                targets.Add(new ServerNameInboxPair
                {
                    Inbox = item,
                    ServerName = item.Host
                });
        }
        else // Private Post
        {
            // Send to all receivers

            _logger.LogDebug("Is private post");

            foreach (var item in receivers)
            {
                var serverNameInboxPair = await GetServerNameInboxPairAsync(new Uri(item), false);
                if (serverNameInboxPair.IsNotNull())
                {
                    targets.Add(serverNameInboxPair);
                }
                else
                {
                    var serverNameInboxPairs = await GetServerNameInboxPairsAsync(new Uri(item), false);
                    foreach (var inboxPair in serverNameInboxPairs) targets.Add(inboxPair);
                }
            }
        }

        _logger.LogDebug("Generated targets");

        // This List is only needed to make sure the HashSet works as expected
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
                if (await _activityApi.SendActivity(activity, actorSecrets, target, actor)) break;

                if (i == 4) everythingSuccessful = false;

                Thread.Sleep(10000); // This should be done in another way
            }
        }

        _logger.LogTrace($"Left {nameof(SendActivitiesAsync)} in {nameof(CreateActivityService)}");

        return everythingSuccessful;
    }

    public async Task<ServerNameInboxPair?> GetServerNameInboxPairAsync(Uri actorUri, bool isPublic)
    {
        var actor = await _actorApi.GetActor(actorUri);

        if (actor.IsNull()) return null;
        if (actor.Inbox.IsNull()) return null;

        if (isPublic) // Public Activity
        {
            var sharedInbox = actor?.Endpoints?.SharedInbox;

            if (sharedInbox.IsNull())
                return new ServerNameInboxPair
                {
                    Inbox = actor.Inbox,
                    ServerName = actor.Inbox.Host
                };

            await _sharedInboxService.AddSharedInboxAsync(sharedInbox);

            return new ServerNameInboxPair
            {
                Inbox = sharedInbox,
                ServerName = sharedInbox.Host
            };
        }

        // Private Activity
        return new ServerNameInboxPair
        {
            Inbox = actor.Inbox,
            ServerName = actor.Inbox.Host
        };
    }

    public async Task<IEnumerable<ServerNameInboxPair>> GetServerNameInboxPairsAsync(Uri target, bool isPublic)
    {
        var serverNameInboxPairs = new List<ServerNameInboxPair>();

        var orderedCollection = await _collectionApi.GetOrderedCollection<Uri>(target);

        if (orderedCollection.IsNull())
        {
            var collection = await _collectionApi.GetCollection<Uri>(target);

            if (collection.IsNull() || collection.Items.IsNull() || collection.Items.StringLinks.IsNull())
                _logger.LogWarning($"Could not retrieve an object in {nameof(GetServerNameInboxPairsAsync)} -> " +
                                   $"{nameof(CreateActivityService)} with {nameof(target)}=\"{target}\"");
            else
                foreach (var item in collection.Items.StringLinks)
                {
                    var serverNameInboxPair = await GetServerNameInboxPairAsync(new Uri(item), isPublic);

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
            if (orderedCollection.IsNull() || orderedCollection.Items.IsNull() ||
                orderedCollection.Items.StringLinks.IsNull())
                _logger.LogWarning($"Could not retrieve an object in {nameof(GetServerNameInboxPairsAsync)} -> " +
                                   $"{nameof(CreateActivityService)} with {nameof(target)}=\"{target}\"");
            else
                foreach (var item in orderedCollection.Items.StringLinks)
                {
                    var serverNameInboxPair = await GetServerNameInboxPairAsync(new Uri(item), isPublic);

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
}
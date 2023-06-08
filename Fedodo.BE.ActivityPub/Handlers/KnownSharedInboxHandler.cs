using CommonExtensions;
using Fedodo.BE.ActivityPub.Interfaces;
using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Handlers;

public class KnownSharedInboxHandler : IKnownSharedInboxHandler
{
    private readonly ILogger<KnownSharedInboxHandler> _logger;
    private readonly IMongoDbRepository _repository;
    private readonly IActorAPI _actorApi;

    public KnownSharedInboxHandler(ILogger<KnownSharedInboxHandler> logger, IMongoDbRepository repository, IActorAPI actorApi)
    {
        _logger = logger;
        _repository = repository;
        _actorApi = actorApi;
    }

    public async Task AddSharedInboxFromActorAsync(Uri actorId)
    {
        var actor = await _actorApi.GetActor(actorId);
        var sharedInbox = actor?.Endpoints?.SharedInbox;

        if (sharedInbox.IsNotNull())
        {
            await AddSharedInboxAsync(sharedInbox);
        }
    }

    public async Task AddSharedInboxAsync(Uri sharedInbox)
    {
        var filterDefinitionBuilder = Builders<SharedInbox>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i.SharedInboxUri == sharedInbox);
        var items = await _repository.GetSpecificItems(filter, DatabaseLocations.KnownSharedInbox.Database,
            DatabaseLocations.KnownSharedInbox.Collection);

        if (!items.Any())
            await _repository.Create(new SharedInbox { SharedInboxUri = sharedInbox },
                DatabaseLocations.KnownSharedInbox.Database, DatabaseLocations.KnownSharedInbox.Collection);
    }

    public async Task<IEnumerable<Uri>> GetSharedInboxesAsync()
    {
        var sharedInbox = await _repository.GetAll<SharedInbox>(DatabaseLocations.KnownSharedInbox.Database,
            DatabaseLocations.KnownSharedInbox.Collection);
        return sharedInbox.Select(i => i.SharedInboxUri);
    }
}
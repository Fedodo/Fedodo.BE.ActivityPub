using CommonExtensions;
using Fedodo.BE.ActivityPub.Interfaces.APIs;
using Fedodo.BE.ActivityPub.Interfaces.Services;
using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Services;

public class KnownSharedInboxService : IKnownSharedInboxService
{
    private readonly IActorAPI _actorApi;
    private readonly ILogger<KnownSharedInboxService> _logger;
    private readonly IMongoDbRepository _repository;

    public KnownSharedInboxService(ILogger<KnownSharedInboxService> logger, IMongoDbRepository repository,
        IActorAPI actorApi)
    {
        _logger = logger;
        _repository = repository;
        _actorApi = actorApi;
    }

    public async Task AddSharedInboxFromActorAsync(Uri actorId)
    {
        var actor = await _actorApi.GetActor(actorId);
        var sharedInbox = actor?.Endpoints?.SharedInbox;

        if (sharedInbox.IsNotNull()) await AddSharedInboxAsync(sharedInbox);
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
using Fedido.Server.Interfaces;
using Fedido.Server.Model.Helpers;
using MongoDB.Driver;

namespace Fedido.Server.Handlers;

public class KnownSharedInboxHandler : IKnownSharedInboxHandler
{
    private readonly ILogger<KnownSharedInboxHandler> _logger;
    private readonly IMongoDbRepository _repository;

    public KnownSharedInboxHandler(ILogger<KnownSharedInboxHandler> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task AddSharedInbox(Uri sharedInbox)
    {
        var filterDefinitionBuilder = Builders<SharedInbox>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i.SharedInboxUri == sharedInbox);
        var items = await _repository.GetSpecificItems(filter, DatabaseLocations.KnownSharedInbox.Database,
            DatabaseLocations.KnownSharedInbox.Collection);

        if (!items.Any())
            await _repository.Create(new SharedInbox { SharedInboxUri = sharedInbox },
                DatabaseLocations.KnownSharedInbox.Database, DatabaseLocations.KnownSharedInbox.Collection);
    }

    public async Task<IEnumerable<Uri>> GetSharedInboxes()
    {
        var sharedInbox = await _repository.GetAll<SharedInbox>(DatabaseLocations.KnownSharedInbox.Database,
            DatabaseLocations.KnownSharedInbox.Collection);
        return sharedInbox.Select(i => i.SharedInboxUri);
    }
}
using Fedido.Server.Interfaces;
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
        var filterDefinitionBuilder = Builders<Uri>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i == sharedInbox);
        var items = await _repository.GetSpecificItems(filter, "ForeignData", "SharedInboxes");

        if (!items.Any()) await _repository.Create(sharedInbox, "ForeignData", "SharedInboxes");
    }

    public async Task<IEnumerable<Uri>> GetSharedInboxes()
    {
        return await _repository.GetAll<Uri>("ForeignData", "SharedInboxes");
    }
}
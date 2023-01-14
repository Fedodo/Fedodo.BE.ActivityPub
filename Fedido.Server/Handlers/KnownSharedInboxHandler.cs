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
        var items = await _repository.GetSpecificItems(filter, "ForeignData", "SharedInboxes");

        if (!items.Any())
            await _repository.Create(new SharedInbox { SharedInboxUri = sharedInbox }, "ForeignData", "SharedInboxes");
    }

    public async Task<IEnumerable<Uri>> GetSharedInboxes()
    {
        var sharedInbox = await _repository.GetAll<SharedInbox>("ForeignData", "SharedInboxes");
        return sharedInbox.Select(i => i.SharedInboxUri);
    }
}
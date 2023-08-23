using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly ILogger<OutboxRepository> _logger;
    private readonly IMongoDbRepository _mongoDbRepository;

    public OutboxRepository(ILogger<OutboxRepository> logger, IMongoDbRepository mongoDbRepository)
    {
        _logger = logger;
        _mongoDbRepository = mongoDbRepository;
    }

    public async Task<long> CountOutboxActivitiesAsync(string actorId)
    {
        // This filter can not use the extensions method IsPostPublic
        var filterDefinitionBuilder = Builders<Activity>.Filter;
        // You have to do it like this because if you make everything in one call MongoDB does not like it anymore.
        var filter = filterDefinitionBuilder.Where(i => i.To.StringLinks.Any(item =>
            item == "https://www.w3.org/ns/activitystreams#Public") || i.To.StringLinks.Any(item =>
            item == "as:Public") || i.To.StringLinks.Any(item => item == "public"));

        var postCount = await _mongoDbRepository.CountSpecific(DatabaseLocations.Activity.Database, actorId, filter);

        return postCount;
    }

    public async Task<IEnumerable<Activity>> GetPagedAsync(string actorId, int pageId)
    {
        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.To.StringLinks.Any(item =>
            item == "https://www.w3.org/ns/activitystreams#Public") || i.To.StringLinks.Any(item =>
            item == "as:Public") || i.To.StringLinks.Any(item => item == "public"));

        var page = await _mongoDbRepository.GetSpecificPaged(DatabaseLocations.Activity.Database,
            actorId, pageId, 20, sort, filter);

        return page;
    }
}
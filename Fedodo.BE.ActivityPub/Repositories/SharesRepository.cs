using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class SharesRepository : ISharesRepository
{
    private readonly ILogger<SharesRepository> _logger;
    private readonly IMongoDbRepository _mongoDbRepository;

    public SharesRepository(ILogger<SharesRepository> logger, IMongoDbRepository mongoDbRepository)
    {
        _logger = logger;
        _mongoDbRepository = mongoDbRepository;
    }

    public async Task<List<Activity>> GetSharesAsync(string activityId, int page)
    {
        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filter = GetFilter(activityId);

        var collections = _mongoDbRepository.GetCollectionNames(DatabaseLocations.Activity.Database);

        var shares = (await _mongoDbRepository.GetSpecificPagedFromCollections(DatabaseLocations.Activity.Database,
            collections, page, 20, sort, filter)).ToList();

        return shares;
    }

    public async Task<long> CountAsync(string activityId)
    {
        var filter = GetFilter(activityId);

        var collections = _mongoDbRepository.GetCollectionNames(DatabaseLocations.Activity.Database);

        var postCount = await _mongoDbRepository.CountSpecificFromCollections(DatabaseLocations.Activity.Database,
            collections, filter);

        return postCount;
    }

    private FilterDefinition<Activity> GetFilter(string activityId)
    {
        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i =>
            i.Type == "Announce" && i.Object != null && i.Object.StringLinks != null &&
            i.Object.StringLinks.FirstOrDefault() == activityId);
        return filter;
    }
}
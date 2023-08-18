using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class LikesRepository : ILikesRepository
{
    private readonly ILogger<LikesRepository> _logger;
    private readonly IMongoDbRepository _mongoDbRepository;

    public LikesRepository(ILogger<LikesRepository> logger, IMongoDbRepository mongoDbRepository)
    {
        _logger = logger;
        _mongoDbRepository = mongoDbRepository;
    }

    public async Task<long> CountLikesAsync(string activityId)
    {
        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object!.StringLinks!.First() == activityId);

        var collections = _mongoDbRepository.GetCollectionNames(DatabaseLocations.Activity.Database);

        var postCount =
            await _mongoDbRepository.CountSpecificFromCollections(DatabaseLocations.Activity.Database, collections,
                filter);

        return postCount;
    }

    public async Task<IEnumerable<Activity>> GetLikesAsync(string activityId, int page)
    {
        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object!.StringLinks!.First() == activityId);

        var collections = _mongoDbRepository.GetCollectionNames(DatabaseLocations.Activity.Database);

        var posts = await _mongoDbRepository.GetSpecificPagedFromCollections(
            databaseName: DatabaseLocations.Activity.Database,
            collectionNames: collections,
            filter: filter,
            pageId: page,
            pageSize: 20,
            sortDefinition: sort);

        return posts;
    }
}
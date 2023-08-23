using CommonExtensions;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class FollowingRepository : IFollowingRepository
{
    private readonly FilterDefinition<Activity> _filterDefinition;
    private readonly ILogger _logger;
    private readonly IMongoDbRepository _mongoDbRepository;

    public FollowingRepository(IMongoDbRepository mongoDbRepository, ILogger logger)
    {
        _mongoDbRepository = mongoDbRepository;
        _logger = logger;

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        _filterDefinition = filterBuilder.Where(i => i.Type == "Follow");
    }

    public async Task<IEnumerable<Activity>> GetFollowingsPageAsync(string actorId, int page)
    {
        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var followings = await _mongoDbRepository.GetSpecificPaged(DatabaseLocations.Activity.Database,
            actorId, page, 20, sort, _filterDefinition);

        return followings;
    }

    public async Task<long> CountFollowingsAsync(string actorId)
    {
        var postCount =
            await _mongoDbRepository.CountSpecific(DatabaseLocations.Activity.Database, actorId, _filterDefinition);
        return postCount;
    }

    public async Task<IEnumerable<string>> GetAllFollowingsAsync(string actorId)
    {
        var items = await _mongoDbRepository.GetSpecificItems(_filterDefinition, DatabaseLocations.Activity.Database,
            actorId);
        return items.Select(i => i.Object?.StringLinks?.FirstOrDefault()).Where(i => i.IsNotNullOrEmpty())!;
    }
}
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class NodeInfoRepository : INodeInfoRepository
{
    private readonly ILogger<NodeInfoRepository> _logger;
    private readonly IMongoDbRepository _mongoDbRepository;
    private readonly IUserRepository _userRepository;

    public NodeInfoRepository(ILogger<NodeInfoRepository> logger, IMongoDbRepository mongoDbRepository,
        IUserRepository userRepository)
    {
        _logger = logger;
        _mongoDbRepository = mongoDbRepository;
        _userRepository = userRepository;
    }

    public async Task<long> CountLocalPostsAsync()
    {
        var actors = (await _userRepository.GetActorsAsync())?.Select(i => i.Id?.ToString()) ??
                     new List<string>();

        var collections = _mongoDbRepository.GetCollectionNames(DatabaseLocations.Activity.Database);

        collections = collections.Where(i => actors.Any(j => i == j));

        var definitionBuilder = Builders<Activity>.Filter;
        var filter = definitionBuilder.Where(i => i.Type == "Create" || i.Type == "Announce");
        var count = await _mongoDbRepository.CountSpecificFromCollections(DatabaseLocations.Activity.Database,
            collections, filter);

        return count;
    }

    public async Task<long> CountLocalActorsAsync()
    {
        var count = await _mongoDbRepository.CountAll<Activity>(DatabaseLocations.Actors.Database,
            DatabaseLocations.Actors.Collection);
        return count;
    }
}
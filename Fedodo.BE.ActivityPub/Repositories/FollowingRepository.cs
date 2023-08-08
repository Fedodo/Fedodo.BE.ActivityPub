using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class FollowingRepository : IFollowingRepository
{
    private readonly IMongoDbRepository _mongoDbRepository;

    public FollowingRepository(IMongoDbRepository mongoDbRepository)
    {
        _mongoDbRepository = mongoDbRepository;
    }
    
    public async Task<List<Activity>> GetFollowingsPage(string actorId, int page)
    {
        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i =>
            i.Type == "Follow" && i.Actor != null && i.Actor.StringLinks != null &&
            i.Actor.StringLinks.ToList()[0].ToString() == actorId);

        var followings = (await _mongoDbRepository.GetSpecificPaged(DatabaseLocations.Activity.Database,
            actorId, page, 20, sort, filter)).ToList();
        
        return followings;
    }
}
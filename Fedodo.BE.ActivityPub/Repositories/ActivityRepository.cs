using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class ActivityRepository : IActivityRepository
{
    private readonly ILogger<ActivityRepository> _logger;
    private readonly IMongoDbRepository _mongoDbRepository;

    public ActivityRepository(ILogger<ActivityRepository> logger, IMongoDbRepository mongoDbRepository)
    {
        _logger = logger;
        _mongoDbRepository = mongoDbRepository;
    }

    public async Task<Activity> GetActivityByIdAsync(string activityId)
    {
        var filterDefinitionBuilder = Builders<Activity>.Filter;
        var filter = filterDefinitionBuilder.Eq(i => i.Id, new Uri(activityId));

        var collections = _mongoDbRepository.GetCollectionNames(DatabaseLocations.Activity.Database);

        var post = await _mongoDbRepository.GetSpecificItemFromCollections(filter, DatabaseLocations.Activity.Database,
            collections);

        return post;
    }

    public async Task<Activity?> GetActivityByIdAsync(Uri id, string activitySender)
    {
        var actorDefinitionBuilder = Builders<Activity>.Filter;
        var filter = actorDefinitionBuilder.Where(i => i.Id == id);
        var sendActivity =
            await _mongoDbRepository.GetSpecificItem(filter, DatabaseLocations.Activity.Database, activitySender);
        return sendActivity;
    }

    public async Task DeleteActivityByIdAsync(Uri id, string activitySender)
    {
        var actorDefinitionBuilder = Builders<Activity>.Filter;
        var filter = actorDefinitionBuilder.Where(i => i.Id == id);
        await _mongoDbRepository.Delete(filter, DatabaseLocations.Activity.Database, activitySender);
    }
}
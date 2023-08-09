using CommonExtensions;
using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class InboxRepository : IInboxRepository
{
    private readonly ILogger<InboxRepository> _logger;
    private readonly IMongoDbRepository _mongoDbRepository;
    private readonly IFollowingRepository _followingRepository;

    public InboxRepository(ILogger<InboxRepository> logger, IMongoDbRepository mongoDbRepository,
        IFollowingRepository followingRepository)
    {
        _logger = logger;
        _mongoDbRepository = mongoDbRepository;
        _followingRepository = followingRepository;
    }

    public async Task<long> CountInboxItemsAsync(string actorId)
    {
        var filterBuilder = Builders<Activity>.Filter;
        var filter = filterBuilder.Where(i => i.Type == "Create" || i.Type == "Announce");

        var followings = await _followingRepository.GetAllFollowingsAsync(actorId);

        var postCount =
            await _mongoDbRepository.CountSpecificFromCollections(DatabaseLocations.Activity.Database, followings,
                filter);

        return postCount;
    }

    public async Task<List<Activity>> GetPagedAsync(string actorId, int page)
    {
        var builder = Builders<Activity>.Sort;
        var sort = builder.Descending(i => i.Published);

        var filterBuilder = Builders<Activity>.Filter;
        var filter = filterBuilder.Where(i => i.To != null && i.To.StringLinks != null && 
            (i.To.StringLinks.Any(item => item == "https://www.w3.org/ns/activitystreams#Public") || 
             i.To.StringLinks.Any(item => item == "as:Public")) && (i.Type == "Create" || i.Type == "Announce"));

        var followings = await _followingRepository.GetAllFollowingsAsync(actorId);

        var activities = (await _mongoDbRepository.GetSpecificPagedFromCollections(filter: filter,
            databaseName: DatabaseLocations.Activity.Database, collectionNames: followings, pageId: page, pageSize: 20,
            sortDefinition: sort)).ToList();

        return activities;
    }

    public async Task AddAsync(Activity activity, string activitySender)
    {
        var activityDefinitionBuilder = Builders<Activity>.Filter;
        var postFilter = activityDefinitionBuilder.Where(i => i.Id == activity.Id);
        var fItem = await _mongoDbRepository.GetSpecificItems(postFilter, DatabaseLocations.Activity.Database, activitySender);

        if (fItem.IsNotNullOrEmpty())
        {
            throw new ArgumentException("Item already exists");
        }

        await _mongoDbRepository.Create(activity, DatabaseLocations.Activity.Database, activitySender);
    }
}
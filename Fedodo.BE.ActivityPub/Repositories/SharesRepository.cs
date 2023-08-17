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

        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object!.StringLinks!.First() == activityId);

        var shares = (await _mongoDbRepository.GetSpecificPaged(DatabaseLocations.Activity.Database,
            DatabaseLocations.OutboxAnnounce.Collection, page, 20, sort, filter)).ToList();
        
        return shares;
    }

    public async Task<long> CountAsync(string activityId)
    {
        var filterBuilder = new FilterDefinitionBuilder<Activity>();
        var filter = filterBuilder.Where(i => i.Object!.StringLinks!.First() == activityId);

        var postCount = await _mongoDbRepository.CountSpecific(DatabaseLocations.Activity.Database,
            DatabaseLocations.InboxAnnounce.Collection, filter);
       
        return postCount;
    }
}
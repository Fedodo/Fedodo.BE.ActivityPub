using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface IInboxRepository
{
    public Task<long> CountInboxItemsAsync(string actorId);
    public Task<List<Activity>> GetPagedAsync(string actorId, int page);
    public Task AddAsync(Activity activity, string activitySender);
    public Task UpdateAsync(Activity activity, string activitySender);
}
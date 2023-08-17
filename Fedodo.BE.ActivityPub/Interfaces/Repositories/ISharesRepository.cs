using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface ISharesRepository
{
    public Task<List<Activity>> GetSharesAsync(string activityId, int page);
    public Task<long> CountAsync(string activityId);
}
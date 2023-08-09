using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface IFollowingRepository
{
    public Task<IEnumerable<Activity>> GetFollowingsPageAsync(string actorId, int page);
    public Task<long> CountFollowingsAsync(string actorId);
    public Task<IEnumerable<string>> GetAllFollowingsAsync(string actorId);
}
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface IFollowerRepository
{
    public Task<IEnumerable<Activity>> GetFollowersPagedAsync(string actorId, int page);
    public Task<long> CountFollowersAsync(string actorId);
}
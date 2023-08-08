using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface IFollowingRepository
{
    public Task<List<Activity>> GetFollowingsPage(string actorId, int page);
}
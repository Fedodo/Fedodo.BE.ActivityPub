using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface ILikesRepository
{
    public Task<long> CountLikesAsync(string activityId);
    public Task<IEnumerable<Activity>> GetLikesAsync(string activityId, int page);
}
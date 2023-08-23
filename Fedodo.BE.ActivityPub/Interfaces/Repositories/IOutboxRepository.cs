using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface IOutboxRepository
{
    public Task<long> CountOutboxActivitiesAsync(string actorId);
    public Task<IEnumerable<Activity>> GetPagedAsync(string actorId, int pageId);
}
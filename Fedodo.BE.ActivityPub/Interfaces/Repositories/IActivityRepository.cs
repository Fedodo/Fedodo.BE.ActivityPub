using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface IActivityRepository
{
    public Task<Activity?> GetActivityByIdAsync(Uri id, string activitySender);
    public Task DeleteActivityByIdAsync(Uri id, string activitySender);
    public Task<Activity> GetActivityByIdAsync(string activityId);
}
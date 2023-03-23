using Fedodo.BE.ActivityPub.Model.ActivityPub;
using Fedodo.BE.ActivityPub.Model.DTOs;
using Fedodo.NuGet.Common.Models;

namespace Fedodo.BE.ActivityPub.Interfaces;

public interface IActivityHandler
{
    public Task<bool> SendActivitiesAsync(Activity activity, User user, Actor actor);
    public Task<Actor> GetActorAsync(Guid userId, string domainName);
    public Task<Activity?> CreateActivity(Guid userId, CreateActivityDto activityDto, string domainName);
}
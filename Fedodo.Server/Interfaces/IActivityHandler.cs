using Fedodo.Server.Model.ActivityPub;
using Fedodo.Server.Model.Authentication;
using Fedodo.Server.Model.DTOs;

namespace Fedodo.Server.Interfaces;

public interface IActivityHandler
{
    public Task<bool> SendActivitiesAsync(Activity activity, User user, Actor actor);
    public Task<Actor> GetActorAsync(Guid userId, string domainName);
    public Task<Activity?> CreateActivity(Guid userId, CreateActivityDto activityDto, string domainName);
}
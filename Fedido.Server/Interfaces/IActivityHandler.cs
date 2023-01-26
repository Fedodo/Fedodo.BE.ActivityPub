using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Authentication;
using Fedido.Server.Model.DTOs;

namespace Fedido.Server.Interfaces;

public interface IActivityHandler
{
    public Task<bool> SendActivitiesAsync(Activity activity, User user, Actor actor);
    public Task<Actor> GetActorAsync(Guid userId, string domainName);
    public Task<Activity?> CreateActivity(Guid userId, CreateActivityDto activityDto, string domainName);
}
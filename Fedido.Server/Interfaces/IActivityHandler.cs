using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Authentication;

namespace Fedido.Server.Interfaces;

public interface IActivityHandler
{
    public Task SendActivities(Activity activity, User user, Actor actor);
    public Task<Actor> GetActor(Guid userId);
}
using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.Authentication;

namespace ActivityPubServer.Interfaces;

public interface IActivityHandler
{
    public Task SendActivities(Activity activity, User user, Actor actor);
    public Task<Actor> GetActor(Guid userId);
}
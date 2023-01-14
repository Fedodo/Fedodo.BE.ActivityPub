using Fedido.Server.Model.ActivityPub;

namespace Fedido.Server.Interfaces;

public interface IActorAPI
{
    public Task<Actor?> GetActor(Uri actorId);
}
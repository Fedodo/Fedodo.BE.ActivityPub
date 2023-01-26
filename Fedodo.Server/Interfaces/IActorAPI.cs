using Fedodo.Server.Model.ActivityPub;

namespace Fedodo.Server.Interfaces;

public interface IActorAPI
{
    public Task<Actor?> GetActor(Uri actorId);
}
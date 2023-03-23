using Fedodo.BE.ActivityPub.Model.ActivityPub;

namespace Fedodo.BE.ActivityPub.Interfaces;

public interface IActorAPI
{
    public Task<Actor?> GetActor(Uri actorId);
}
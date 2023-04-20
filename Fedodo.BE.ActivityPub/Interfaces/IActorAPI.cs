
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;

namespace Fedodo.BE.ActivityPub.Interfaces;

public interface IActorAPI
{
    public Task<Actor?> GetActor(Uri actorId);
}
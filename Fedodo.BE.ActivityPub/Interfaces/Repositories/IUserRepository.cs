using Fedodo.NuGet.ActivityPub.Model.ActorTypes;
using Fedodo.NuGet.Common.Models;

namespace Fedodo.BE.ActivityPub.Interfaces.Repositories;

public interface IUserRepository
{
    public Task<Actor?> GetActorByIdAsync(string actorId);
    public Task<ActorSecrets?> GetActorSecretsAsync(string actorId);
    public Task<IEnumerable<Actor>?> GetActorsAsync();
}
using Fedodo.BE.ActivityPub.Model.DTOs;
using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Models;

namespace Fedodo.BE.ActivityPub.Interfaces;

public interface IActivityHandler
{
    public Task<bool> SendActivitiesAsync(Activity activity, ActorSecrets actorSecrets, Actor actor);
    public Task<Actor> GetActorAsync(Guid userId, string domainName);
    public Task<Activity?> CreateActivity(Guid userId, CreateActivityDto activityDto, string domainName);
    public Task<ServerNameInboxPair?> GetServerNameInboxPairAsync(Uri actorUri, bool isPublic);
    public Task<ActorSecrets?> GetActorSecretsAsync(Guid actorId, string domainName);
}
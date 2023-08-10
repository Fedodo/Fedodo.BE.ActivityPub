using Fedodo.BE.ActivityPub.Model.DTOs;
using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Models;

namespace Fedodo.BE.ActivityPub.Interfaces.Services;

public interface ICreateActivityService
{
    public Task<bool> SendActivitiesAsync(Activity activity, ActorSecrets actorSecrets, Actor actor);
    public Task<Activity?> CreateActivity(string actorId, CreateActivityDto activityDto);
    public Task<ServerNameInboxPair?> GetServerNameInboxPairAsync(Uri actorUri, bool isPublic);
}
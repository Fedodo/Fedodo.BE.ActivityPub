using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Models;

namespace Fedodo.BE.ActivityPub.Interfaces;

public interface IActivityAPI
{
    public string ComputeHash(string jsonData);

    public Task<bool> SendActivity(Activity activity, ActorSecrets actorSecrets, ServerNameInboxPair serverInboxPair, Actor actor);
}
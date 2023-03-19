using Fedodo.NuGet.Common.Models;
using Fedodo.Server.Model.ActivityPub;
using Fedodo.Server.Model.Helpers;

namespace Fedodo.Server.Interfaces;

public interface IActivityAPI
{
    public string ComputeHash(string jsonData);

    public Task<bool> SendActivity(Activity activity, User user, ServerNameInboxPair serverInboxPair, Actor actor);
}
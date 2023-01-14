using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Authentication;
using Fedido.Server.Model.Helpers;

namespace Fedido.Server.Interfaces;

public interface IActivityAPI
{
    public string ComputeHash(string jsonData);

    public Task<bool> SendActivity(Activity activity, User user, ServerNameInboxPair serverInboxPair, Actor actor);
}
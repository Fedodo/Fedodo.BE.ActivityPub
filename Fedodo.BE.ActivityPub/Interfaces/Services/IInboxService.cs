using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Services;

public interface IInboxService
{
    public Task ActivityReceived(Activity activity, string actorId);
}
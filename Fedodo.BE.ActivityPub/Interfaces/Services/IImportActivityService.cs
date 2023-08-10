using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Interfaces.Services;

public interface IImportActivityService
{
    public Task Create(Activity activity, string activitySender);
    public Task Announce(Activity activity, string activitySender);
    public Task Follow(Activity activity, string activitySender, string actorId);
    public Task Like(Activity activity, string activitySender);
    public Task Accept(Activity activity, string activitySender, string actorId);
    public Task Update(Activity activity, string activitySender);
    public Task Delete(Activity activity, string activitySender);
    public Task Undo(Activity activity, string activitySender);
}
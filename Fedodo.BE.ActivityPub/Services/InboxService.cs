using CommonExtensions;
using Fedodo.BE.ActivityPub.Interfaces;
using Fedodo.BE.ActivityPub.Interfaces.Services;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;

namespace Fedodo.BE.ActivityPub.Services;

public class InboxService : IInboxService
{
    private readonly ILogger<InboxService> _logger;
    private readonly IActivityHandler _activityHandler;
    private readonly IImportActivityService _importActivityService;

    public InboxService(ILogger<InboxService> logger, IActivityHandler activityHandler, IImportActivityService importActivityService)
    {
        _logger = logger;
        _activityHandler = activityHandler;
        _importActivityService = importActivityService;
    }

    public async Task ActivityReceived(Activity activity)
    {
        if (activity.IsNull())
        {
            throw new ArgumentNullException(nameof(activity));
        }

        if (activity.Published.IsNull() || activity.Published <= DateTime.Parse("2000-01-01"))
            activity.Published = DateTime.Now;

        var activitySender = "";

        if (activity.Actor?.StringLinks?.FirstOrDefault().IsNotNull() ?? false)
        {
            activitySender = activity.Actor.StringLinks.FirstOrDefault()!;
            await _activityHandler.GetServerNameInboxPairAsync(new Uri(activity.Actor.StringLinks.First()), true);
        }
        else
        {
            throw new ArgumentNullException(nameof(activity.Actor));
        }

        switch (activity.Type)
        {
            case "Create":
            {
                await _importActivityService.Create(activity, activitySender);
                break;
            }
            case "Follow":
            {
                await _importActivityService.Follow(activity, activitySender);
                break;
            }
            case "Accept":
            {
                await _importActivityService.Accept(activity, activitySender);
                break;
            }
            case "Announce":
            {
                await _importActivityService.Announce(activity, activitySender);
                break;
            }
            case "Like":
            {
                await _importActivityService.Like(activity, activitySender);
                break;
            }
            case "Update":
            {
                await _importActivityService.Update(activity, activitySender);
                break;
            }
            case "Undo":
            {
                await _importActivityService.Undo(activity, activitySender);
                break;
            }
            case "Delete":
            {
                await _importActivityService.Delete(activity, activitySender);
                break;
            }
        }
    }
}
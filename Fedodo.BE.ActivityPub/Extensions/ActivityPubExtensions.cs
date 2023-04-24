using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.ObjectTypes;

namespace Fedodo.BE.ActivityPub.Extensions;

public static class ActivityPubExtensions
{
    public static bool IsPostPublic(this Note post)
    {
        foreach (var item in post.To.StringLinks)
        {
            if (item is "https://www.w3.org/ns/activitystreams#Public" or "as:Public" or "public") return true;
        }

        return false;
    }

    public static bool IsActivityPublic(this Activity activity)
    {
        return activity.To.StringLinks.Any(
            item => item is "https://www.w3.org/ns/activitystreams#Public" or "as:Public" or "public");
    }
}
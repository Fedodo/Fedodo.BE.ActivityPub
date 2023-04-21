using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.ObjectTypes;

namespace Fedodo.BE.ActivityPub.Extensions;

public static class ActivityPubExtensions
{
    public static bool IsPostPublic(this Note post)
    {
        foreach (var item in post.To.StringLinks)
        {
            if (item == new Uri("https://www.w3.org/ns/activitystreams#Public") || item == new Uri("as:Public") ||
                item == new Uri("public")) return true;
        }

        return false;
    }

    public static bool IsActivityPublic(this Activity activity)
    {
        return activity.To.StringLinks.Any(
            item => item == new Uri("https://www.w3.org/ns/activitystreams#Public") || item == new Uri("as:Public") ||
                    item == new Uri("public"));
    }
}
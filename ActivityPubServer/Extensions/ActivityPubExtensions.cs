using ActivityPubServer.Model.ActivityPub;
using ActivityPubServer.Model.DTOs;

namespace ActivityPubServer.Extensions;

public static class ActivityPubExtensions
{
    public static bool IsPostPublic(this Post post)
    {
        return post.To.Any(item => item is "https://www.w3.org/ns/activitystreams#Public" or "as:Public" or "public");
    }

    public static bool IsPostPublic(this CreatePostDto post)
    {
        return post.To.Any(item => item is "https://www.w3.org/ns/activitystreams#Public" or "as:Public" or "public");
    }

    public static bool IsActivityPublic(this Activity activity)
    {
        return activity.To.Any(
            item => item is "https://www.w3.org/ns/activitystreams#Public" or "as:Public" or "public");
    }

    public static string ExtractServerName(this string url)
    {
        var removedHttp = url.Replace("https://", "".Replace("http://", ""));
        return removedHttp.Split("/")[0];
    }
}
using Fedodo.BE.ActivityPub.Constants;

namespace Fedodo.BE.ActivityPub.Extensions;

public static class GeneralExtensions
{
    public static string ToFullActorId(this string actorId)
    {
        return $"https://{GeneralConstants.DomainName}/actor/{actorId}";
    }    
    
    public static string ToFullActorId(this Guid actorId)
    {
        return ToFullActorId(actorId.ToString());
    }
}
namespace Fedodo.BE.ActivityPub.Constants;

public static class GeneralConstants
{
    public static string DomainName { get; } = Environment.GetEnvironmentVariable("DOMAINNAME");
}
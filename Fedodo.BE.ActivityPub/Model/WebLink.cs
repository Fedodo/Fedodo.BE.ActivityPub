namespace Fedodo.BE.ActivityPub.Model;

public class WebLink
{
    public string? Rel { get; set; }
    public string? Type { get; set; }
    public Uri? Href { get; set; }
}
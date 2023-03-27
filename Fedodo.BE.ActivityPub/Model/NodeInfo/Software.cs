namespace Fedodo.BE.ActivityPub.Model.NodeInfo;

public class Software
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public Uri Repository { get; set; }
    public Uri HomePage { get; set; }
}
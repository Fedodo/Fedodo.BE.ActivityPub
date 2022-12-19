namespace ActivityPubServer.Model.ActivityPub.NodeInfo;

public class NodeInfo
{
    public string? Version { get; set; }
    public Software? Software { get; set; }
    public string[]? Protocols { get; set; }
    public Services? Services { get; set; }
    public Usage? Usage { get; set; }
    public bool OpenRegistrations { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
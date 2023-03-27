namespace Fedodo.BE.ActivityPub.Model.NodeInfo;

public class Usage
{
    public Users? Users { get; set; }
    public long LocalPosts { get; set; }
    public long LocalComments { get; set; }
}
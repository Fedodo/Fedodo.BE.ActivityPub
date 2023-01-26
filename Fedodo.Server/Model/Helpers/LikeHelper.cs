namespace Fedodo.Server.Model.Helpers;

public class LikeHelper
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Uri Like { get; set; }
}
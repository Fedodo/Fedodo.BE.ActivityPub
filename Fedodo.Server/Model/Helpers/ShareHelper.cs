namespace Fedodo.Server.Model.Helpers;

public class ShareHelper
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Uri Share { get; set; }
}
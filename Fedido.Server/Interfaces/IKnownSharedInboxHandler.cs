namespace Fedido.Server.Interfaces;

public interface IKnownSharedInboxHandler
{
    public Task AddSharedInbox(Uri sharedInbox);
    public Task<IEnumerable<Uri>> GetSharedInboxes();
}
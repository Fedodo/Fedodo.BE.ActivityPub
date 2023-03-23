namespace Fedodo.BE.ActivityPub.Interfaces;

public interface IKnownSharedInboxHandler
{
    public Task AddSharedInboxAsync(Uri sharedInbox);
    public Task<IEnumerable<Uri>> GetSharedInboxesAsync();
}
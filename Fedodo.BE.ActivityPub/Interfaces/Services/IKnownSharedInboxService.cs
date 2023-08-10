namespace Fedodo.BE.ActivityPub.Interfaces.Services;

public interface IKnownSharedInboxService
{
    public Task AddSharedInboxAsync(Uri sharedInbox);
    public Task<IEnumerable<Uri>> GetSharedInboxesAsync();
    public Task AddSharedInboxFromActorAsync(Uri actorId);
}
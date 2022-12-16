using ActivityPubServer.Model;

namespace ActivityPubServer.Interfaces;

public interface IInMemRepository
{
    public Actor GetActor(Guid actorId);
    public Webfinger? GetWebfinger(string resource);
}
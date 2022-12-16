using ActivityPubServer.Model;

namespace ActivityPubServer.Interfaces;

public interface IInMemRepository
{
    public Actor GetActor();
    public Webfinger GetWebfinger(string resource);
}
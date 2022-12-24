using ActivityPubServer.Extensions;
using ActivityPubServer.Interfaces;

namespace ActivityPubServer.Handlers;

public class KnownServersHandler : IKnownServersHandler
{
    private readonly IMongoDbRepository _repository;

    public KnownServersHandler(IMongoDbRepository repository)
    {
        _repository = repository;
    }

    public async Task Add(string postTo)
    {
        // TODO Check if the server already exists

        Model.ActivityPubServer server = new()
        {
            ServerDomainName = postTo.ExtractServerName()
        };
        server.DefaultInbox = new Uri($"https://{server.ServerDomainName}/inbox");

        await _repository.Create(server, "Information", "KnownServers");
    }

    public async Task<IEnumerable<Model.ActivityPubServer>> GetAll()
    {
        return await _repository.GetAll<Model.ActivityPubServer>("Information", "KnownServers");
    }
}
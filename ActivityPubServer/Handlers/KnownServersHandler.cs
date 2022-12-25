using ActivityPubServer.Extensions;
using ActivityPubServer.Interfaces;
using MongoDB.Driver;

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
        Model.ActivityPubServer server = new()
        {
            ServerDomainName = postTo.ExtractServerName()
        };
        server.DefaultInbox = new Uri($"https://{server.ServerDomainName}/inbox");
        
        var filterDefinitionBuilder = Builders<Model.ActivityPubServer>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i.ServerDomainName == server.ServerDomainName);
        var items = await _repository.GetSpecificItems(filter, "Information", "KnownServers");
        
        if (items.Any())
        {
            return;
        }
        else
        {
            await _repository.Create(server, "Information", "KnownServers");
        }
    }

    public async Task<IEnumerable<Model.ActivityPubServer>> GetAll()
    {
        return await _repository.GetAll<Model.ActivityPubServer>("Information", "KnownServers");
    }
}
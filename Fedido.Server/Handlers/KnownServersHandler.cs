using CommonExtensions;
using Fedido.Server.Extensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model;
using MongoDB.Driver;

namespace Fedido.Server.Handlers;

public class KnownServersHandler : IKnownServersHandler
{
    private readonly IMongoDbRepository _repository;

    public KnownServersHandler(IMongoDbRepository repository)
    {
        _repository = repository;
    }

    public async Task Add(string? postTo)
    {
        if (postTo.IsNullOrEmpty()) return;

        ActivityPubServer server = new()
        {
            ServerDomainName = postTo.ExtractServerName()
        };
        server.DefaultInbox = new Uri($"https://{server.ServerDomainName}/inbox");

        var filterDefinitionBuilder = Builders<ActivityPubServer>.Filter;
        var filter = filterDefinitionBuilder.Where(i => i.ServerDomainName == server.ServerDomainName);
        var items = await _repository.GetSpecificItems(filter, "Information", "KnownServers");

        if (!items.Any()) await _repository.Create(server, "Information", "KnownServers");
    }

    public async Task<IEnumerable<ActivityPubServer>> GetAll()
    {
        return await _repository.GetAll<ActivityPubServer>("Information", "KnownServers");
    }
}
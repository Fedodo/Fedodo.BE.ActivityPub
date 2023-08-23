using Fedodo.BE.ActivityPub.Interfaces.Repositories;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Fedodo.NuGet.Common.Models;
using MongoDB.Driver;

namespace Fedodo.BE.ActivityPub.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ILogger<UserRepository> _logger;
    private readonly IMongoDbRepository _mongoDbRepository;

    public UserRepository(ILogger<UserRepository> logger, IMongoDbRepository mongoDbRepository)
    {
        _logger = logger;
        _mongoDbRepository = mongoDbRepository;
    }

    public async Task<Actor?> GetActorByIdAsync(string actorId)
    {
        var filterActorDefinitionBuilder = Builders<Actor>.Filter;
        var filterActor = filterActorDefinitionBuilder.Eq(i => i.Id, new Uri(actorId));
        var actor = await _mongoDbRepository.GetSpecificItem(filterActor, DatabaseLocations.Actors.Database,
            DatabaseLocations.Actors.Collection);

        return actor;
    }

    public async Task<IEnumerable<Actor>?> GetActorsAsync()
    {
        var actors =
            await _mongoDbRepository.GetAll<Actor>(DatabaseLocations.Actors.Database,
                DatabaseLocations.Actors.Collection);

        return actors;
    }

    public async Task<ActorSecrets?> GetActorSecretsAsync(string actorId)
    {
        var filterActorDefinitionBuilder = Builders<ActorSecrets>.Filter;
        var filterActor = filterActorDefinitionBuilder.Eq(i => i.ActorId, new Uri(actorId));
        var actorSecrets = await _mongoDbRepository.GetSpecificItem(filterActor,
            DatabaseLocations.ActorSecrets.Database, DatabaseLocations.ActorSecrets.Collection);
        return actorSecrets;
    }
}
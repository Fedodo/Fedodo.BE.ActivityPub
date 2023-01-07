using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.Authentication;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace ActivityPubServer.Handlers;

public class UserHandler : IUserHandler
{
    private readonly ILogger<UserHandler> _logger;
    private readonly IMongoDbRepository _repository;

    public UserHandler(ILogger<UserHandler> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    public async Task<User> GetUser(Guid userId)
    {
        var filterUserDefinitionBuilder = Builders<User>.Filter;
        var filterUser = filterUserDefinitionBuilder.Eq(i => i.Id, userId);
        var user = await _repository.GetSpecificItem(filterUser, "Authentication", "Users");
        return user;
    }
}
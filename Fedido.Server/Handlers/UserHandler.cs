using CommonExtensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.Authentication;
using MongoDB.Driver;

namespace Fedido.Server.Handlers;

public class UserHandler : IUserHandler
{
    private readonly ILogger<UserHandler> _logger;
    private readonly IMongoDbRepository _repository;

    public UserHandler(ILogger<UserHandler> logger, IMongoDbRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<User> GetUserById(Guid userId)
    {
        var filterUserDefinitionBuilder = Builders<User>.Filter;
        var filterUser = filterUserDefinitionBuilder.Eq(i => i.Id, userId);
        var user = await _repository.GetSpecificItem(filterUser, "Authentication", "Users");
        return user;
    }

    public async Task<User> GetUserByName(string userName)
    {
        var filterUserDefinitionBuilder = Builders<User>.Filter;
        var filterUser = filterUserDefinitionBuilder.Eq(i => i.UserName, userName);
        var user = await _repository.GetSpecificItem(filterUser, "Authentication", "Users");
        return user;
    }

    public bool VerifyUser(Guid userId, HttpContext context)
    {
        var activeUserClaims = context.User.Claims.ToList();
        var tokenUserId = activeUserClaims.Where(i => i.ValueType.IsNotNull() && i.Type == "sub")?.FirstOrDefault();

        if (tokenUserId.IsNull())
        {
            _logger.LogWarning($"No {nameof(tokenUserId)} found for {nameof(userId)}:\"{userId}\"");
            return false;
        }

        if (tokenUserId.Value == userId.ToString()) return true;

        _logger.LogWarning($"Someone tried to post as {userId} but was authorized as {tokenUserId}");
        return false;
    }
}
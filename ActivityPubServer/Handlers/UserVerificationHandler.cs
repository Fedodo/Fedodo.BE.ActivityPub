using System.Security.Claims;
using ActivityPubServer.Interfaces;
using CommonExtensions;

namespace ActivityPubServer.Handlers;

public class UserVerificationHandler : IUserVerificationHandler
{
    private readonly ILogger<UserVerificationHandler> _logger;

    public UserVerificationHandler(ILogger<UserVerificationHandler> logger)
    {
        _logger = logger;
    }
    
    public bool VerifyUser(Guid userId, HttpContext context)
    {
        var activeUserClaims = context.User.Claims.ToList();
        var tokenUserId = activeUserClaims.Where(i => i.ValueType.IsNotNull() && i.Type == ClaimTypes.Sid)?.First()
            .Value;

        if (tokenUserId == userId.ToString()) return true;

        _logger.LogWarning($"Someone tried to post as {userId} but was authorized as {tokenUserId}");
        return false;
    }
}
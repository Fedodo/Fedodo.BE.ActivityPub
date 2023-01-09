using ActivityPubServer.Model.Authentication;

namespace ActivityPubServer.Interfaces;

public interface IUserHandler
{
    public Task<User> GetUser(Guid userId);
    public bool VerifyUser(Guid userId, HttpContext context);
    public Task<User> GetUser(string userName);
}
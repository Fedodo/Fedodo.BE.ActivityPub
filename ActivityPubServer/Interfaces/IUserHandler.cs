using ActivityPubServer.Model.Authentication;

namespace ActivityPubServer.Interfaces;

public interface IUserHandler
{
    public Task<User> GetUserById(Guid userId);
    public bool VerifyUser(Guid userId, HttpContext context);
    public Task<User> GetUserByName(string userName);
}
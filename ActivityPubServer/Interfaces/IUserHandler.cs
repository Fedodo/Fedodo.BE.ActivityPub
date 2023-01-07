using ActivityPubServer.Model.Authentication;

namespace ActivityPubServer.Interfaces;

public interface IUserHandler
{
    public Task<User> GetUser(Guid userId);
}
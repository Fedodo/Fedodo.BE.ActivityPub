using Fedido.Server.Model.Authentication;

namespace Fedido.Server.Interfaces;

public interface IUserHandler
{
    public Task<User> GetUserByIdAsync(Guid userId);
    public bool VerifyUser(Guid userId, HttpContext context);
    public Task<User> GetUserByNameAsync(string userName);
}
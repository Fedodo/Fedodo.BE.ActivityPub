using Fedodo.Server.Model.Authentication;

namespace Fedodo.Server.Interfaces;

public interface IUserHandler
{
    public Task<User> GetUserByIdAsync(Guid userId);
    public bool VerifyUser(Guid userId, HttpContext context);
    public Task<User> GetUserByNameAsync(string userName);
}
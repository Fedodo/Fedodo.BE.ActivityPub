namespace Fedodo.Server.Interfaces;

public interface IAuthenticationHandler
{
    public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
    public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);
}
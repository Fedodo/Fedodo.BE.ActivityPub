using ActivityPubServer.Model.Authentication;

namespace ActivityPubServer.Interfaces;

public interface IAuthenticationHandler
{
    public string CreateToken(User user);
    public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
    public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);
}
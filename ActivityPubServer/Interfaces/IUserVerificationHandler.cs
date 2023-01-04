namespace ActivityPubServer.Interfaces;

public interface IUserVerificationHandler
{
    public bool VerifyUser(Guid userId, HttpContext context);
}
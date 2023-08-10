namespace Fedodo.BE.ActivityPub.Interfaces.Services;

public interface IHttpSignatureService
{
    public Task<bool> VerifySignature(IHeaderDictionary requestHeaders, string currentPath);
}
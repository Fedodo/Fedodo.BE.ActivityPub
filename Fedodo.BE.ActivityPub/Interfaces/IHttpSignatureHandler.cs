namespace Fedodo.BE.ActivityPub.Interfaces;

public interface IHttpSignatureHandler
{
    public Task<bool> VerifySignature(IHeaderDictionary requestHeaders, string currentPath);
}
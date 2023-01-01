namespace ActivityPubServer.Interfaces;

public interface IHttpSignatureHandler
{
    public Task<bool> VerifySignature(IHeaderDictionary requestHeaders, string currentPath);
}
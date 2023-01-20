using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Fedido.Server.Handlers;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fedido.Server.Test.Handlers;

public class HttpSignatureHandlerShould
{
    private readonly HttpSignatureHandler _signatureHandler;
    private RSA Rsa = RSA.Create();
    
    public HttpSignatureHandlerShould()
    {
        var logger = new Mock<ILogger<HttpSignatureHandler>>();
        var actorApi = new Mock<IActorAPI>();
        var actor = new Actor()
        {
            PublicKey = new PublicKeyAP()
            {
                Id = new Uri("https://example.com/id"),
                Owner = new Uri("https://example.com/key"),
                //PublicKeyPem = Rsa.Ex() // TODO Export this with standalone class
            }
        };

        actorApi.Setup(i => i.GetActor(new Uri("https://example.com/key")))
            .ReturnsAsync(actor);
        
        _signatureHandler = new HttpSignatureHandler(logger.Object, actorApi.Object);
    }
    
    [Theory]
    [InlineData("", "", "", "", "", false)]
    [InlineData("RandomString", "", "", "", "", false)]
    [InlineData("RandomString%/&()", "", "", "", "", false)]
    [InlineData("/inbox/id", $"keyId=\"https://example.com/key\",headers=\"(request-target) host date digest\"," +
                             $"signature=\"test\"", "string host", "digest", "date", false)]    
    [InlineData("/inbox/id", $"keyId=\"https://example.com/key\",headers=\"(request-target) host date digest\"," +
                             $"signature=\"test\"", "string host", "digest", "date", true)]
    public async Task VerifySignature(string currentPath, string signature, string host, string digest, string date, bool isSuccessful)
    {
        // Arrange
        // var signedString =
        //     $"(request-target): post {serverInboxPair.Inbox.AbsolutePath}\nhost: {serverInboxPair.ServerName}\ndate: {date}\ndigest: sha-256={digest}";
        IHeaderDictionary headers = new HeaderDictionary();
        headers.Add("Host", host);
        headers.Add("Date", date);
        headers.Add("Digest", $"sha-256={digest}");
        headers.Add("Signature", signature);
        
        // Act
        var result = await _signatureHandler.VerifySignature(headers, currentPath);

        // Assert
        result.ShouldBe(isSuccessful);
    }
}
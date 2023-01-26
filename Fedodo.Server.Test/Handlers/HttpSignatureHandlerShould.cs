using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions.Cryptography;
using Fedodo.Server.Handlers;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fedodo.Server.Test.Handlers;

public class HttpSignatureHandlerShould
{
    private readonly RSA _rsa = RSA.Create();
    private readonly HttpSignatureHandler _signatureHandler;

    public HttpSignatureHandlerShould()
    {
        var logger = new Mock<ILogger<HttpSignatureHandler>>();
        var actorApi = new Mock<IActorAPI>();
        var actor = new Actor
        {
            PublicKey = new PublicKeyAP
            {
                Id = new Uri("https://example.com/id"),
                Owner = new Uri("https://example.com/key"),
                PublicKeyPem = _rsa.ExtractRsaPublicKeyPem()
            }
        };

        actorApi.Setup(i => i.GetActor(new Uri("https://example.com/key")))
            .ReturnsAsync(actor);

        _signatureHandler = new HttpSignatureHandler(logger.Object, actorApi.Object);
    }

    [Theory]
    [InlineData("", "", "", "", "", false, false)]
    [InlineData("RandomString", "", "", "", "", false, true)]
    [InlineData("RandomString%/&()", "", "", "", "", false, true)]
    [InlineData("/inbox/id", "keyId=\"https://example.com/key9\",headers=\"(request-target) host date digest\"," +
                             "signature=\"ToBeReplaced\"", "string host", "digest", "date", false, true)]
    [InlineData("/inbox/id", "keyId=\"https://example.com/key\",headers=\"(request-target) host date digest break\"," +
                             "signature=\"ToBeReplaced\"", "string host", "digest", "date", false, true)]
    [InlineData("/inbox/id", "keyId=\"https://example.com/key\",headers=\"(request-target) host date digest\"," +
                             "signature=\"YXNkZg==\"", "host", "digest", "date", false, true)]
    [InlineData("/inbox/id", "keyId=\"https://example.com/key\",headers=\"(request-target) host date digest\"," +
                             "signature=\"ToBeReplaced\"", "host", "digest", "date", true, true)]
    [InlineData("/inbox/id",
        "keyId=\"https://example.com/key\",headers=\"(request-target) host date digest content-type\"," +
        "signature=\"ToBeReplaced\"", "host", "digest", "date", true, true)]
    public async Task VerifySignature(string currentPath, string signatureHeader, string host, string digest,
        string date,
        bool isSuccessful, bool signatureEnabled)
    {
        // Arrange
        string signedString;
        IHeaderDictionary headers = new HeaderDictionary();
        headers.Add("Host", host);
        headers.Add("Date", date);
        headers.Add("Digest", $"sha-256={digest}");
        if (signatureHeader.Contains("content-type"))
        {
            headers.ContentType = "json";
            signedString =
                $"(request-target): post {currentPath}\nhost: {host}\ndate: {date}\ndigest: sha-256={digest}\ncontent-type: json";
        }
        else
        {
            signedString =
                $"(request-target): post {currentPath}\nhost: {host}\ndate: {date}\ndigest: sha-256={digest}";
        }

        if (signatureEnabled)
        {
            var signature = _rsa.SignData(Encoding.UTF8.GetBytes(signedString), HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            headers.Add("Signature", signatureHeader.Replace("signature=\"ToBeReplaced\"",
                $"signature=\"{Convert.ToBase64String(signature)}\""));
        }

        // Act
        var result = await _signatureHandler.VerifySignature(headers, currentPath);

        // Assert
        result.ShouldBe(isSuccessful);
    }
}
using ActivityPubServer.Extensions;
using ActivityPubServer.Model.ActivityPub;
using Shouldly;
using Xunit;

namespace ActivityPubServer.Test.Extensions;

public class ActivityPubExtensionsShould
{
    [Fact]
    public void IsPostPublic()
    {
        // Arrange
        Post post = new()
        {
            
        };

        // Act

        // Assert
    }

    [Theory]
    [InlineData("https://lna-dev.net")]
    [InlineData("http://lna-dev.net")]
    [InlineData("https://lna-dev.net/posts/cool-post/")]
    public void ExtractServerName(string url)
    {
        // Arrange

        // Act 
        var extractedUrl = url.ExtractServerName();

        // Assert
        extractedUrl.ShouldNotContain("http");
        extractedUrl.ShouldNotContain("/");
    }
}
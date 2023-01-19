using System;
using System.Linq;
using System.Security.Claims;
using Fedido.Server.Handlers;
using Fedido.Server.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OpenIddict.Abstractions;
using Shouldly;
using Xunit;

namespace Fedido.Server.Test.Handlers;

public class UserHandlerShould
{
    private readonly UserHandler _userHandler;

    public UserHandlerShould()
    {
        var logger = new Mock<ILogger<UserHandler>>();
        var repository = new Mock<IMongoDbRepository>();

        _userHandler = new UserHandler(logger.Object, repository.Object);
    }

    [Theory]
    [InlineData("BA913A66-2B6A-4236-9854-9477906DA12D", true, true)]
    [InlineData("Ba913A66-2B6A-4236-9854-9477906DA12D", true, true)]
    [InlineData("ba913a66-2b6a-4236-9854-9477906da12d", true, true)]
    [InlineData("ba933a66-2b6a-4236-9854-9477906da12d", false, true)]
    [InlineData("ba933a66-2b6a-4236-9854-9477906da12d", false, false)]
    public void VerifyUser(string id, bool successful, bool setClaim)
    {
        // Arrange
        var httpContext = new DefaultHttpContext().HttpContext;
        if (setClaim)
        {
            httpContext.User.SetClaim(OpenIddictConstants.Claims.Subject, "BA913A66-2B6A-4236-9854-9477906DA12D");
        }

        // Act
        var result = _userHandler.VerifyUser(new Guid(id), httpContext);

        // Assert
        result.ShouldBe(successful);
    }
}
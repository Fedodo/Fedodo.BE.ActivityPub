using System;
using System.Threading.Tasks;
using CommonExtensions;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Handlers;
using Fedodo.NuGet.Common.Interfaces;
using Fedodo.NuGet.Common.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Shouldly;
using Xunit;

namespace Fedodo.BE.ActivityPub.Test.Handlers;

public class UserHandlerShould
{
    private readonly UserHandler _userHandler;
    private readonly User user;

    public UserHandlerShould()
    {
        var logger = new Mock<ILogger<UserHandler>>();
        var repository = new Mock<IMongoDbRepository>();

        user = new User
        {
            Id = new Guid("7D524FBC-9B45-417B-83A3-A67A1F5F595D"),
            UserName = "userName",
            Role = "TestUser"
        };

        var filterIdDefinitionBuilder = Builders<User>.Filter;
        var filterId = filterIdDefinitionBuilder.Eq(i => i.Id,
            new Guid("7D524FBC-9B45-417B-83A3-A67A1F5F595D"));
        repository.Setup(i => i.GetSpecificItem(It.Is<FilterDefinition<User>>(item => item.IsSameAs(filterId)),
            DatabaseLocations.Users.Database,
            DatabaseLocations.Users.Collection)).ReturnsAsync(user);

        var filterUserDefinitionBuilder = Builders<User>.Filter;
        var filterUser = filterUserDefinitionBuilder.Eq(i => i.UserName, "userName");
        repository.Setup(i => i.GetSpecificItem(It.Is<FilterDefinition<User>>(item => item.IsSameAs(filterUser)),
            DatabaseLocations.Users.Database,
            DatabaseLocations.Users.Collection)).ReturnsAsync(user);

        _userHandler = new UserHandler(logger.Object, repository.Object);
    }

    // [Theory]
    // [InlineData("BA913A66-2B6A-4236-9854-9477906DA12D", true, true)]
    // [InlineData("Ba913A66-2B6A-4236-9854-9477906DA12D", true, true)]
    // [InlineData("ba913a66-2b6a-4236-9854-9477906da12d", true, true)]
    // [InlineData("ba933a66-2b6a-4236-9854-9477906da12d", false, true)]
    // [InlineData("ba933a66-2b6a-4236-9854-9477906da12d", false, false)]
    // public void VerifyUser(string id, bool successful, bool setClaim)
    // {
    //     // Arrange
    //     var httpContext = new DefaultHttpContext().HttpContext;
    //     if (setClaim)
    //         httpContext.User.SetClaim(OpenIddictConstants.Claims.Subject, "BA913A66-2B6A-4236-9854-9477906DA12D");
    //
    //     // Act
    //     var result = _userHandler.VerifyUser(new Guid(id), httpContext);
    //
    //     // Assert
    //     result.ShouldBe(successful);
    // }

    [Theory]
    [InlineData("7D524FBC-9B45-417B-83A3-A67A1F5F595D")]
    public async Task GetUserByIdAsync(Guid userId)
    {
        // Arrange

        // Act
        var result = await _userHandler.GetUserByIdAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(user);
    }

    [Theory]
    [InlineData("userName")]
    public async Task GetUserByNameAsync(string userName)
    {
        // Arrange

        // Act
        var result = await _userHandler.GetUserByNameAsync(userName);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(user);
    }
}
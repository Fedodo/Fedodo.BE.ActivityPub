using System;
using System.Threading.Tasks;
using Fedodo.BE.ActivityPub.Controllers;
using Fedodo.BE.ActivityPub.Model.DTOs;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Handlers;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fedodo.BE.ActivityPub.Test.Controllers.ActivityPub;

public class UserControllerShould
{
    private readonly UserController _userController;
    
    public UserControllerShould()
    {
        var logger = new Mock<ILogger<UserController>>();
        var repository = new Mock<IMongoDbRepository>();
        var authHandler = new AuthenticationHandler();

        repository.Setup(i =>
                i.GetAll<Activity>(DatabaseLocations.InboxLike.Database, DatabaseLocations.InboxLike.Collection))
            .Throws<Exception>();
        
        _userController = new UserController(logger.Object, repository.Object, authHandler);
    }
    
    [Fact]
    public async Task CreateUser()
    {
        // Arrange
        var actorDto = new CreateActorDto()
        {
            
        };
        
        // Act
        var result = await _userController.CreateUserAsync(actorDto);

        // Assert
    }
}
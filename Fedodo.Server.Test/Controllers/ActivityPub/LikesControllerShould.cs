using System;
using System.Threading.Tasks;
using CommonExtensions;
using Fedodo.Server.Controllers;
using Fedodo.Server.Controllers.ActivityPub;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model;
using Fedodo.Server.Model.Helpers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Shouldly;
using Xunit;

namespace Fedodo.Server.Test.Controllers.ActivityPub;

public class LikesControllerShould
{
    private readonly LikesController _likesController;

    public LikesControllerShould()
    {
        var logger = new Mock<ILogger<LikesController>>();
        var repository = new Mock<IMongoDbRepository>();
        
        repository.Setup(i =>
                i.GetAll<LikeHelper>(DatabaseLocations.Likes.Database, "")).Throws<Exception>();        
        _likesController = new LikesController(logger.Object, repository.Object);
    }
    
    [Fact]
    public async Task GetLikes()
    {
        // Arrange
        
        // Act
        var likes = await _likesController.GetLikes(
            "https:%2F%2Fsocial.heise.de%2Fusers%2Fheiseonline%2Fstatuses%2F109835554713856912");

        // Assert
        likes.ShouldNotBeNull();
    }
}
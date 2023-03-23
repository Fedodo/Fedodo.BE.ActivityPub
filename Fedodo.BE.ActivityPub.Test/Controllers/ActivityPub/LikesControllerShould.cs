using System;
using Fedodo.BE.ActivityPub.Controllers.ActivityPub;
using Fedodo.BE.ActivityPub.Model.ActivityPub;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Fedodo.BE.ActivityPub.Test.Controllers.ActivityPub;

public class LikesControllerShould
{
    private readonly LikesController _likesController;

    public LikesControllerShould()
    {
        var logger = new Mock<ILogger<LikesController>>();
        var repository = new Mock<IMongoDbRepository>();

        repository.Setup(i =>
                i.GetAll<Activity>(DatabaseLocations.InboxLike.Database, DatabaseLocations.InboxLike.Collection))
            .Throws<Exception>();
        _likesController = new LikesController(logger.Object, repository.Object);
    }

    // [Fact]
    // public async Task GetLikes()
    // {
    //     // Arrange
    //
    //     // Act
    //     var likes = await _likesController.GetLikesPage(
    //         "https:%2F%2Fsocial.heise.de%2Fusers%2Fheiseonline%2Fstatuses%2F109835554713856912");
    //
    //     // Assert
    //     likes.ShouldNotBeNull();
    // }
}
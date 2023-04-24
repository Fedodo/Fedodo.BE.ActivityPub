using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonExtensions;
using Fedodo.BE.ActivityPub.Handlers;
using Fedodo.BE.ActivityPub.Interfaces;
using Fedodo.BE.ActivityPub.Model.DTOs;
using Fedodo.BE.ActivityPub.Model.Helpers;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes;
using Fedodo.NuGet.ActivityPub.Model.ActorTypes.SubTypes;
using Fedodo.NuGet.ActivityPub.Model.CoreTypes;
using Fedodo.NuGet.ActivityPub.Model.JsonConverters.Model;
using Fedodo.NuGet.ActivityPub.Model.ObjectTypes;
using Fedodo.NuGet.Common.Constants;
using Fedodo.NuGet.Common.Interfaces;
using Fedodo.NuGet.Common.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Shouldly;
using Xunit;
using Object = Fedodo.NuGet.ActivityPub.Model.CoreTypes.Object;

namespace Fedodo.BE.ActivityPub.Test.Handlers;

public class ActivityHandlerShould
{
    private readonly Actor _actor;
    private readonly ActivityHandler _handler;

    public ActivityHandlerShould()
    {
        var logger = new Mock<ILogger<ActivityHandler>>();
        var repository = new Mock<IMongoDbRepository>();
        var actorApi = new Mock<IActorAPI>();
        var activityApi = new Mock<IActivityAPI>();
        var sharedInboxHandler = new Mock<IKnownSharedInboxHandler>();
        var collectionApi = new Mock<ICollectionApi>();

        _actor = new Actor
        {
            Name = "Lexa kom Trikru",
            Id = new Uri("https://example.com/actor/00E90526-288D-41E2-9B21-39BAC05ED5B6"),
            Inbox = new Uri("https://example.com/inbox"),
            Endpoints = new Endpoints
            {
                SharedInbox = new Uri("https://example.com/sharedInbox")
            }
        };

        var sharedInboxes = new List<Uri>
        {
            new("https://example.com/sharedInbox")
        };

        var filterIdDefinitionBuilder = Builders<Actor>.Filter;
        var filterId = filterIdDefinitionBuilder.Eq(i => i.Id,
            new Uri("https://example.com/actor/00E90526-288D-41E2-9B21-39BAC05ED5B6"));
        repository.Setup(i => i.GetSpecificItem(It.Is<FilterDefinition<Actor>>(
                item => item.IsSameAs(filterId)), DatabaseLocations.Actors.Database,
            DatabaseLocations.Actors.Collection)).ReturnsAsync(_actor);

        sharedInboxHandler.Setup(i => i.GetSharedInboxesAsync()).ReturnsAsync(sharedInboxes);

        activityApi.Setup(i => i.SendActivity(It.IsAny<Activity>(), It.Is<User>(i => i.UserName == "Fail"),
            It.IsAny<ServerNameInboxPair>(), It.IsAny<Actor>())).ReturnsAsync(false);

        activityApi.Setup(i => i.SendActivity(It.IsAny<Activity>(), It.Is<User>(i => i.UserName != "Fail"),
            It.IsAny<ServerNameInboxPair>(), It.IsAny<Actor>())).ReturnsAsync(true);

        collectionApi.Setup(i => i.GetOrderedCollection<Uri>(It.Is<Uri>(i => i != new Uri("https://example.com/null"))))
            .ReturnsAsync(
                new OrderedCollection
                {
                    Items = new TripleSet<Object>
                    {
                        StringLinks = new[]
                        {
                            "https://example.com/asdf"
                        }
                    }
                });

        collectionApi.Setup(i => i.GetCollection<Uri>(It.IsAny<Uri>())).ReturnsAsync(
            new Collection
            {
                Items = new TripleSet<Object>
                {
                    StringLinks = new[]
                    {
                        "https://example.com/uri"
                    }
                }
            });

        actorApi.Setup(i => i.GetActor(It.Is<Uri>(i => i != new Uri("https://example.com/fail")))).ReturnsAsync(_actor);

        _handler = new ActivityHandler(logger.Object, repository.Object, actorApi.Object,
            activityApi.Object, sharedInboxHandler.Object, collectionApi.Object);
    }

    [Theory]
    [InlineData("EAB26E2C-48BE-45F6-BB17-FB35BB7F889F", "Create")]
    [InlineData("EAB26E2C-48BE-45F6-BB17-FB35BB7F889F", "Like")]
    [InlineData("EAB26E2C-48BE-45F6-BB17-FB35BB7F889F", "Announce")]
    public async Task CreateActivity(string userId, string type)
    {
        // Arrange
        object obj;

        if (type == "Create")
            obj = new Note();
        else
            obj = "https://example.com/resource";

        var dto = new CreateActivityDto
        {
            Object = obj,
            Type = type
        };

        // Act
        var result = await _handler.CreateActivity(new Guid(userId), dto, "example.com");

        // Assert
        result.ShouldNotBeNull();
        result.Actor!.StringLinks!.First().ShouldBe("https://example.com/actor/eab26e2c-48be-45f6-bb17-fb35bb7f889f");
        result.Type.ShouldBe(type);
        if (type != "Create")
            result.Object.ShouldBe(obj);
        else
            result.Object!.Objects!.First().ShouldBeOfType<Note>();
    }

    [Theory]
    [InlineData("00E90526-288D-41E2-9B21-39BAC05ED5B6", "lna-dev.net")]
    public async Task GetActor(string userId, string domainName)
    {
        // Arrange

        // Act
        var result = await _handler.GetActorAsync(new Guid(userId), domainName);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(_actor);
    }

    [Theory]
    [InlineData("Fail", "as:Public")]
    [InlineData("NotFail", "as:Public")]
    [InlineData("NotFail", "https://example.com/blub")]
    [InlineData("Fail", "https://example.com/fail")]
    public async Task SendActivities(string userName, string to)
    {
        // Arrange
        var activity = new Activity
        {
            To = new TripleSet<Object>
            {
                StringLinks = new[]
                {
                    to,
                    "https://example.com/user/123",
                    "https://example.com/fail"
                }
            },
            Bto = new TripleSet<Object>
            {
                StringLinks = new[]
                {
                    to,
                    "https://example.com/user/123"
                }
            },
            Audience = new TripleSet<Object>
            {
                StringLinks = new[]
                {
                    to,
                    "https://example.com/user/123"
                }
            },
            Cc = new TripleSet<Object>
            {
                StringLinks = new[]
                {
                    to,
                    "https://example.com/user/123"
                }
            },
            Bcc = new TripleSet<Object>
            {
                StringLinks = new[]
                {
                    to,
                    "https://example.com/user/123"
                }
            }
        };
        var user = new User
        {
            UserName = userName
        };

        // Act
        var result = await _handler.SendActivitiesAsync(activity, user, _actor);

        // Assert
        result.ShouldNotBe(userName == "Fail");
    }

    [Theory]
    [InlineData("https://example.com/target", true, "https://example.com/sharedInbox")]
    [InlineData("https://example.com/target", false, "https://example.com/inbox")]
    [InlineData("https://example.com/null", false, "https://example.com/inbox")]
    [InlineData("https://example.com/null", true, "https://example.com/sharedInbox")]
    public async Task GetServerNameInboxPairs(string targetString, bool isPublic, string expectedResult)
    {
        // Arrange
        var target = new Uri(targetString);

        // Act
        var result = await _handler.GetServerNameInboxPairsAsync(target, isPublic);

        // Assert
        result.ShouldNotBeNull();
        result.First().Inbox.ShouldBe(new Uri(expectedResult));
        result.First().ServerName.ShouldBe(new Uri(expectedResult).Host);
    }
}
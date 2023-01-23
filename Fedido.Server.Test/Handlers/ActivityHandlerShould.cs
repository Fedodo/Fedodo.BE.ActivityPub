using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonExtensions;
using Fedido.Server.APIs;
using Fedido.Server.Handlers;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.ActivityPub;
using Fedido.Server.Model.Authentication;
using Fedido.Server.Model.Helpers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Moq.Language.Flow;
using Shouldly;
using Xunit;

namespace Fedido.Server.Test.Handlers;

public class ActivityHandlerShould
{
    private readonly ActivityHandler _handler;
    private readonly Actor _actor;

    public ActivityHandlerShould()
    {
        var logger = new Mock<ILogger<ActivityHandler>>();
        var repository = new Mock<IMongoDbRepository>();
        var actorApi = new Mock<IActorAPI>();
        var activityApi = new Mock<IActivityAPI>();
        var sharedInboxHandler = new Mock<IKnownSharedInboxHandler>();
        var collectionApi = new Mock<ICollectionApi>();

        _actor = new Actor()
        {
            Name = "Lexa kom Trikru",
            Id = new Uri("https://example.com/actor/00E90526-288D-41E2-9B21-39BAC05ED5B6")
        };

        var sharedInboxes = new List<Uri>()
        {
            new Uri("https://example.com/sharedInbox")
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
        
        collectionApi.Setup(i => i.GetOrderedCollection<Uri>(It.Is<Uri>(i => i != new Uri("https://example.com/null")))).ReturnsAsync(
            new OrderedCollection<Uri>()
        {
            OrderedItems = new []
            {
                new Uri("https://example.com/asdf")
            }
        });

        collectionApi.Setup(i => i.GetCollection<Uri>(It.IsAny<Uri>())).ReturnsAsync(
            new Collection<Uri>()
        {
            Items = new []
            {
                new Uri("https://example.com/uri")
            }
        });

        actorApi.Setup(i => i.GetActor(It.IsAny<Uri>())).ReturnsAsync(_actor);
        
        _handler = new ActivityHandler(logger: logger.Object, repository: repository.Object, actorApi: actorApi.Object, 
            activityApi: activityApi.Object, sharedInboxHandler: sharedInboxHandler.Object, collectionApi: collectionApi.Object);
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
    public async Task SendActivities(string userName, string to)
    {
        // Arrange
        var activity = new Activity()
        {
            To = new []
            {
                to,
                "https://example.com/user/123"
            },
            Bto = new []
            {
                to,
                "https://example.com/user/123"            
            },
            Audience = new []
            {
                to,
                "https://example.com/user/123"            
            },
            Cc = new []
            {
                to,
                "https://example.com/user/123"
            },
            Bcc = new []
            {
                to,
                "https://example.com/user/123"
            }
        };
        var user = new User()
        {
            UserName = userName
        };
        
        // Act
        await _handler.SendActivitiesAsync(activity, user, _actor);

        // Assert
    }

    [Theory]
    [InlineData("https://example.com/target", true)]
    [InlineData("https://example.com/target", false)]
    [InlineData("https://example.com/null", false)]
    [InlineData("https://example.com/null", true)]
    public async Task GetServerNameInboxPairs(string targetString, bool isPublic)
    {
        // Arrange
        var target = new Uri(targetString);

        // Act
        var result = await _handler.GetServerNameInboxPairsAsync(target, isPublic);

        // Assert
        result.ShouldNotBeNull();
    }
}
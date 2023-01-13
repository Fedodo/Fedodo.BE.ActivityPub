using Fedido.Server.Model.ActivityPub;
using Shouldly;
using Xunit;

namespace Fedido.Server.Test.Model.ActivityPub;

public class ActivityShould
{
    [Fact]
    public void ExtractItemFromObject()
    {
        // Arrange
        var activity = new Activity
        {
            Object = new
            {
                test = "test"
            }
        };

        // Act
        var extractItem = activity.ExtractItemFromObject<Post>();

        // Assert
        extractItem.ShouldNotBeNull();
    }
}
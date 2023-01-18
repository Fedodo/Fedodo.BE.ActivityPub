using Fedido.Server.Model.Helpers;

namespace Fedido.Server;

public static class DatabaseLocations
{
    #region UserIdCollections

    public static DatabaseCollectionPair Followings { get; } = new()
    {
        Database = "Followings",
        Collection = null
    };

    public static DatabaseCollectionPair Followers { get; } = new()
    {
        Database = "Followers",
        Collection = null
    };

    public static DatabaseCollectionPair Shares { get; } = new()
    {
        Database = "Shares",
        Collection = null
    };

    public static DatabaseCollectionPair Likes { get; } = new()
    {
        Database = "Likes",
        Collection = null
    };

    public static DatabaseCollectionPair Activities { get; } = new()
    {
        Database = "Activities",
        Collection = null
    };

    #endregion

    #region Hardcoded

    public static DatabaseCollectionPair KnownSharedInbox { get; } = new()
    {
        Database = "Others",
        Collection = "SharedInboxes"
    };

    public static DatabaseCollectionPair Actors { get; } = new()
    {
        Database = "Account",
        Collection = "Actors"
    };

    public static DatabaseCollectionPair Webfinger { get; } = new()
    {
        Database = "Account",
        Collection = "Webfingers"
    };

    public static DatabaseCollectionPair Users { get; } = new()
    {
        Database = "Account",
        Collection = "Users"
    };

    public static DatabaseCollectionPair OutboxNotes { get; } = new()
    {
        Database = "Outbox",
        Collection = "Notes"
    };

    public static DatabaseCollectionPair InboxNotes { get; } = new()
    {
        Database = "Inbox",
        Collection = "Notes"
    };

    #endregion
}
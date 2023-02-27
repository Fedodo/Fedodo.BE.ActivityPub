using Fedodo.Server.Model.Helpers;

namespace Fedodo.Server;

public static class DatabaseLocations
{
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

    public static DatabaseCollectionPair OutboxFollow { get; } = new()
    {
        Database = "Outbox",
        Collection = "Follow"
    };

    public static DatabaseCollectionPair InboxCreate { get; } = new()
    {
        Database = "Inbox",
        Collection = "Create"
    };

    public static DatabaseCollectionPair InboxAnnounce { get; } = new()
    {
        Database = "Inbox",
        Collection = "Announce"
    };
    
    public static DatabaseCollectionPair InboxFollow { get; } = new()
    {
        Database = "Inbox",
        Collection = "Follow"
    };
    
    public static DatabaseCollectionPair InboxLike { get; } = new()
    {
        Database = "Inbox",
        Collection = "Like"
    };
}
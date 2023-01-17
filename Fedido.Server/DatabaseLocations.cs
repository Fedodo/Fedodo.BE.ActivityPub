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
    #endregion

    #region Hardcoded
        public static DatabaseCollectionPair KnownSharedInbox { get; } = new()
        {
            Database = "GeneratedData",
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
    #endregion
    
    #region Dynamic
        public static DatabaseCollectionPair Inbox { get; } = new()
        {
            Database = "Inbox",
            Collection = null
        };
        
        public static DatabaseCollectionPair Posts { get; } = new()
        {
            Database = "Outbox",
            Collection = null
        };
        
        public static DatabaseCollectionPair Activities { get; } = new()
        {
            Database = "Activities",
            Collection = null
        };
    #endregion
}
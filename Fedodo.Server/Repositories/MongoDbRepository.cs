using Fedodo.Server.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Fedodo.Server.Repositories;

public class MongoDbRepository : IMongoDbRepository
{
    private readonly IMongoClient _client;
    private readonly ILogger<MongoDbRepository> _logger;

    public MongoDbRepository(ILogger<MongoDbRepository> logger, IMongoClient client)
    {
        _logger = logger;
        _client = client;

        _logger.LogTrace($"Created {nameof(MongoDbRepository)}");
    }

    public async Task Create<T>(T item, string databaseName, string collectionName)
    {
        _logger.LogTrace($"Creating item with type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        await collection.InsertOneAsync(item);

        _logger.LogTrace($"Finished creating item with type: {typeof(T)}");
    }

    public async Task Delete<T>(FilterDefinition<T> filter, string databaseName, string collectionName)
    {
        _logger.LogTrace($"Deleting item with type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        await collection.DeleteOneAsync(filter);

        _logger.LogTrace($"Finished deleting item with type: {typeof(T)}");
    }

    public async Task<IEnumerable<T>> GetAll<T>(string databaseName, string collectionName)
    {
        _logger.LogTrace($"Getting all items of type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        var result = (await collection.FindAsync(new BsonDocument())).ToList();

        _logger.LogTrace($"Finished getting all items of type: {typeof(T)}");

        return result;
    }

    public async Task<IEnumerable<T>> GetAllPaged<T>(string databaseName, string collectionName, int pageId,
        int pageSize,
        SortDefinition<T> sortDefinition)
    {
        _logger.LogTrace($"Getting all items paged of type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        var result = await collection.Find(new BsonDocument())
            .Sort(sortDefinition)
            .Skip(pageId * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        _logger.LogTrace($"Finished getting all items paged of type: {typeof(T)}");

        return result;
    }

    public async Task<IEnumerable<T>> GetAllPagedFromCollections<T>(string databaseName, string collectionName,
        int pageId, int pageSize, SortDefinition<T> sortDefinition, string foreignCollectionName)
    {
        _logger.LogTrace($"Getting all items paged of type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);
        var foreignCollection = database.GetCollection<T>(foreignCollectionName);

        var result = await collection.Aggregate().UnionWith(foreignCollection)
            .Sort(sortDefinition)
            .Skip(pageId * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        _logger.LogTrace($"Finished getting all items paged of type: {typeof(T)}");

        return result;
    }

    public async Task<IEnumerable<T>> GetSpecificPaged<T>(string databaseName, string collectionName, int pageId,
        int pageSize,
        SortDefinition<T> sortDefinition, FilterDefinition<T> filter)
    {
        _logger.LogTrace($"Getting all items paged of type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        var result = await collection.Find(filter)
            .Sort(sortDefinition)
            .Skip(pageId * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        _logger.LogTrace($"Finished getting all items paged of type: {typeof(T)}");

        return result;
    }

    public async Task<long> CountAll<T>(string databaseName, string collectionName)
    {
        _logger.LogTrace($"Counting all items of type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        var result = await collection.CountDocumentsAsync(new BsonDocument());

        _logger.LogTrace($"Finished counting all items of type: {typeof(T)}");

        return result;
    }

    public async Task<long> CountSpecific<T>(string databaseName, string collectionName, FilterDefinition<T> filter)
    {
        _logger.LogTrace($"Counting all items of type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        var result = await collection.CountDocumentsAsync(filter);

        _logger.LogTrace($"Finished counting all items of type: {typeof(T)}");

        return result;
    }

    public async Task<T> GetSpecificItem<T>(FilterDefinition<T> filter, string databaseName, string collectionName)
    {
        _logger.LogTrace($"Getting specific item with type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);
        var result = (await collection.FindAsync(filter)).SingleOrDefault();

        _logger.LogTrace($"Returning specific item with type: {typeof(T)}");

        return result;
    }

    public async Task<IEnumerable<T>> GetSpecificItems<T>(FilterDefinition<T> filter, string databaseName,
        string collectionName)
    {
        _logger.LogTrace($"Getting specific item with type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);
        var result = (await collection.FindAsync(filter)).ToList();

        _logger.LogTrace($"Returning specific item with type: {typeof(T)}");

        return result;
    }

    public async Task Update<T>(T item, FilterDefinition<T> filter, string databaseName, string collectionName)
    {
        _logger.LogInformation($"Updating item of type: {typeof(T)}");

        var database = _client.GetDatabase(databaseName);
        var collection = database.GetCollection<T>(collectionName);

        await collection.ReplaceOneAsync(filter, item);

        _logger.LogInformation($"Finished updating item of type: {typeof(T)}");
    }
}
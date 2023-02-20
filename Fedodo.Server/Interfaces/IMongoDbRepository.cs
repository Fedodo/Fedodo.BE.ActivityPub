using MongoDB.Driver;

namespace Fedodo.Server.Interfaces;

public interface IMongoDbRepository
{
    public Task Create<T>(T item, string databaseName, string collectionName);
    public Task<IEnumerable<T>> GetAll<T>(string databaseName, string collectionName);
    public Task<T> GetSpecificItem<T>(FilterDefinition<T> filter, string databaseName, string collectionName);

    public Task<IEnumerable<T>> GetSpecificItems<T>(FilterDefinition<T> filter, string databaseName,
        string collectionName);

    public Task Update<T>(T item, FilterDefinition<T> filter, string databaseName, string collectionName);
    public Task Delete<T>(FilterDefinition<T> filter, string databaseName, string collectionName);
    public Task<long> CountAll<T>(string databaseName, string collectionName);
    public Task<IEnumerable<T>> GetPaged<T>(string databaseName, string collectionName, int pageId, int pageSize,
        SortDefinition<T> sortDefinition);
}
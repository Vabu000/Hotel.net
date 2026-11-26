using HotelReviews.Domain.Entities;
using HotelReviews.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HotelReviews.Infrastructure.Persistence;
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var mongoSettings = settings.Value;

        var clientSettings = MongoClientSettings.FromConnectionString(mongoSettings.ConnectionString);
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(mongoSettings.ConnectionTimeoutSeconds);
        clientSettings.SocketTimeout = TimeSpan.FromSeconds(mongoSettings.OperationTimeoutSeconds);
        clientSettings.MaxConnectionPoolSize = mongoSettings.MaxConnectionPoolSize;

        if (mongoSettings.UseSsl)
        {
            clientSettings.UseTls = true;
        }

        _client = new MongoClient(clientSettings);
        _database = _client.GetDatabase(mongoSettings.DatabaseName);
    }
    public IMongoCollection<T> GetCollection<T>() where T : class
    {
        var collectionName = GetCollectionName<T>();
        return _database.GetCollection<T>(collectionName);
    }
    public IMongoCollection<Review> Reviews => GetCollection<Review>();
    public IMongoCollection<Request> Requests => GetCollection<Request>();
    public IMongoDatabase Database => _database;
    public MongoClient Client => _client;
    private static string GetCollectionName<T>() where T : class
    {
        if (typeof(T) == typeof(Review))
        {
            return "reviews";
        }

        if (typeof(T) == typeof(Request))
        {
            return "requests";
        }

        return typeof(T).Name.ToLowerInvariant() + "s";
    }
    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}", cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
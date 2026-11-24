using HotelReviews.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace HotelReviews.Infrastructure.Persistence;

internal static class MongoClassMapRegistration
{
    private static readonly object Sync = new();

    public static void Register()
    {
        lock (Sync)
        {
            Register<Review>();
            Register<Request>();
            Register<Discussion>();
            Register<DiscussionMessage>();
        }
    }

    private static void Register<T>() where T : class
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<T>(classMap =>
        {
            classMap.AutoMap();
            classMap.SetIgnoreExtraElements(true);
        });
    }
}

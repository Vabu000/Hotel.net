using HotelReviews.Domain.Common;
using HotelReviews.Domain.Entities;
using HotelReviews.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace HotelReviews.Infrastructure.Persistence;

internal static class MongoClassMapRegistration
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Sync)
        {
            if (_registered)
            {
                return;
            }

            var conventions = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register(
                "HotelReviewsMongoConventions",
                conventions,
                type => type.Namespace?.StartsWith("HotelReviews.Domain", StringComparison.Ordinal) == true);

            BsonSerializer.RegisterSerializer(typeof(Rating), new RatingBsonSerializer());
            BsonSerializer.RegisterSerializer(typeof(RequestStatus), new RequestStatusBsonSerializer());

            RegisterBaseEntityMap();
            Register<DiscussionMessage>();
            Register<Review>(classMap =>
            {
                classMap.UnmapMember(review => review.IsPositive);
                classMap.UnmapMember(review => review.IsNegative);
            });
            Register<Request>(classMap =>
            {
                classMap.UnmapMember(request => request.IsHighPriority);
                classMap.UnmapMember(request => request.IsCritical);
                classMap.UnmapMember(request => request.HasResponse);
            });
            Register<Discussion>();

            _registered = true;
        }
    }

    private static void RegisterBaseEntityMap()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<BaseEntity>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapIdMember(entity => entity.Id)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            classMap.UnmapMember(entity => entity.IsNew);
            classMap.SetIgnoreExtraElements(true);
        });
    }

    private static void Register<T>(Action<BsonClassMap<T>>? configure = null) where T : class
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<T>(classMap =>
        {
            classMap.AutoMap();
            classMap.SetIgnoreExtraElements(true);
            configure?.Invoke(classMap);
        });
    }

    private sealed class RatingBsonSerializer : SerializerBase<Rating>
    {
        public override Rating Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Rating.Create(context.Reader.ReadInt32());

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Rating value) =>
            context.Writer.WriteInt32(value.Value);
    }

    private sealed class RequestStatusBsonSerializer : SerializerBase<RequestStatus>
    {
        public override RequestStatus Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            RequestStatus.Create(context.Reader.ReadString());

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, RequestStatus value) =>
            context.Writer.WriteString(value.Value);
    }
}

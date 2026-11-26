using HotelReviews.Domain.Common;
using HotelReviews.Domain.Entities;
using HotelReviews.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace HotelReviews.Infrastructure.Persistence;

internal static class MongoClassMapRegistration
{
    private static bool _registered;
    private static readonly object Sync = new();

    public static void Register()
    {
        lock (Sync)
        {
            if (_registered)
            {
                return;
            }

            BsonSerializer.RegisterSerializer(typeof(Rating), new RatingSerializer());
            BsonSerializer.RegisterSerializer(typeof(RequestStatus), new RequestStatusSerializer());

            BsonClassMap.RegisterClassMap<BaseEntity>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapIdMember(entity => entity.Id)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId))
                    .SetIdGenerator(StringObjectIdGenerator.Instance);
                classMap.MapMember(entity => entity.CreatedAt).SetElementName("createdAt");
                classMap.MapMember(entity => entity.UpdatedAt).SetElementName("updatedAt");
                classMap.UnmapMember(entity => entity.IsNew);
            });

            BsonClassMap.RegisterClassMap<Review>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapMember(review => review.ClientId).SetElementName("clientId");
                classMap.MapMember(review => review.RoomId).SetElementName("roomId");
                classMap.MapMember(review => review.Rating).SetElementName("rating");
                classMap.MapMember(review => review.Comment).SetElementName("comment");
                classMap.MapMember(review => review.Date).SetElementName("date");
                classMap.MapMember(review => review.IsVerified).SetElementName("isVerified");
                classMap.MapMember(review => review.IsApproved).SetElementName("isApproved");
                classMap.MapMember(review => review.RejectionReason).SetElementName("rejectionReason");
                classMap.UnmapMember(review => review.IsPositive);
                classMap.UnmapMember(review => review.IsNegative);
            });

            BsonClassMap.RegisterClassMap<Request>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapMember(request => request.ClientId).SetElementName("clientId");
                classMap.MapMember(request => request.RoomId).SetElementName("roomId");
                classMap.MapMember(request => request.RequestText).SetElementName("request");
                classMap.MapMember(request => request.Status).SetElementName("status");
                classMap.MapMember(request => request.Date).SetElementName("date");
                classMap.MapMember(request => request.Priority).SetElementName("priority");
                classMap.MapMember(request => request.Category).SetElementName("category");
                classMap.MapMember(request => request.Response).SetElementName("response");
                classMap.MapMember(request => request.ResponseDate).SetElementName("responseDate");
                classMap.MapMember(request => request.HandledBy).SetElementName("handledBy");
                classMap.UnmapMember(request => request.IsHighPriority);
                classMap.UnmapMember(request => request.IsCritical);
                classMap.UnmapMember(request => request.HasResponse);
            });

            _registered = true;
        }
    }

    private sealed class RatingSerializer : SerializerBase<Rating>
    {
        public override Rating Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Rating.Create(context.Reader.ReadInt32());

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Rating value) =>
            context.Writer.WriteInt32(value.Value);
    }

    private sealed class RequestStatusSerializer : SerializerBase<RequestStatus>
    {
        public override RequestStatus Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            RequestStatus.Create(context.Reader.ReadString());

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, RequestStatus value) =>
            context.Writer.WriteString(value.Value);
    }
}

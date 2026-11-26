const reviews = db.getCollection("reviews");
const requests = db.getCollection("requests");
const discussions = db.getCollection("discussions");

reviews.updateOne(
  { _id: ObjectId("65a000000000000000000001") },
  {
    $setOnInsert: {
      clientId: 1,
      roomId: 101,
      rating: NumberInt(5),
      comment: "Clean room, friendly staff, and a quiet location.",
      date: ISODate("2025-01-15T12:00:00Z"),
      isVerified: true,
      isApproved: true,
      createdAt: ISODate("2025-01-15T12:00:00Z"),
      updatedAt: null,
      version: NumberInt(0)
    }
  },
  { upsert: true }
);

requests.updateOne(
  { _id: ObjectId("65a000000000000000000002") },
  {
    $setOnInsert: {
      clientId: 1,
      roomId: 101,
      request: "Please provide a baby cot for the room.",
      status: "pending",
      date: ISODate("2025-01-16T09:00:00Z"),
      priority: NumberInt(3),
      category: "accommodation",
      response: null,
      responseDate: null,
      handledBy: null,
      createdAt: ISODate("2025-01-16T09:00:00Z"),
      updatedAt: null,
      version: NumberInt(0)
    }
  },
  { upsert: true }
);

// Hybrid document example: roomId/reviewId are logical references to other
// service-owned data; the bounded message list is embedded in the discussion.
discussions.updateOne(
  { _id: ObjectId("65a000000000000000000003") },
  {
    $setOnInsert: {
      roomId: NumberInt(101),
      reviewId: "65a000000000000000000001",
      subject: "Question about the room review",
      messages: [
        { clientId: NumberInt(1), text: "Is there a quiet place to work?", createdAt: ISODate("2025-01-17T10:00:00Z") },
        { clientId: NumberInt(2), text: "Yes, the room has a desk by the window.", createdAt: ISODate("2025-01-17T10:05:00Z") }
      ],
      // Optional fields demonstrate a flexible schema for evolving discussion metadata.
      moderation: { state: "open", labels: ["room-question"] },
      createdAt: ISODate("2025-01-17T10:00:00Z"),
      updatedAt: null,
      version: NumberInt(0)
    }
  },
  { upsert: true }
);

// A second document has a different optional field set in the same collection.
discussions.updateOne(
  { _id: ObjectId("65a000000000000000000004") },
  {
    $setOnInsert: {
      roomId: NumberInt(102),
      subject: "Breakfast schedule",
      messages: [
        { clientId: NumberInt(3), text: "What time is breakfast served?", createdAt: ISODate("2025-01-18T08:00:00Z") }
      ],
      priority: NumberInt(2),
      createdAt: ISODate("2025-01-18T08:00:00Z"),
      version: NumberInt(0)
    }
  },
  { upsert: true }
);

db.discussions.createIndex({ roomId: 1, createdAt: -1 }, { name: "idx_discussions_room_created" });
db.discussions.createIndex({ reviewId: 1 }, { name: "idx_discussions_review" });

// Optional validator: required aggregate fields are checked while extra fields remain allowed.
db.runCommand({
  collMod: "discussions",
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["roomId", "subject", "messages"],
      properties: {
        roomId: { bsonType: "int", minimum: 1 },
        reviewId: { bsonType: ["string", "null"] },
        subject: { bsonType: "string", minLength: 1 },
        messages: {
          bsonType: "array",
          maxItems: 100,
          items: {
            bsonType: "object",
            required: ["clientId", "text", "createdAt"],
            properties: {
              clientId: { bsonType: "int", minimum: 1 },
              text: { bsonType: "string", minLength: 1 },
              createdAt: { bsonType: "date" }
            }
          }
        }
      }
    }
  },
  validationLevel: "moderate",
  validationAction: "warn"
});

// Example read aggregation: average approved rating and count grouped by room.
db.reviews.aggregate([
  { $match: { isApproved: true } },
  { $group: { _id: "$roomId", averageRating: { $avg: "$rating" }, reviewCount: { $sum: 1 } } },
  { $sort: { averageRating: -1 } }
]);

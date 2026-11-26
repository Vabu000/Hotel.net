// Project 3: Idempotent MongoDB Seed Script (demonstrating embedding, referencing, and flexible schema)

const reviews = db.getCollection("reviews");
const requests = db.getCollection("requests");
const discussions = db.getCollection("discussions");

reviews.updateOne(
  { _id: ObjectId("65a000000000000000000001") },
  {
    $setOnInsert: {
      clientId: NumberInt(1),
      roomId: NumberInt(101),
      roomName: "Deluxe 101", // denormalized read field updated via catalog events
      rating: NumberInt(5),
      comment: "Clean room, friendly staff, and a quiet location.",
      date: ISODate("2025-01-15T12:00:00Z"),
      isVerified: true,
      isApproved: true,
      tags: ["cleanliness", "quiet"], // flexible schema field
      createdAt: ISODate("2025-01-15T12:00:00Z"),
      updatedAt: null,
      version: NumberInt(0)
    }
  },
  { upsert: true }
);

reviews.updateOne(
  { _id: ObjectId("65a000000000000000000005") },
  {
    $setOnInsert: {
      clientId: NumberInt(2),
      roomId: NumberInt(102),
      roomName: "Standard 102",
      rating: NumberInt(4),
      comment: "Great breakfast, convenient check-in.",
      date: ISODate("2025-01-16T14:30:00Z"),
      isVerified: false,
      isApproved: true,
      createdAt: ISODate("2025-01-16T14:30:00Z"),
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
      clientId: NumberInt(1),
      roomId: NumberInt(101),
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

// Hybrid document: logical references (roomId, reviewId) + embedded bounded messages array + flexible metadata
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
      moderation: { state: "open", labels: ["room-question"] },
      createdAt: ISODate("2025-01-17T10:00:00Z"),
      updatedAt: null,
      version: NumberInt(0)
    }
  },
  { upsert: true }
);

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

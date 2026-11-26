// Project 3: MongoDB Collections, Indexes, and JSON Schema Validation

db.createCollection("reviews", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["clientId", "roomId", "rating", "comment"],
      properties: {
        clientId: { bsonType: "int", minimum: 1 },
        roomId: { bsonType: "int", minimum: 1 },
        rating: { bsonType: "int", minimum: 1, maximum: 5 },
        comment: { bsonType: "string", minLength: 1 }
      }
    }
  },
  validationLevel: "moderate"
});

db.createCollection("requests");

db.createCollection("discussions", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["roomId", "subject", "messages"],
      properties: {
        roomId: { bsonType: "int", minimum: 1 },
        reviewId: { bsonType: ["string", "null"] },
        subject: { bsonType: "string", minLength: 1 },
        messages: { bsonType: "array", maxItems: 100 }
      }
    }
  },
  validationLevel: "moderate"
});

// Indexes for common query patterns
db.reviews.createIndex({ roomId: 1, date: -1 }, { name: "idx_reviews_room_date" });
db.reviews.createIndex({ clientId: 1, roomId: 1 }, { name: "idx_reviews_client_room" });
db.reviews.createIndex({ comment: "text" }, { name: "idx_reviews_text" });

db.requests.createIndex({ status: 1, priority: -1 }, { name: "idx_requests_status_priority" });
db.requests.createIndex({ clientId: 1 }, { name: "idx_requests_client" });

db.discussions.createIndex({ roomId: 1, createdAt: -1 }, { name: "idx_discussions_room_created" });
db.discussions.createIndex({ reviewId: 1 }, { name: "idx_discussions_review" });

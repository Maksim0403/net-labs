/* =====================================================================
   Проєкт №3 — Tracking & Reviews (MongoDB)
   Запуск: mongosh --file db/p3/collections.js
   ===================================================================== */
use("trackingdb");

// ---------------------------------------------------------------------
// shipment_tracking — EMBEDDING: події та поточний кур'єр вбудовані
// в один документ, бо завжди потрібні разом із відправленням.
// currentStatus і courier.name/phone — дубльовані поля (джерело: p1.Shipments
// та p2.Couriers відповідно), оновлюються подіями. Див. docs/dedup-policy.md
// ---------------------------------------------------------------------
db.createCollection("shipment_tracking", {
  validator: { $jsonSchema: {
    bsonType: "object",
    required: ["shipmentId", "trackingNumber", "currentStatus", "events", "updatedAt"],
    properties: {
      shipmentId: { bsonType: ["int", "long"] },
      trackingNumber: { bsonType: "string" },
      currentStatus: { enum: ["Draft", "Confirmed", "InTransit", "Delivered", "Cancelled"] },
      courier: {
        bsonType: "object",
        properties: {
          courierId: { bsonType: ["int", "long"] },
          name: { bsonType: "string" },
          phone: { bsonType: "string" }
        }
      },
      events: {
        bsonType: "array",
        items: {
          bsonType: "object",
          required: ["ts", "type"],
          properties: {
            ts: { bsonType: "date" },
            type: { enum: ["Picked", "InTransit", "Delivered", "Delayed", "Returned"] },
            location: { bsonType: "object" },
            note: { bsonType: "string" }
          }
        }
      }
    }
  } },
  validationLevel: "moderate"
});

if (!db.shipment_tracking.getIndexes().some(i => i.name === "ux_shipmentId"))
  db.shipment_tracking.createIndex({ shipmentId: 1 }, { unique: true, name: "ux_shipmentId" });
if (!db.shipment_tracking.getIndexes().some(i => i.name === "ux_trackingNumber"))
  db.shipment_tracking.createIndex({ trackingNumber: 1 }, { unique: true, name: "ux_trackingNumber" });

// ---------------------------------------------------------------------
// reviews — HYBRID: прямі поля-посилання (reference за id, без FK) на
// shipmentId/courierId + вбудований масив comments (REFERENCE + EMBEDDING
// в одному документі) + courierName продубльовано для швидкого читання.
// Документи різної структури в одній колекції: type=rating / photo_review.
// ---------------------------------------------------------------------
db.createCollection("reviews", {
  validator: { $jsonSchema: {
    bsonType: "object",
    required: ["shipmentId", "customerId", "type", "rating", "createdAt"],
    properties: {
      shipmentId: { bsonType: ["int", "long"] },        // reference на p1.Shipments, без FK
      courierId:  { bsonType: ["int", "long"] },         // reference на p2.Couriers, без FK
      courierName: { bsonType: "string" },                // денормалізовано для читання
      type: { enum: ["rating", "photo_review", "text"] },
      rating: { bsonType: "int", minimum: 1, maximum: 5 },
      comments: { bsonType: "array" }
    }
  } },
  validationLevel: "moderate"
});

if (!db.reviews.getIndexes().some(i => i.name === "ix_courier_created"))
  db.reviews.createIndex({ courierId: 1, createdAt: -1 }, { name: "ix_courier_created" });
if (!db.reviews.getIndexes().some(i => i.name === "ix_shipment"))
  db.reviews.createIndex({ shipmentId: 1 }, { name: "ix_shipment" });

// ---------------------------------------------------------------------
// discussions — REFERENCING: окрема колекція для потенційно великих,
// необмежених обговорень, на яку reviews лише посилаються через discussionId
// ---------------------------------------------------------------------
db.createCollection("discussions", {
  validator: { $jsonSchema: {
    bsonType: "object",
    required: ["reviewId", "messages"],
    properties: {
      reviewId: { bsonType: "objectId" },     // reference на _id у reviews
      messages: { bsonType: "array" }
    }
  } }
});
if (!db.discussions.getIndexes().some(i => i.name === "ix_reviewId"))
  db.discussions.createIndex({ reviewId: 1 }, { name: "ix_reviewId" });

// ---------------------------------------------------------------------
// incidents — гнучка схема: різний набір полів у details залежно від type
// ---------------------------------------------------------------------
db.createCollection("incidents", {
  validator: { $jsonSchema: {
    bsonType: "object",
    required: ["shipmentId", "type", "severity", "status", "createdAt"],
    properties: {
      shipmentId: { bsonType: ["int", "long"] },
      type: { enum: ["damage", "delay", "lost", "wrong_address"] },
      severity: { enum: ["low", "medium", "high"] },
      status: { enum: ["open", "closed"] },
      details: { bsonType: "object" }     // поля різні залежно від type
    }
  } }
});
if (!db.incidents.getIndexes().some(i => i.name === "ix_status_severity"))
  db.incidents.createIndex({ status: 1, severity: 1 }, { name: "ix_status_severity" });

print("Колекції та індекси trackingdb готові.");

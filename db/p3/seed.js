/* Запуск: mongosh --file db/p3/seed.js
   Ідемпотентність: upsert за ключовими полями — повторний запуск
   не створює дублікатів. */
use("trackingdb");

// shipment_tracking: embedded events + продубльований courier/currentStatus
db.shipment_tracking.updateOne(
  { shipmentId: 1 },
  { $setOnInsert: {
      shipmentId: 1,
      trackingNumber: "UA2026000123",
      currentStatus: "InTransit",
      courier: { courierId: 1, name: "Андрій Мельник", phone: "+380991112233" },
      events: [
        { ts: new Date("2026-09-24T08:00:00Z"), type: "Picked", note: "Забрано зі складу" },
        { ts: new Date("2026-09-24T09:30:00Z"), type: "InTransit" }
      ],
      updatedAt: new Date("2026-09-24T09:30:00Z")
  } },
  { upsert: true }
);

// reviews: два документи РІЗНОЇ структури в одній колекції (гнучка схема)
const r1 = db.reviews.findOneAndUpdate(
  { shipmentId: 1, customerId: 1, type: "rating" },
  { $setOnInsert: {
      shipmentId: 1, customerId: 1, courierId: 1, courierName: "Андрій Мельник",
      type: "rating", rating: 5, text: "Швидко та ввічливо",
      criteria: { speed: 5, politeness: 5, packaging: 4 },
      comments: [
        { author: "support", text: "Дякуємо за відгук!", createdAt: new Date("2026-09-25T10:00:00Z") }
      ],
      createdAt: new Date("2026-09-25T09:00:00Z"), isDeleted: false
  } },
  { upsert: true, returnDocument: "after" }
);

db.reviews.updateOne(
  { shipmentId: 2, customerId: 2, type: "photo_review" },
  { $setOnInsert: {
      shipmentId: 2, customerId: 2, courierId: 1, courierName: "Андрій Мельник",
      type: "photo_review", rating: 3,
      photos: [{ url: "https://cdn.example.com/x.jpg", caption: "Пошкоджена коробка" }],
      tags: ["damage", "packaging"],
      createdAt: new Date("2026-09-26T12:00:00Z"), isDeleted: false
  } },
  { upsert: true }
);

// discussions: reference на _id відгуку (без FK, логічне посилання)
if (r1 && r1._id) {
  db.discussions.updateOne(
    { reviewId: r1._id },
    { $setOnInsert: {
        reviewId: r1._id,
        messages: [
          { author: "Іван", text: "Теж задоволений доставкою", createdAt: new Date("2026-09-25T11:00:00Z") }
        ]
    } },
    { upsert: true }
  );
}

// incidents: гнучкі details залежно від type
db.incidents.updateOne(
  { shipmentId: 2, type: "damage" },
  { $setOnInsert: {
      shipmentId: 2, type: "damage", severity: "high",
      details: { description: "Розірвана упаковка", photoUrls: ["https://cdn.example.com/y.jpg"] },
      status: "open", createdAt: new Date("2026-09-26T13:00:00Z")
  } },
  { upsert: true }
);

db.incidents.updateOne(
  { shipmentId: 5, type: "delay" },
  { $setOnInsert: {
      shipmentId: 5, type: "delay", severity: "low",
      details: { delayMinutes: 45, reason: "Затор" },
      status: "closed", createdAt: new Date("2026-09-27T10:00:00Z")
  } },
  { upsert: true }
);

print("Seed trackingdb виконано (ідемпотентно).");

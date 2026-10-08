# Проєкт №3 — MongoDB (Tracking & Reviews)

## Колекції та прийоми моделювання

| Колекція | Прийом | Чому |
|---|---|---|
| `shipment_tracking` | **Embedding** | події та дані кур'єра завжди потрібні разом з відправленням, їх небагато |
| `reviews` | **Hybrid** | `shipmentId`/`courierId` — reference (id без FK), `comments` — embedded масив, `courierName` — денормалізовано |
| `discussions` | **Referencing** | обговорення може містити необмежену кількість повідомлень — окрема колекція з `reviewId` |
| `incidents` | **Гнучка схема** | `details` має різний набір полів залежно від `type` |

## Приклади документів

**shipment_tracking** (embedding; `currentStatus` і `courier.*` — дубльовані
поля, джерела — p1 і p2, див. `dedup-policy.md`):
```json
{
  "shipmentId": 1,
  "trackingNumber": "UA2026000123",
  "currentStatus": "InTransit",
  "courier": { "courierId": 1, "name": "Андрій Мельник", "phone": "+380991112233" },
  "events": [
    { "ts": "2026-09-24T08:00:00Z", "type": "Picked", "note": "Забрано зі складу" },
    { "ts": "2026-09-24T09:30:00Z", "type": "InTransit" }
  ],
  "updatedAt": "2026-09-24T09:30:00Z"
}
```

**reviews** — два документи різної структури в одній колекції (hybrid +
гнучка схема):
```json
{
  "shipmentId": 1, "customerId": 1, "courierId": 1, "courierName": "Андрій Мельник",
  "type": "rating", "rating": 5, "text": "Швидко та ввічливо",
  "criteria": { "speed": 5, "politeness": 5, "packaging": 4 },
  "comments": [ { "author": "support", "text": "Дякуємо за відгук!", "createdAt": "2026-09-25T10:00:00Z" } ]
}
{
  "shipmentId": 2, "customerId": 2, "courierId": 1, "courierName": "Андрій Мельник",
  "type": "photo_review", "rating": 3,
  "photos": [ { "url": "https://cdn.example.com/x.jpg", "caption": "Пошкоджена коробка" } ],
  "tags": ["damage", "packaging"]
}
```

**discussions** (referencing на `reviews._id`):
```json
{ "reviewId": "ObjectId(...)", "messages": [ { "author": "Іван", "text": "Теж задоволений", "createdAt": "2026-09-25T11:00:00Z" } ] }
```

**incidents** (гнучка схема — різні поля в `details` залежно від `type`):
```json
{ "shipmentId": 2, "type": "damage", "severity": "high", "status": "open",
  "details": { "description": "Розірвана упаковка", "photoUrls": ["https://cdn.example.com/y.jpg"] } }
{ "shipmentId": 5, "type": "delay", "severity": "low", "status": "closed",
  "details": { "delayMinutes": 45, "reason": "Затор" } }
```

## Приклади запитів та агрегацій

```javascript
use("trackingdb");

// пагінація відгуків кур'єра
db.reviews.find({ courierId: 1, isDeleted: { $ne: true } }).sort({ createdAt: -1 }).skip(0).limit(10);

// додати подію відстеження
db.shipment_tracking.updateOne(
  { shipmentId: 1 },
  { $push: { events: { ts: new Date(), type: "Delivered", note: "Вручено" } },
    $set: { currentStatus: "Delivered", updatedAt: new Date() } }
);

// середній рейтинг кур'єрів (aggregation pipeline)
db.reviews.aggregate([
  { $match: { isDeleted: { $ne: true } } },
  { $group: { _id: "$courierId", name: { $first: "$courierName" }, avgRating: { $avg: "$rating" }, cnt: { $sum: 1 } } },
  { $sort: { avgRating: -1 } }
]);

// інциденти за типами
db.incidents.aggregate([{ $group: { _id: "$type", total: { $sum: 1 } } }]);
```

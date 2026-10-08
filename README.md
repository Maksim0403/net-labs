# Лаб. 01 — Проєктування БД для мікросервісної архітектури

**Тема:** Доставка та логістика

## Предметна область

Сервіс кур'єрської доставки: клієнти оформлюють відправлення, компанія
призначає кур'єрів і транспорт зі свого автопарку, отримувачі відстежують
посилку та залишають відгуки. Детальний опис контекстів — `docs/contexts.md`.

## Три bounded context і три бази

| Контекст | Технологія | БД | Папка зі скриптами |
|---|---|---|---|
| **Shipping** (відправлення) | SQL Server + ADO.NET/Dapper | `ShippingDb` | `db/p1/` |
| **Fleet** (автопарк, маршрути) | SQL Server + EF Core (схема; реалізація — Лаб. 03) | `FleetDb` | `db/p2/` |
| **Tracking & Reviews** | MongoDB | `trackingdb` | `db/p3/` |

Між базами немає жодного `FOREIGN KEY` — лише логічні id-посилання. Детальніше
про дубльовані поля — `docs/dedup-policy.md`.

## Документація

- `docs/contexts.md` — опис домену та меж контекстів
- `docs/dedup-policy.md` — таблиця політики дублювання даних
- `docs/erd-p1.md`, `docs/erd-p2.md` — ERD-діаграми (Mermaid)
- `docs/mongo-model.md` — модель колекцій MongoDB, приклади документів і запитів

## Розгортання з нуля

### Вимоги
SQL Server (Developer/Express, 2019+), SSMS або Azure Data Studio, MongoDB 6+,
mongosh.

### Проєкт №1 — Shipping (SQL Server)
```bash
sqlcmd -S <server> -i db/p1/schema.sql
sqlcmd -S <server> -i db/p1/procedures.sql
sqlcmd -S <server> -i db/p1/seed.sql
```
Або відкрий файли по черзі в SSMS і виконай (F5): `schema.sql` →
`procedures.sql` → `seed.sql`.

**Перевірка:**
```sql
USE ShippingDb;
SELECT name FROM sys.procedures ORDER BY name;      -- має бути 6 процедур
SELECT * FROM sys.foreign_keys;                       -- усі FK лише в межах цієї БД
EXEC dbo.usp_GetCustomerShipments @CustomerId = 1, @Page = 1, @PageSize = 10;
```

### Проєкт №2 — Fleet (SQL Server)
```bash
sqlcmd -S <server> -i db/p2/schema.sql
sqlcmd -S <server> -i db/p2/seed.sql
```
**Перевірка:**
```sql
USE FleetDb;
SELECT * FROM sys.foreign_keys;   -- усі FK лише в межах цієї БД
SELECT * FROM dbo.CourierZones;   -- M:N з власним полем AssignedAt
```

### Проєкт №3 — Tracking & Reviews (MongoDB)
```bash
mongosh --file db/p3/collections.js
mongosh --file db/p3/seed.js
```
**Перевірка:**
```javascript
use("trackingdb");
db.reviews.find();              // документи різної структури (rating / photo_review)
db.shipment_tracking.find();
db.getCollectionNames();        // shipment_tracking, reviews, discussions, incidents
```

### Порядок для всього репозиторію
1. `db/p1/schema.sql` → `db/p1/procedures.sql` → `db/p1/seed.sql`
2. `db/p2/schema.sql` → `db/p2/seed.sql`
3. `db/p3/collections.js` → `db/p3/seed.js`

Усі seed-скрипти ідемпотентні: повторний запуск не створює дублікатів
(перевірка `IF NOT EXISTS` / `updateOne(... , { upsert: true })`).

## Таблиця політики дублювання (коротко)

| Поле | Де | Джерело | Політика |
|---|---|---|---|
| `Shipments.AssignedCourierName/Phone` | p1 | p2.Couriers | copy-for-read, подія `CourierUpdated` |
| `ShipmentServices.ServiceName/UnitPrice` | p1 | p1.ServiceTypes | знімок, не оновлюється |
| `shipment_tracking.currentStatus` | p3 | p1.Shipments | copy-for-read, подія `ShipmentStatusChanged` |
| `shipment_tracking.courier.*`, `reviews.courierName` | p3 | p2.Couriers | copy-for-read, подія `CourierUpdated` |

Повна версія — `docs/dedup-policy.md`.
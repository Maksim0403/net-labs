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

## Лаб. 02 — Shipping.Api: тришарова архітектура на ADO.NET та Dapper

### Структура рішення

```
Platform.slnx
Platform.AppHost/              # оркестрація через .NET Aspire
Services/Shipping/
├── Shipping.Api/               # контролери, Program.cs, appsettings.json
├── Shipping.Bll/                # сервіси, DTO, профіль AutoMapper
├── Shipping.Dal/                 # репозиторії (ADO.NET + Dapper), Unit of Work
└── Shipping.Domain/              # моделі, доменні винятки
```

Залежності спрямовані в один бік: `Api → Bll → Dal`, `Domain` підключають і `Dal`, і `Bll`. Контролери звертаються лише до сервісів BLL, ніколи — до репозиторіїв напряму.

### Що реалізовано

- **DAL:** `IGenericRepository<T>` + спеціалізовані інтерфейси; `CustomerRepository` — чистий ADO.NET (SqlConnection/SqlCommand, ручне читання SqlDataReader); `ShipmentRepository` і `ServiceTypeRepository` — Dapper, спільний CRUD у `BaseDapperRepository<T>`; `UnitOfWork` з одним з'єднанням і транзакцією (рівень ізоляції `ReadCommitted`, обґрунтування — у коментарі `UnitOfWork.cs`).
- **BLL:** DTO з DataAnnotations, профіль AutoMapper `ShippingProfile`, сервіси `ShipmentService`/`CustomerService` з доменними винятками `NotFoundException`/`BusinessConflictException`/`ValidationException`.
- **Флагманська транзакційна операція** — `ShipmentService.ConfirmAsync`: виклик `usp_ConfirmShipment` (репозиторій Shipments) + оновлення клієнта `TouchAsync` (репозиторій Customers) в одній транзакції Unit of Work; при помилці — повний відкат.
- **API:** тонкі контролери `ShipmentsController`/`CustomersController`, атрибутна маршрутизація, `CreatedAtAction` для 201, `NoContent` для 204; `DomainExceptionHandler` централізовано перетворює винятки на `ProblemDetails` (404/409/400); Swagger підключено в `Program.cs`.
- **Aspire:** `Platform.AppHost/AppHost.cs` реєструє ресурс підключення `ShippingDb` (той самий ключ, що і в `appsettings.json` сервісу) і передає його в `Shipping.Api` через `WithReference`.

### Запуск

1. База `ShippingDb` має бути розгорнута за інструкцією з Лаб. 01 (`db/p1/schema.sql → procedures.sql → seed.sql`).
2. Перевір/онови рядок підключення в `Platform.AppHost/appsettings.json` і `Services/Shipping/Shipping.Api/appsettings.json` (ключ `ConnectionStrings:ShippingDb`).
3. З кореня репозиторію:
   ```bash
   dotnet restore
   dotnet run --project Platform.AppHost
   ```
4. Відкрий посилання на **Aspire Dashboard** з консолі — ресурс `shipping-api` має мати статус **Running**.
5. Візьми фактичний HTTPS endpoint ресурсу `shipping-api` з Dashboard і відкрий його з `/swagger`. Порт API призначається Aspire динамічно.
6. У поточній конфігурації SQL Server Express є локальною залежністю: Aspire передає connection string через `WithReference`, але не створює локальний SQL Server.

`Platform.DatabaseInitializer` є одноразовим startup-компонентом контейнерного
режиму. Його повторний запуск безпечний для вже підготовленої бази завдяки
`CREATE OR ALTER` процедур і idempotent seed.

Для перевірки автоматичних тестів:

```bash
dotnet test Services/Shipping/Shipping.Tests/Shipping.Tests.csproj
```

Інтеграційний тест використовує реальний SQL Server лише коли задано
`SHIPPING_TEST_CONNECTION_STRING`. Рядок має вказувати на окрему тестову базу,
назва якої містить `test`; тест створює схему, процедури й seed, а після виконання
видаляє цю базу. Не вказуй робочу базу:

```powershell
$env:SHIPPING_TEST_CONNECTION_STRING = "Server=.\SQLEXPRESS;Database=ShippingLab02Test;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test Services/Shipping/Shipping.Tests/Shipping.Tests.csproj
```

### Приклади запитів

```bash
# Візьми API_BASE_URL із Aspire Dashboard, наприклад https://localhost:49741
# Клієнти
curl -k -X POST ${API_BASE_URL}/api/customers \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Іван Петренко","phone":"+380501112233","email":"ivan@example.com"}'

# Створення відправлення
curl -k -X POST ${API_BASE_URL}/api/shipments \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "senderAddress": "Чернівці, вул. Руська, 100",
    "recipientName": "Коваль Ірина",
    "recipientPhone": "+380931234567",
    "recipientAddress": "Київ, вул. Хрещатик, 5",
    "weightKg": 2.5,
    "declaredValue": 1000,
    "items": [ { "serviceTypeId": 1, "quantity": 1 } ]
  }'

# Підтвердження (транзакційна операція, 204 або 404/409)
curl -k -X POST ${API_BASE_URL}/api/shipments/1/confirm

# Повторне підтвердження -> 409 ProblemDetails
curl -k -i -X POST ${API_BASE_URL}/api/shipments/1/confirm

# Неіснуючий Id -> 404, а не 500
curl -k -i ${API_BASE_URL}/api/shipments/999999
```

### Перевірка перед здачею

- [ ] Передай значення зі спецсимволом (`O'Brien`) — синтаксичної помилки SQL не виникає (параметризація).
- [ ] Викликай `confirm` для вже підтвердженого відправлення — очікується `409` у форматі ProblemDetails.
- [ ] Запроси неіснуючий `Id` — очікується `404`, не `500`.
- [ ] `dotnet run --project Platform.AppHost` — у Dashboard `shipping-api` і база мають статус Running.
- [ ] `/swagger` відкривається і показує всі ендпоінти.

---

## Лаб. 03 — Fleet.Api: тришарова архітектура на EF Core (Code-First)

### Структура

```
Services/Fleet/
├── Fleet.Api/      контролери, ErrorHandling/, Program.cs, appsettings.json, Dockerfile
├── Fleet.Bll/      сервіси, Dto/, Mapping/, Validators/, Specifications/
├── Fleet.Dal/      FleetDbContext, Configurations/, Migrations/, Repositories/, UnitOfWork, Seeding/
└── Fleet.Domain/   сутності з бізнес-правилами, доменні винятки
```

Залежності: `Api → Bll → Dal`, `Domain` підключають `Dal` і `Bll`. Контролери не звертаються до `DbContext` чи репозиторіїв напряму, лише до сервісів BLL.

### Що реалізовано

- **Code-First + Fluent API:** окремий `IEntityTypeConfiguration<T>` для кожної сутності, підключені централізовано через `ApplyConfigurationsFromAssembly`. Зв'язки: 1:1 `Courier ↔ CourierLicense` (PK=FK), 1:N `Warehouse → Vehicle/Courier`, M:N `Courier ↔ DeliveryZone` через `CourierZone` (власне поле `AssignedAt`).
- **Бізнес-правила в сутностях (без відкритих setter-ів):**
  - `Vehicle`: перевірка `CapacityKg > 0` у конструкторі та методі `ChangeCapacity(decimal)`.
  - `Route`: перевірка `PlannedDistanceKm >= 0`; методи `Start()`, `Complete()`, `Cancel()` валідують допустимі переходи статусів (`Planned → InProgress → Completed` та `Planned/InProgress → Cancelled`), інакше кидають `DomainException`.
  - `Courier`: метод `AssignToZone(DeliveryZone zone)` перевіряє унікальність призначення і додає `CourierZone`.
- **Репозиторії та Unit of Work:**
  - `Async Generic Repository` над `DbSet<T>` (`GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`, `Delete`, `ListAsync`, `CountAsync`).
  - `CourierRepository` (**Eager Loading**): `GetWithDetailsAsync` завантажує кур'єра, ліцензію, склад і зони одним SQL-запитом через `Include` / `ThenInclude`.
  - `WarehouseRepository` (**Explicit Loading**): `GetWithVehiclesAsync` завантажує склад 1-м запитом, а колекцію транспорту — 2-м запитом через `Entry.Collection.LoadAsync`.
  - `CourierZoneRepository` (**LINQ to Entities для M:N**): `GetCouriersAssignedSinceAsync` робить вибірку через проміжну сутність.
  - `UnitOfWork` об'єднує репозиторії з єдиним `SaveChangesAsync()`.
- **Пагінація, фільтрація, сортування на рівні SQL:**
  - `CouriersFilterSpec` рахує `totalCount` усіх відповідних записів у БД.
  - `CouriersPageSpec` застосовує `Where`, `OrderBy`, `Skip` та `Take` — база повертає лише потрібну сторінку.
  - Метадані пагінації повертаються і в тілі відповіді (`PagedResult<T>`), і в HTTP-заголовках (`X-Total-Count`, `X-Page`, `X-Page-Size`).
- **Валідація:** DataAnnotations на DTO для простих правил + FluentValidation для складних бізнес-правил (формат телефону, код складу, номер знака) з явною перевіркою та відповіддю `ValidationProblemDetails` (HTTP 400).
- **Централізована обробка помилок:** `DomainExceptionHandler` (`IExceptionHandler`) мапить винятки на стандартизовані коди ProblemDetails: `EntityNotFoundException` → 404, `ValidationException` → 400, `ConflictException` / `DomainException` / `DbUpdateConcurrencyException` → 409, непередбачені помилки → 500.
- **Serilog:** структуровані логи, збагачення контексту (`CorrelationId`, `RequestPath`), вивід команд SQL EF Core на рівні `Information`.
- **Сідінг:**
  - Модельний (`HasData`) у конфігураціях для довідників (склади `CV-01`, `KY-01` та зони `Центр`, `Околиці`, `Міжміська`).
  - Програмний ідемпотентний `DbInitializer` після `MigrateAsync()` з перевіркою `AnyAsync` (транспорт `AA1234BT`, кур'єр `Андрій Мельник`, ліцензія `AB123456`, призначення в зону `Центр`).

### База даних і міграції

Сервіс налаштований на роботу з базою `FleetEfDb` (щоб уникнути конфліктів з існуючою базою `FleetDb` з Лаб. 01).
> [!NOTE]
> Якщо потрібно використовувати базу з назвою `FleetDb`, перед виконанням міграцій слід виконати `DROP DATABASE FleetDb;` у SQL Server, оскільки міграції EF Core створюють таблиці самостійно.

Команди міграцій:
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project Services/Fleet/Fleet.Dal --startup-project Services/Fleet/Fleet.Api
dotnet ef database update --project Services/Fleet/Fleet.Dal --startup-project Services/Fleet/Fleet.Api
dotnet ef migrations script --project Services/Fleet/Fleet.Dal --startup-project Services/Fleet/Fleet.Api -o Services/Fleet/Fleet.Dal/Migrations/InitialCreate.sql
```

Міграції також застосовуються автоматично під час старту застосунку (`MigrateAsync`).

### Запуск

```bash
# Окремий запуск Fleet.Api (порт за замовчуванням: http://localhost:5080)
dotnet run --project Services/Fleet/Fleet.Api

# Або запуск всього рішення через Aspire AppHost
dotnet run --project Platform.AppHost
```

Swagger UI доступний за адресою: `http://localhost:5080/swagger`

### Приклади запитів (порт 5080 або порт з Aspire Dashboard)

```bash
# 1. Список кур'єрів з пагінацією, фільтрацією та сортуванням
curl "http://localhost:5080/api/couriers?page=1&pageSize=5&search=Андр&sortBy=name&sortDir=desc"

# 2. Кур'єр з деталями (Eager Loading — 1 SQL-запит)
curl http://localhost:5080/api/couriers/1

# 3. Склад з транспортом (Explicit Loading — 2 SQL-запити)
curl http://localhost:5080/api/warehouses/1

# 4. Створення кур'єра: 201 Created + заголовок Location
curl -i -X POST http://localhost:5080/api/couriers -H "Content-Type: application/json" \
  -d '{"warehouseId":1,"fullName":"Олексій Шевченко","phone":"+380507654321"}'

# 5. Повторне створення з тим самим номером телефону: 409 Conflict (ProblemDetails)
curl -i -X POST http://localhost:5080/api/couriers -H "Content-Type: application/json" \
  -d '{"warehouseId":1,"fullName":"Олексій Шевченко","phone":"+380507654321"}'

# 6. Помилка валідації (порожнє ім'я): 400 Bad Request (ValidationProblemDetails)
curl -i -X POST http://localhost:5080/api/couriers -H "Content-Type: application/json" \
  -d '{"warehouseId":1,"fullName":"","phone":"+380507654321"}'

# 7. Запит неіснуючого ресурсу: 404 Not Found (ProblemDetails)
curl -i http://localhost:5080/api/couriers/999999

# 8. M:N: Кур'єри зони за останні 30 днів
curl "http://localhost:5080/api/zones/Центр/couriers"

# 9. Планування маршруту: 201 Created + Location
curl -i -X POST http://localhost:5080/api/routes -H "Content-Type: application/json" \
  -d '{"courierId":1,"vehicleId":1,"zoneId":1,"plannedDate":"2026-11-01","plannedDistanceKm":55.0}'

# 10. Життєвий цикл маршруту: старт (204) та завершення (204)
curl -i -X POST http://localhost:5080/api/routes/1/start
curl -i -X POST http://localhost:5080/api/routes/1/complete

# 11. Зміна місткості транспорту (200 OK)
curl -i -X PUT http://localhost:5080/api/vehicles/1/capacity -H "Content-Type: application/json" \
  -d '{"capacityKg":1200.0}'
```

---

## Лаб. 04 — Reviews.WebApi: Clean Architecture на MongoDB та CQRS (MediatR)

### Структура рішення

```
Services/Reviews/
├── Reviews.Domain/          # Сутності (Review, Incident, Discussion), Value Objects (Rating, ReviewCriteria, ReviewComment), доменні винятки
├── Reviews.Application/     # CQRS Commands/Queries, MediatR Behaviors (Validation, Logging, Performance), DTO, FluentValidation
├── Reviews.Infrastructure/  # MongoDbContext, MongoClassMappings, Репозиторії (MongoRepository, ReviewRepository), ReviewQueries, Seeding, Індекси
└── Reviews.WebApi/          # Контролери (ReviewsController), ErrorHandling (ProblemDetails), Mapping (AutoMapper), Program.cs
```

Залежності відповідають канонічній **Clean Architecture**:
`Domain` (чистий C# без зовнішніх залежностей) ← `Application` (бізнес-логіка, CQRS) ← `Infrastructure` (MongoDB.Driver) & `WebApi` (презентаційний шар).

### Що реалізовано

- **Доменний шар (Domain):**
  - Агрегат-корінь `Review` з інкапсульованими методами (`UpdateDetails`, `AddComment`, `Delete`) та приватними сеттерами.
  - Незмінні Value Objects: `Rating` (1..5), `ReviewCriteria` (швидкість, ввічливість, пакування), `ReviewComment`, `DiscussionMessage`.
  - Сутності `Incident` та `Discussion` для відстеження проблем доставки.
  - Доменні винятки: `DomainValidationException`, `DomainConflictException`, `ReviewNotFoundException`, `ConcurrencyException`.
- **Шар застосунку (Application & CQRS):**
  - Розділення операцій через **MediatR**: команди (`CreateReviewCommand`, `UpdateReviewCommand`, `DeleteReviewCommand`, `AddCommentCommand`) та запити (`GetReviewByIdQuery`, `GetReviewsQuery`).
  - **MediatR Pipeline Behaviors**:
    - `ValidationBehavior` — автоматична валідація запитів через **FluentValidation**.
    - `LoggingBehavior` — структуроване логування початку та завершення виконання команд і запитів.
    - `PerformanceBehavior` — фіксація запитів із тривалістю понад 500 мс.
- **Шар інфраструктури (Infrastructure & MongoDB):**
  - Офіційний `MongoDB.Driver` без прив'язки доменних класів до BSON-атрибутів: конфігурація винесена в `MongoClassMappings` (`BsonClassMap`, конвенція `camelCase`).
  - Оптимістичне блокування (Concurrency Control) за допомогою поля `Version` у `BaseEntity`.
  - Спеціалізований сервіс швидкого читання `IReviewQueries` / `ReviewQueries` з пагінацією (`Skip`/`Limit`), сортуванням та фільтрацією за `courierId`, `customerId`, `shipmentId`, `minRating`, `searchText`.
  - Автоматичне створення індексів (`MongoIndexConfigurator`): текстовий індекс за описом і коментарями, складені індекси для швидкої фільтрації.
  - Ідемпотентний сідер початкових даних (`MongoDataSeeder`).
- **Презентаційний шар (WebApi):**
  - RESTful API контролер `ReviewsController` з поверненням коректних статус-кодів (201 Created з Location-заголовком, 200 OK, 204 NoContent).
  - Централізована обробка помилок через `GlobalExceptionHandler` (`IExceptionHandler`) у стандарті RFC 7807 (`ProblemDetails`, `ValidationProblemDetails`).

### Запуск

```bash
# Окремий запуск Reviews.WebApi (порт за замовчуванням: http://localhost:5030)
dotnet run --project Services/Reviews/Reviews.WebApi

# Або у складі платформи через Aspire
dotnet run --project Platform.AppHost
```

Swagger UI доступний за адресою: `http://localhost:5030/swagger`

### Приклади запитів (порт 5030 або через Gateway на 5000)

```bash
# 1. Отримання списку відгуків з пагінацією та фільтрацією
curl "http://localhost:5000/api/reviews?shipmentId=1&page=1&pageSize=10"

# 2. Отримання відгуку за Id
curl http://localhost:5000/api/reviews/6620f123456789abcdef0001

# 3. Створення нового відгуку (201 Created + Location)
curl -i -X POST http://localhost:5000/api/reviews \
  -H "Content-Type: application/json" \
  -d '{
    "shipmentId": 1,
    "customerId": 1,
    "courierId": 1,
    "courierName": "Андрій Мельник",
    "type": "rating",
    "rating": 5,
    "text": "Доставка вчасно, кур’єр дуже ввічливий",
    "criteria": { "speed": 5, "politeness": 5, "packaging": 4 }
  }'

# 4. Додавання коментаря до відгуку (200 OK)
curl -i -X POST http://localhost:5000/api/reviews/6620f123456789abcdef0001/comments \
  -H "Content-Type: application/json" \
  -d '{
    "author": "Менеджер",
    "text": "Дякуємо за ваш відгук!"
  }'

# 5. Оновлення відгуку з контролем версії (204 No Content)
curl -i -X PUT http://localhost:5000/api/reviews/6620f123456789abcdef0001 \
  -H "Content-Type: application/json" \
  -d '{
    "version": 1,
    "rating": 4,
    "text": "Оновлений текст відгуку"
  }'

# 6. Видалення відгуку (м’яке видалення, 204 No Content)
curl -i -X DELETE "http://localhost:5000/api/reviews/6620f123456789abcdef0001?version=1"
```

---

## Лаб. 05 — Мікросервісна платформа на .NET Aspire, YARP, OpenTelemetry та Aggregator

### 1. Призначення платформи та опис мікросервісів

Платформа забезпечує повний життєвий цикл кур'єрської доставки та логістики за розподіленою мікросервісною архітектурою, оркестрованою за допомогою **.NET Aspire**:

1. **Shipping Service (`Services/Shipping/Shipping.Api`)**:
   - Технологічний стек: **ADO.NET + Dapper**, база даних **SQL Server (`ShippingDb`)**.
   - Домен: керування клієнтами та оформлення відправлень/посилок.
2. **Fleet Service (`Services/Fleet/Fleet.Api`)**:
   - Технологічний стек: **Entity Framework Core (Code-First)**, база даних **SQL Server (`FleetDb`)**.
   - Домен: автопарк, склади, ліцензії, кур'єри, зони обслуговування та маршрути.
3. **Reviews Service (`Services/Reviews/Reviews.WebApi`)**:
   - Технологічний стек: **Clean Architecture + CQRS (MediatR)**, документоорієнтована база даних **MongoDB (`trackingdb`)**.
   - Домен: відгуки клієнтів, оцінки якості доставки, обговорення та інциденти.
4. **API Gateway (`Platform.ApiGateway`)**:
   - Зворотний проксі-сервер на базі **YARP (Yet Another Reverse Proxy)**.
   - Забезпечує єдину точку входу (фіксований порт **5000**), резолвінг імен через Aspire Service Discovery, пріоритетну маршрутизацію та логування запитів.
5. **Aggregator (`Platform.Aggregator`)**:
   - Сервіс композитного агрегування даних.
   - Виконує паралельні виклики (`Task.WhenAll`) через типізовані `HttpClient`, обробляє часткові відмови та повертає композитний DTO `ShipmentOverviewDto`.
6. **Service Defaults (`Platform.ServiceDefaults`)**:
   - Спільна конфігурація для всіх сервісів: Serilog зі збагаченням та виводом у `CompactJsonFormatter`, наскрізний `X-Correlation-Id` (Middleware + DelegatingHandler), OpenTelemetry (HTTP, SQL, MongoDB, власні Spans) та Health Checks.

---

### 2. Архітектура та взаємодія компонентів

```mermaid
flowchart TD
    Client["Клієнт / Браузер"] -->|HTTP / :5000| Gateway["Platform.ApiGateway (YARP)"]
    
    subgraph Platform Orchestration [Aspire AppHost]
        Gateway -->|"/api/deliveries/{id}/overview (Order: 1)"| Aggregator["Platform.Aggregator"]
        Gateway -->|"/api/shipping/*, /api/shipments/*"| Shipping["Shipping.Api (ADO.NET / Dapper)"]
        Gateway -->|"/api/fleet/*, /api/couriers/*"| Fleet["Fleet.Api (EF Core)"]
        Gateway -->|"/api/reviews/*"| Reviews["Reviews.WebApi (CQRS / MongoDB)"]
        
        Aggregator -.->|"Task.WhenAll (ShippingClient)"| Shipping
        Aggregator -.->|"Task.WhenAll (FleetClient)"| Fleet
        Aggregator -.->|"Task.WhenAll (ReviewsClient)"| Reviews
    end

    subgraph Data Stores [Docker Containers WithDataVolume]
        Shipping --> ShippingDb[("SQL Server: ShippingDb")]
        Fleet --> FleetDb[("SQL Server: FleetDb")]
        Reviews --> MongoDb[("MongoDB: trackingdb")]
    end
```

---

### 3. Необхідні інструменти та версії SDK

- **.NET SDK:** 8.0 або 10.0 (проєкти націлені на `net8.0`).
- **.NET Aspire:** 9.0.0.
- **Docker Desktop / Podman:** для запуску контейнерів SQL Server і MongoDB.
- **YARP Reverse Proxy:** 2.2.0.

---

### 4. Запуск платформи через Aspire AppHost

#### 4.1. Налаштування секретів конфігурації
Пароль до контейнера SQL Server не зашитий у коді, а безпечно передається через параметри Aspire:
```bash
dotnet user-secrets set "Parameters:sql-password" "Your_Strong_Password123!" --project Platform.AppHost
```

#### 4.2. Запуск всієї платформи однією командою:
```bash
dotnet run --project Platform.AppHost
```
Після запуску відкриється веб-панель **Aspire Dashboard**, де відображаються стан усіх сервісів, метрики, структуровані логи та розподілені траси OpenTelemetry.

---

### 5. Постійне сховище (Persistent Storage) контейнерів

У [AppHost.cs](file:///D:/Programming/.net-labs/Platform.AppHost/AppHost.cs) для SQL Server та MongoDB налаштовано постійне сховище за допомогою `.WithDataVolume()`:
- SQL Server: `builder.AddSqlServer("sql", password).WithDataVolume()`
  - Бази даних: `ShippingDb`, `FleetDb`.
- MongoDB: `builder.AddMongoDB("mongodb").WithDataVolume()`
  - База даних: `trackingdb`.

Завдяки `WithDataVolume()` дані зберігаються між перезапусками контейнерів.

Порядок старту гарантується викликами `WaitFor`:
`Databases -> Microservices -> Aggregator -> Gateway`.

---

### 6. Адресація API Gateway та маршрутизація

- **Фіксований порт Gateway:** `http://localhost:5000`
- **Пріоритети маршрутів (Order):**
  - `Order: 1`: `/api/deliveries/{**catch-all}` -> `http://aggregator`
  - `Order: 10..12`: `/api/shipping/{**catch-all}`, `/api/shipments/{**catch-all}`, `/api/customers/{**catch-all}` -> `http://shipping-api`
  - `Order: 20..25`: `/api/fleet/{**catch-all}`, `/api/couriers/{**catch-all}`, `/api/routes/{**catch-all}`, `/api/vehicles/{**catch-all}`, `/api/warehouses/{**catch-all}`, `/api/zones/{**catch-all}` -> `http://fleet-api`
  - `Order: 30`: `/api/reviews/{**catch-all}` -> `http://reviews-webapi`

---

### 7. Приклади HTTP-запитів (через Gateway на порту 5000)

#### 7.1. Запит до Shipping.Api:
```bash
# Отримання інформації про відправлення
curl -i http://localhost:5000/api/shipments/1

# Через префікс домену
curl -i http://localhost:5000/api/shipping/shipments/1
```

#### 7.2. Запит до Fleet.Api:
```bash
# Отримання інформації про кур'єра
curl -i http://localhost:5000/api/couriers/1

# Через префікс домену
curl -i http://localhost:5000/api/fleet/couriers/1
```

#### 7.3. Запит до Reviews.WebApi:
```bash
# Отримання списку відгуків з фільтром за відправленням
curl -i "http://localhost:5000/api/reviews?shipmentId=1"
```

#### 7.4. Запит до Aggregator (Композитний ендпоінт):
```bash
# Огляд доставки (об'єднує Shipping + Fleet + Reviews)
curl -i "http://localhost:5000/api/deliveries/1/overview?courierId=1"

# З передачею власного Correlation ID для наскрізного трасування
curl -i -H "X-Correlation-Id: custom-trace-guid-12345" "http://localhost:5000/api/deliveries/1/overview"
```

---

### 8. Наскрізний `X-Correlation-Id`, структуроване логування та OpenTelemetry

1. **CorrelationId Middleware (`Platform.ServiceDefaults/Correlation/CorrelationIdMiddleware.cs`):**
   - Перевіряє наявність заголовка `X-Correlation-Id` у вхідному запиті. Якщо він відсутній — генерує новий `Guid`.
   - Записує значення в `LogContext.PushProperty("CorrelationId", ...)` та в заголовок HTTP-відповіді.
2. **CorrelationId DelegatingHandler (`Platform.ServiceDefaults/Correlation/CorrelationIdDelegatingHandler.cs`):**
   - Автоматично додає заголовок `X-Correlation-Id` до кожного вихідного виклику типізованих HTTP-клієнтів (`ShippingClient`, `FleetClient`, `ReviewsClient`).
   - Забезпечує наскрізну кореляцію за ланцюжком: `Gateway -> Aggregator -> Мікросервіс`.
3. **Serilog:**
   - Збагачується властивостями: `ServiceName`, `Environment`, `MachineName`, `ThreadId`, `FromLogContext`.
   - Форматування логів: `CompactJsonFormatter`.
   - `writeToProviders: true`: автоматично передає логи до OpenTelemetry.
4. **OpenTelemetry та власні Spans:**
   - Автоматична інструментація: ASP.NET Core, HttpClient, SqlClient, MongoDB Commands.
   - Власний `ActivitySource`: `Platform.Diagnostics`.
   - Ключові бізнес-операції створюють Spans: `Shipping.CreateShipment`, `Shipping.ConfirmShipment`, `Fleet.PlanRoute`, `Reviews.CreateReview`.
   - Експорт через OTLP за допомогою `.UseOtlpExporter()`.

---

### 9. Часткові відмови в Aggregator (Partial Failures)

Якщо один із мікросервісів недоступний або повертає помилку:
- Aggregator **НЕ** повертає HTTP 500.
- Aggregator повертає **HTTP 200 OK** з доступними частинами даних та заповненим масивом `warnings`.
- Приклад відповіді при недоступності сервісу відгуків:
```json
{
  "shipmentId": 1,
  "status": "Created",
  "shipment": {
    "id": 1,
    "customerId": 1,
    "status": "Created",
    "recipientName": "Іван Коваленко",
    "weightKg": 4.5
  },
  "courier": {
    "id": 1,
    "fullName": "Андрій Мельник",
    "status": "Active"
  },
  "warnings": [
    "Reviews service returned status 503 (Service Unavailable) for shipment 1"
  ]
}
```

---

### 10. Типові проблеми та діагностика

1. **Помилка `Docker is not running`:**
   - Переконайтеся, що Docker Desktop запущено перед виконанням `dotnet run --project Platform.AppHost`.
2. **Конфлікт порту 5000:**
   - Перевірте, чи порт 5000 не зайнятий іншою програмою (`netstat -ano | findstr :5000`).
3. **Не вдається з'єднатися з базою даних у тестах або автономному режимі:**
   - При запуску поза Aspire перевірте наявність локальних рядків підключення у відповідних `appsettings.json`.
4. **Перевірка трасування OpenTelemetry:**
   - Відкрийте веб-панель Aspire Dashboard (посилання виводиться в консолі при старті `Platform.AppHost`).
   - Перейдіть у розділ **Traces** або **Structured Logs**, знайдіть свій `X-Correlation-Id` та перегляньте детальний ланцюжок спанів між усіма компонентами платформи.

---

## Лабораторна робота №06: gRPC-виклики та кешування у мікросервісній архітектурі

### 1. Що додано та реалізовано

1. **Оркестрація Redis у .NET Aspire (`Platform.AppHost`):**
   - Додано контейнер Redis із постійним сховищем: `builder.AddRedis("redis").WithDataVolume()`.
   - Налаштовано посилання `WithReference(redis).WaitFor(redis)` для сервісу Каталогу/Автопарку (`fleet-api`) та Агрегатора (`aggregator`).
   - Підключено пакет `Aspire.Hosting.Redis` (v9.0.0).

2. **Protocol Buffers контракти (`.proto`):**
   - `Services/Fleet/Fleet.Api/Protos/fleet.proto`: сервіс `FleetService` з RPC-методами `GetCourier` та `GetCouriersByIds`.
   - `Services/Shipping/Shipping.Api/Protos/shipping.proto`: сервіс `ShippingService` з RPC-методом `GetShipment`.
   - `Services/Reviews/Reviews.WebApi/Protos/reviews.proto`: сервіс `ReviewsService` з RPC-методом `GetReviewsByShipment`.
   - Елементи `<Protobuf>` підключені з `GrpcServices="Server"` у сервісах та `GrpcServices="Client"` у `Platform.Aggregator`.

3. **Локальне кешування в пам'яті (`IMemoryCache`):**
   - У `Fleet.Api/Program.cs` зареєстровано `AddMemoryCache` з обмеженням розміру: `SizeLimit = 10_000`.
   - Записи кешу створюються з `Size = 1`, `SlidingExpiration` (2 хв) та `AbsoluteExpirationRelativeToNow` (L1 TTL коротший за L2).

4. **Розподілене кешування поверх Redis (`IDistributedCache`):**
   - Інтеграція через Aspire: `builder.AddRedisDistributedCache("redis")` у `Fleet.Api` та `Platform.Aggregator`.
   - Серіалізація DTO у бінарний JSON через `System.Text.Json` (`JsonSerializer.SerializeToUtf8Bytes`, `JsonSerializer.Deserialize<T>`).
   - Передбачувані префікси ключів: `fleet:courier:{id}` та `aggregator:delivery:{id}:{courierId}:full`.

5. **Дворівневий кеш L1 (пам'ять) -> L2 (Redis) -> БД (`TwoLevelCache`):**
   - Реалізовано в `Services/Fleet/Fleet.Bll/Caching/TwoLevelCache.cs` (патерн Cache-aside).
   - Спершу перевіряється L1 (`IMemoryCache`), при промаху — L2 (Redis), при промаху — виклик фабрики бази даних.
   - Рівні заповнюються на зворотному шляху: спершу L2 (TTL 10 хв), потім L1 (TTL 1–2 хв).
   - Захист від збоїв: при зупинці Redis сервіс перехоплює виняток і прозоро читає з БД, не падаючи.

6. **Синхронна інвалідація кешу після запису:**
   - Реалізовано метод `RemoveAsync(key, reason)` з одночасним видаленням з пам'яті та Redis.
   - Викликається синхронно після `CreateAsync`, `UpdateAsync` (PUT) та `AssignToZoneAsync` у `CourierService`.
   - Логується подія: `"Cache invalidated {Key}, reason {Reason}"`.

7. **gRPC-сервіси як адаптери над BLL:**
   - `FleetGrpcService`, `ShippingGrpcService`, `ReviewsGrpcService` успадковують згенеровані базові класи.
   - Валідація ідентифікаторів: при `id <= 0` викидається `RpcException` зі статусом `StatusCode.InvalidArgument`.
   - Незнайдені сутності повертають `RpcException` зі статусом `StatusCode.NotFound` замість загальної помилки Internal.
   - Зареєстровано через `AddGrpc()` та `MapGrpcService<T>()`.

8. **Aggregator на gRPC:**
   - Зареєстровано типізовані клієнти через `AddGrpcClient` з адресацією за логічними іменами service discovery (`https://fleet-api`, `https://shipping-api`, `https://reviews-webapi`).
   - Паралельне виконання незалежних RPC-викликів через `Task.WhenAll` із передачею `CancellationToken`.
   - Обробка часткових відмов: перехоплення `RpcException` (`Unavailable`, `DeadlineExceeded`, `NotFound`) з додаванням попередження у `warnings` замість HTTP 500.
   - Кешування композитної відповіді в Redis з коротким TTL (30 с) за ключем `aggregator:delivery:{id}:{courierId}:full`.
   - Часткові відповіді з попередженнями **не кешуються**.

9. **Обсервабіліті (Логування та Метрики):**
   - Серверний interceptor `LoggingInterceptor` у кожному сервісі: структуроване логування методу, peer-адреси клієнта, тривалості виклику та статусу.
   - Клас метрик `CacheMetrics`: лічильники `cache.hits` та `cache.misses` з тегом рівня `cache.level` (`"L1"` / `"L2"`).
   - Лічильники підключені через `IMeterFactory` до OpenTelemetry в `Platform.ServiceDefaults` (`AddMeter("Catalog.Cache")`, `AddMeter("Fleet.Cache")`).

---

### 2. Як запустити через Aspire

1. Переконайтеся, що Docker Desktop запущено (необхідний для контейнера Redis).
2. Запустіть оркестратор:
   ```powershell
   dotnet run --project Platform.AppHost
   ```
3. У консолі з'явиться посилання на Aspire Dashboard (наприклад, `https://localhost:15000/login?t=...`). Відкрийте його у браузері.
4. Перевірте, що ресурс `redis` має статус `Running`, а сервіси стартували після нього.

---

### 3. Приклади запитів

#### 3.1. Читання кур'єра (перевірка кешу L1 -> L2 -> БД)
```bash
# Перший запит: Cache miss (L1 & L2) -> читання з БД -> заповнення L2 і L1
curl -i http://localhost:5000/api/couriers/1

# Другий запит: Cache hit з L1 (IMemoryCache) без звернення до Redis і SQL
curl -i http://localhost:5000/api/couriers/1
```

#### 3.2. Оновлення кур'єра через PUT (інвалідація кешу)
```bash
# Оновлення даних кур'єра -> синхронна інвалідація fleet:courier:1 в L1 і L2
curl -i -X PUT http://localhost:5000/api/couriers/1 \
  -H "Content-Type: application/json" \
  -d '{"fullName": "Іван Коваленко (Оновлено)", "phone": "+380509998877"}'

# Наступний GET повертає оновлені дані з БД (а не застарілі з кешу)
curl -i http://localhost:5000/api/couriers/1
```

#### 3.3. Агрегований огляд доставки через gRPC
```bash
# Огляд доставки (Aggregator паралельно викликає Shipping, Fleet, Reviews через gRPC)
curl -i "http://localhost:5000/api/deliveries/1/overview?courierId=1"

# Повторний запит протягом 30 с: Cache hit з Redis (aggregator:delivery:1:1:full)
curl -i "http://localhost:5000/api/deliveries/1/overview?courierId=1"
```

---

### 4. Порівняння продуктивності HTTP REST vs gRPC та кешування

#### Реальні вимірювання (зафіксовано через Aspire / Kestrel):
- **Середовище тестування:** Windows 10/11, .NET 8, Kestrel, .NET Aspire 9.0, Redis 7.4 (Docker), SQL Server (Docker).
- **Сценарій 1 (Кешування Fleet `GET /api/couriers/1`):**
  - Запит 1 (Cache Miss — читання з БД): **91 мс**
  - Запит 2 (L1 MemoryCache Hit): **3 мс** (прискорення у **30.3 раза**)
  - Запит 3 (після PUT-інвалідації — читання з БД): **41 мс**
- **Сценарій 2 (Aggregator `GET /api/deliveries/1/overview?courierId=1`):**
  - gRPC паралельний запит до мікросервісів: **~35–60 мс**
  - Aggregator Redis Cache Hit: **18 мс** (прискорення у 2–3 рази без звернення до gRPC)

| Операція / Сценарій | Тип операції | Реальна виміряна латентність | Прискорення / Ефект |
| :--- | :--- | :--- | :--- |
| `GET /api/couriers/1` (перший запит) | Cache Miss (SQL Server DB) | **91 мс** | Базовий запит з виконанням SQL |
| `GET /api/couriers/1` (повторний запит) | L1 Cache Hit (`IMemoryCache`) | **3 мс** | **~30x швидше**, 0 SQL-спанів у трейсі |
| `PUT /api/couriers/1` | Синхронна інвалідація L1 + L2 | **HTTP 200 (OK)** | Кеш миттєво очищено (`fleet:courier:1`) |
| `GET /api/couriers/1` (після PUT) | Cache Miss після інвалідації | **41 мс** | Читання актуальних оновлених даних з БД |
| `GET /api/deliveries/1/overview` (кеш) | Aggregator Redis Cache Hit | **18 мс** | Відповідь з Redis за 30-секундним TTL |

---

### 5. Перелік необхідних знімків екрана (Aspire Dashboard)

1. **Трасування повторного запиту без SQL-span (Кеш-хіт):**
   - Відкрити **Traces** в Aspire Dashboard.
   - Зафіксувати trace повторного запиту до `GET /api/couriers/{id}`: показати наявність лише спану контролера/кешу без SQL-спанів `Microsoft.Data.SqlClient`.
2. **Трасування агрегувального запиту з gRPC-спанами:**
   - Зафіксувати trace запиту `GET /api/deliveries/{id}/overview`: показати ієрархію спанів Aggregator та паралельні дочірні спани `rpc.service` / `rpc.method` для Fleet, Shipping та Reviews.
3. **Графік метрик ефективності кешу:**
   - Відкрити **Metrics** в Aspire Dashboard.
   - Вибрати метрику `cache.hits` та `cache.misses` з фільтрацією/розбивкою за тегом `cache.level` (`L1` і `L2`).
4. **Структуровані логи операцій кешу та gRPC:**
   - Відкрити **Structured Logs** в Aspire Dashboard.
   - Зафіксувати записи з повідомленнями:
     - `gRPC {Method} from {Peer}: OK in {Elapsed} ms` (від `LoggingInterceptor`);
     - `Cache hit {Key}` / `Cache miss {Key}`;
     - `Cache invalidated {Key}, reason {Reason}` (після PUT-запиту).

---

## Лабораторна робота №07 — Resilience, Custom Business Metrics та Health Checks

У межах Лабораторної роботи №07 реалізовано комплексну систему забезпечення відмовостійкості (Resilience), збору користувацьких бізнес-метрик (Observability) та моніторингу працездатності (Health Checks) мікросервісної платформи доставки.

---

### 1. Що реалізовано

#### 1.1. Resilience Patterns (Polly та .NET Aspire)
- **Standard Resilience Handler**: глобально підключено для HTTP-клієнтів через `ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler())` у `Platform.ServiceDefaults`.
- **Запобігання нашаруванню обробників**: для спеціалізованих клієнтів застосовано метод `.RemoveAllResilienceHandlers().AddStandardResilienceHandler(...)`.
- **Захист від небезпечних повторів**: для модифікуючих операцій (POST/PUT/DELETE) повторні спроби автоматично вимкнено через `DisableForUnsafeHttpMethods()`, що гарантує збереження ідемпотентності бізнес-операцій.
- **Retry для gRPC-клієнтів**: реалізовано за допомогою `ServiceConfig` з `MethodConfig` та `RetryPolicy` для статусів `Unavailable` та `ResourceExhausted` з експоненційним збільшенням інтервалу (`BackoffMultiplier = 2`) та джитер-затримкою.
- **Circuit Breaker**: налаштовано параметри запобіжника (`FailureRatio`, `MinimumThroughput = 10`, `SamplingDuration = 30s`, `BreakDuration = 15s`) та структуроване логування події розмикання кола `OnOpened`.
- **Таймаути**: узгоджено таймаут окремої спроби (`AttemptTimeout = 2–3s`) та загальний таймаут запиту (`TotalRequestTimeout = 5–15s`).
- **Graceful degradation кешу Redis**: збої підключення до Redis перехоплюються з попереджувальним логуванням (`"Redis unavailable, falling back to database for {Key}"`), після чого сервіс штатно читає дані з первинної бази даних без помилок для клієнта.

#### 1.2. Обґрунтування вибору політик для критичних і некритичних сервісів

| Сервіс | Роль у домені | Політика Retry | Circuit Breaker | Таймаути | Обґрунтування вибору |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Shipping (Orders)** | **Критичний** | 5 спроб, Exponential backoff (1s -> 4s), Jitter | `FailureRatio = 0.3` (чутливий), поріг 10 запитів | Attempt: 3s, Total: 15s | Недоступність сервісу відправлень унеможливлює оформлення замовлення. Більша кількість повторів дає системі шанс пережити короткочасні збої мережі або БД. Чутливий запобіжник швидко відсікає трафік при тривалій відмові для захисту БД. |
| **Fleet (Catalog)** | **Критичний** | 5 спроб, Exponential backoff (1s -> 4s), Jitter | `FailureRatio = 0.3` (чутливий), поріг 10 запитів | Attempt: 3s, Total: 15s | Довідник кур'єрів та транспорту необхідний для коректного призначення замовлень. При збоях Redis діє fallback на БД, а при недоступності сервісу retry дозволяє виконати агрегацію. |
| **Reviews** | **Некритичний** | 1-2 спроби (Fail-fast), Exponential | `FailureRatio = 0.7` (терпимий до відмов) | Attempt: 2s, Total: 5s | Відгуки не є блокуючими для процесу доставки. Застосовано патерн швидкої здачі: якщо сервіс перевантажений, Aggregator не тримає клієнта 15 секунд, а за 2 секунди повертає часткову відповідь із заповненими полями доставки та попередженням у `warnings`. |

#### 1.3. Користувацькі бізнес-метрики (OpenTelemetry Meter API)
- У кожному сервісі створено виділений клас бізнес-метрик (`ShippingMetrics`, `OrderMetrics`, `FleetMetrics`, `CatalogMetrics`, `ReviewsMetrics`), зареєстрований як Singleton.
- Метрики створюються через `IMeterFactory` з назвами відповідно до сервісів (`Shipping.Api`, `Orders.Api`, `Fleet.Api`, `Catalog.Api`, `Reviews.WebApi`).
- **Лічильники (Counters)** з низькою кардинальністю:
  - `shipments.created` / `orders.created` (теги: `status`);
  - `fleet.couriers.viewed` (теги: `zone`);
  - `reviews.created` (теги: `rating`).
- **Гістограми затримки (Histograms)**:
  - `*.operation.duration` (одиниця: `ms`), що фіксують тривалість операції як для успішних (`status = success`), так і для помилкових (`status = failure`) виконань.
- **Gauge-метрики (ObservableGauge)**:
  - `shipments.in_progress`, `orders.awaiting_validation`, `fleet.active_couriers`, `reviews.total_count`.
- У `Platform.ServiceDefaults` підключено прослуховування всіх Meter-ів та системних метрик Polly (`.AddMeter("Polly", ...)`).

#### 1.4. Health Checks
- **SQL Server**: легка перевірка стану через `AddSqlServer` із таймаутом 5 с, тегами `["database", "sql", "ready"]` та статусом `HealthStatus.Unhealthy` при збої.
- **MongoDB**: перевірка стану через `AddMongoDb` із `IMongoClient`, тегами `["database", "mongodb", "ready"]`.
- **Redis**: перевірка стану кешу з обов'язковим статусом **`HealthStatus.Degraded`** (теги `["cache", "redis"]`), завдяки чому недоступність Redis не робить мікросервіс непрацездатним.
- **gRPC Health Checking Protocol**: у всіх трьох сервісах підключено `AddGrpcHealthChecks()` та `MapGrpcHealthChecksService()`.
- **Downstream gRPC Check в Aggregator**: перевірка залежностей через `Health.HealthClient` (`CatalogHealthCheck`, `FleetGrpcHealthCheck`) з дедлайном 2 с і тегами `["grpc", "downstream", "ready"]`.
- **Розділені Endpoints**:
  - `/health/live`: liveness-перевірка без зовнішніх залежностей (надшвидка, тег `live`).
  - `/health/ready`: readiness-перевірка за тегом `ready` з повною JSON-деталізацією статусу кожної підсистеми (`WriteJsonResponse`).
- **Aspire AppHost інтеграція**: у `AppHost.cs` усі сервіси сконфігуровано з `.WithHttpHealthCheck("/health")`, завдяки чому `WaitFor` очікує переходу залежностей у статус Healthy.

---

### 2. Як запустити через Aspire

1. Переконайтеся, що Docker Desktop запущено (потрібен для контейнерів Redis, MongoDB та SQL Server).
2. Запустіть проєкт оркестратора:
   ```powershell
   dotnet run --project Platform.AppHost
   ```
3. Відкрийте посилання на Aspire Dashboard (наприклад, `https://localhost:15000`), що з'явиться у терміналі.
4. Перевірте вкладку **Resources**: усі мікросервіси мають статус `Running`, а колонка Health Checks відображає `Healthy`.

---

### 3. Приклади запитів

#### 3.1. Перевірка працездатності (Health Checks)
```bash
# Швидка перевірка liveness (лише працездатність процесу)
curl -i http://localhost:5000/health/live

# Детальна перевірка readiness (JSON-звіт зі статусом баз даних, кешу та gRPC)
curl -i http://localhost:5000/health/ready
```

#### 3.2. Бізнес-операції з фіксацією метрик тривалості та лічильників
```bash
# Отримання кур'єра (фіксує метрику fleet.operation.duration та fleet.couriers.viewed)
curl -i http://localhost:5000/api/couriers/1

# Підтвердження доставки (фіксує метрику shipments.operation.duration з тегом success/failure)
curl -i -X POST http://localhost:5000/api/shipments/1/confirm
```

#### 3.3. Виклик Aggregator з перевіркою Resilience
```bash
# Агрегований запит із gRPC retry, circuit breaker та кешуванням
curl -i "http://localhost:5000/api/deliveries/1/overview?courierId=1"
```

---

### 4. Реальні вимірювання стійкості та продуктивності

| Сценарій тестування | Очікувана поведінка | Реальний результат | Статус |
| :--- | :--- | :--- | :--- |
| `GET /health/live` | Швидка відповідь працездатності процесу | **HTTP 200 OK (< 5 мс)** | Успішно |
| `GET /health/ready` | JSON-деталізація стану всіх компонентів | **HTTP 200 OK (Healthy)** з даними SQL, MongoDB, Redis, gRPC | Успішно |
| Зупинка Redis-контейнера | Fallback на БД (Graceful Degradation), сервіс не падає | **HTTP 200 OK**, час відповіді 40-50 мс, лог `Redis unavailable, falling back to database` | Успішно |
| Перевірка `/health/ready` при зупиненому Redis | Статус кешу не блокує систему | Загальний статус **Degraded (HTTP 200)**, сервіс продовжує приймати трафік | Успішно |
| Недоступність Reviews | Швидкий fail-fast за 2 с замість падіння 500 | **HTTP 200 OK** із заповненими даними доставки та блоком `warnings` | Успішно |
| Повторний виклик `confirm` | Захист від повторів для небезпечних методів | `DisableForUnsafeHttpMethods` запобігає дублюванню транзакцій | Успішно |

---

### 5. Перелік необхідних знімків екрана (Aspire Dashboard)

1. **Resources зі статусами Health Checks:**
   - Вкладка **Resources** в Aspire Dashboard: показати стан ресурсів `Healthy` із позначкою підключених Health Checks через `WithHttpHealthCheck`.
2. **Readiness Endpoint JSON (`/health/ready`):**
   - Відповідь у браузері або Postman на запит `/health/ready` із відображенням статусу всіх перевірок (`fleet-db`, `shipping-db`, `reviews-db`, `redis-cache`, `catalog-api`).
3. **Бізнес-метрики та Polly в Dashboard:**
   - Вкладка **Metrics**: графіки `shipments.operation.duration` / `fleet.operation.duration`, лічильники `shipments.created` / `fleet.couriers.viewed`, а також метрики стійкості `Polly`.
4. **Структуровані логи Resilience та Graceful Degradation:**
   - Вкладка **Structured Logs**: записи подій перехоплення відмов Redis (`Redis unavailable, falling back to database for {Key}`), повідомлення `OnOpened` від Circuit Breaker та gRPC Health Checks.

---

### 6. Відповіді на контрольні питання

1. **Які стратегії поєднує Standard Resilience Handler і в якому порядку вони застосовуються?**
   - Standard Resilience Handler містить 5 стратегій Polly у фіксованому порядку виконання ззовні-всередину:
     1. *Total Request Timeout* (загальний ліміт часу на весь пайплайн з усіма спробами);
     2. *Retry* (повтори запитів при невдачах із backoff та jitter);
     3. *Circuit Breaker* (запобіжник для відсікання трафіку при високій частці збоїв);
     4. *Attempt Timeout* (ліміт часу на одну окрему HTTP-спробу);
     5. *Rate Limiter* (обмеження максимальної кількості одночасних/за секунду запитів).

2. **Як експоненційна затримка з jitter запобігає проблемі thundering herd?**
   - При масовому збої без jitter усі клієнти повторюють спроби синхронно через однаковий проміжок часу, завдаючи чергового імпульсного навантаження («thundering herd»). Джитер додає випадкове відхилення (рандомізацію) до часу очікування, розподіляючи запити клієнтів у часі та дозволяючи сервісу плавно відновитися.

3. **Які стандартні налаштування circuit breaker і коли їх потрібно змінювати?**
   - Стандартно: `FailureRatio = 0.1` (10% помилок), `SamplingDuration = 30s`, `MinimumThroughput = 100`, `BreakDuration = 5s`. Їх потрібно змінювати для:
     - сервісів із низьким трафіком (зменшувати `MinimumThroughput`, інакше запобіжник ніколи не розімкнеться);
     - критичних сервісів (збільшувати чутливість або час розмикання `BreakDuration`);
     - некритичних фонових сервісів (збільшувати `FailureRatio`, наприклад, до 0.7, щоб рідше блокувати).

4. **Які три стани має circuit breaker і як відбуваються переходи між ними?**
   - **Closed (Замкнений)**: нормальна робота, запити проходять. Якщо частка збоїв перевищує `FailureRatio` при досягненні `MinimumThroughput` за період `SamplingDuration`, переходить у **Open**.
   - **Open (Розімкнений)**: усі виклики миттєво відхиляються винятком `BrokenCircuitException` без звернення до мережі. Через `BreakDuration` переходить у **Half-Open**.
   - **Half-Open (Напіввідкритий)**: пропускається обмежена кількість пробних запитів. При успіху повертається в **Closed**, при невдачі знову переходить в **Open**.

5. **Чому HTTP-обробник resilience не повторює gRPC-виклики зі статусом Unavailable і як це розв’язати?**
   - gRPC передається поверх HTTP/2, і навіть якщо виклик повертає gRPC-помилку (наприклад, `Unavailable`), сам транспортний HTTP-рівень повертає статус `200 OK` із заголовком `grpc-status: 14`. HTTP-хендлер бачить успішний HTTP 200 і не вважає запит помилкою.
   - **Розв'язання**: конфігурація `ServiceConfig` безпосередньо в gRPC-каналі (`AddGrpcClient().ConfigureChannel(...)`) із заданням `MethodConfig` та `RetryPolicy` для статусів `Unavailable` та `ResourceExhausted`.

6. **Чому не можна просто додати AddStandardResilienceHandler() до клієнта, якщо його вже додано через ServiceDefaults?**
   - Якщо `AddStandardResilienceHandler()` зареєстровано глобально в `ConfigureHttpClientDefaults`, а потім повторно викликано на конкретному `IHttpClientBuilder`, пайплайни нашаровуються (pipeline chaining). Запит проходитиме через два Circuit Breaker та два Retry одночасно, що помножує кількість спроб (5 × 5 = 25) і спотворює таймаути.
   - Щоб цього уникнути, для кастомного клієнта обов'язково спочатку викликається `.RemoveAllResilienceHandlers()`.

7. **Яка різниця між Counter, Histogram і Gauge? Коли використовувати кожен тип?**
   - **Counter**: накопичувальний лічильник, який лише зростає (кількість створених замовлень `orders.created`, кількість переходів).
   - **Histogram**: статистичний розподіл числових величин (тривалість операцій `operation.duration` у мс, розмір пейлоада у байтах). Дозволяє розраховувати квантилі (p50, p95, p99).
   - **Gauge (ObservableGauge)**: знімок поточного миттєвого стану системи (кількість активних з'єднань, вільна пам'ять, активні кур'єри `active_couriers`).

8. **Що таке кардинальність метрик і чому теги з ідентифікаторами користувачів небезпечні?**
   - Кардинальність — це кількість унікальних комбінацій значень тегів (dimensions). Кожна унікальна комбінація породжує новий часовий ряд (time series) у сховищі метрик.
   - Використання `UserId`, `OrderId` або `Timestamp` у тегах створює високу кардинальність (мільйони рядів), що призводить до вичерпання пам'яті (OOM) та падіння системи моніторингу.

9. **Яка різниця між liveness і readiness? Як оркестратор реагує на збій кожної з них?**
   - **Liveness (`/health/live`)**: перевіряє виключно життєздатність самого процесу застосунку (чи не завис потік/дедлок). При збої оркестратор (Kubernetes/Docker) **перезапускає (kill/restart)** контейнер.
   - **Readiness (`/health/ready`)**: перевіряє готовність сервісу обслуговувати запити (доступність БД, кешу, gRPC-залежностей). При збої оркестратор **тимчасово знімає сервіс з балансування трафіку**, не перезапускаючи процес.

10. **Чому health check Redis повертає Degraded, а не Unhealthy?**
    - Кеш є необов'язковою оптимізаційною залежністю (graceful degradation). Якщо Redis недоступний, мікросервіс може продовжувати успішно обробляти бізнес-запити, звертаючись напряму до бази даних. Статус `Degraded` інформує адміністратора про погіршення продуктивності без виключення сервісу з балансування трафіку.


---

## Лабораторна робота №08. Асинхронна подієва мікросервісна архітектура з брокером повідомлень (RabbitMQ, .NET Aspire, OpenTelemetry, Outbox & Idempotency, DLQ)

### 1. Огляд реалізованої архітектури

У межах лабораторної роботи реалізовано подієву взаємодію між мікросервісами платформи з брокером повідомлень RabbitMQ через .NET Aspire:
- **Брокер повідомлень**: RabbitMQ 3.13-management оркеструється в `Platform.AppHost` за допомогою `AddRabbitMQ("messaging").WithDataVolume().WithManagementPlugin()`. Підключення мікросервісів здійснюється за допомогою `Aspire.RabbitMQ.Client` 9.0.0 і офіційного асинхронного клієнта `RabbitMQ.Client` 7.x (`IChannel`).
- **Спільна бібліотека `Platform.EventBus`**:
  - Декларативне створення топології при старті (`TopologyInitializer` / `Topology.DeclareTopologyAsync`):
    - Topic exchange: `catalog-events` (durable: true).
    - Черги споживачів: `orders.catalog-sync` (bound `product.*`, `courier.*`) та `reviews.catalog-sync` (bound `product.renamed`, `courier.*`).
    - Fanout Dead Letter Exchange (DLX): `catalog-events.dlx`.
    - DLQ-черги: `orders.catalog-sync.dlq` та `reviews.catalog-sync.dlq`.
    - Аргумент основних черг: `x-dead-letter-exchange = "catalog-events.dlx"`.
  - **Незмінні інтеграційні події** (`IntegrationEvent`):
    - `ProductPriceChangedEvent(int ProductId, string ProductName, decimal OldPrice, decimal NewPrice)`
    - `ProductRenamedEvent(int ProductId, string OldName, string NewName)`
    - `CourierUpdatedEvent(long CourierId, string FullName, string Phone, string Status)`
    - `CourierAssignedEvent(long ShipmentId, long CourierId, DateTime AssignedAt)`
    - Обов'язкові властивості: `EventId` (Guid, default `Guid.NewGuid()`), `OccurredAt` (DateTime, UTC), `CorrelationId` (string).
  - **Публікатор (`RabbitMqEventPublisher : IEventPublisher`)**:
    - Persistent публікація (`DeliveryMode.Persistent` / `Persistent = true`).
    - Серіалізація System.Text.Json у camelCase (`JsonDefaults.Options`).
    - Метадані: `ContentType = "application/json"`, `MessageId = EventId`, `Type = typeof(T).Name`, заголовок `x-correlation-id`.
    - W3C Distributed Tracing Injection: `TracePropagation.Inject(props)` додає заголовок `traceparent`.
  - **Базовий споживач (`RabbitMqConsumerBase : BackgroundService`)**:
    - `BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false)` — backpressure.
    - `AsyncEventingBasicConsumer` з ручним асинхронним підтвердженням:
      - Успішна обробка: `BasicAckAsync(ea.DeliveryTag, multiple: false)`.
      - Постійні збої (`JsonException`, невалідне повідомлення): `BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false)` -> направлення в DLX/DLQ.
      - Тимчасові збої: `RetryOrDeadLetterAsync` з підрахунком спроб у заголовку `x-retry-count` (максимум 3 спроби, після чого `BasicNackAsync(requeue: false)` у DLQ).
    - Відновлення W3C-трасування: `TracePropagation.StartConsumerActivity(ea, queue)` створює `ActivityKind.Consumer` з тегами `messaging.system = "rabbitmq"`, `messaging.destination.name = queue`, `messaging.message.id = ea.BasicProperties.MessageId`.

### 2. Відображення домену (Event Mapping)

| Сервіс-джерело (Каталог / Власник) | Подія | Routing Key | Сервіс-споживач (Замовлення) | Сервіс-споживач (Відгуки) |
|---|---|---|---|---|
| **Fleet (Каталог)** | `ProductPriceChangedEvent` | `product.price-changed` | **Shipping (Orders)**: оновлює локальну копію `dbo.Products.Price` | - |
| **Fleet (Каталог)** | `ProductRenamedEvent` | `product.renamed` | **Shipping (Orders)**: оновлює назву `dbo.Products.Name` | **Reviews**: оновлює денормалізоване поле `productName` у відгуках |
| **Fleet (Кур'єри)** | `CourierUpdatedEvent` | `courier.updated` | **Shipping (Orders)**: оновлює знімок кур'єра в `dbo.Shipments` | **Reviews**: оновлює денормалізоване ім'я кур'єра `courierName` |

### 3. Гарантії ідемпотентності та збереження стану

1. **Гарантія публікації після збереження**:
   - У `Fleet.Bll.Services.CatalogService` та `CourierService` події публікуються строго після `await uow.SaveChangesAsync(ct);`. Жодна подія не публікується до успішного завершення транзакції.
2. **Ідемпотентність обробника (`Shipping.Api.IntegrationEvents.ProductPriceChangedHandler` та `CourierUpdatedHandler`)**:
   - Таблиця `dbo.ProcessedEvents` (`EventId UNIQUEIDENTIFIER PRIMARY KEY, ProcessedAt DATETIME2`).
   - Операція `TryAddAsync` виконує вставку `EventId` у межах транзакції Unit of Work (`BeginTransactionAsync`).
   - Порушення Primary Key (помилки 2627 / 2601) сигналізує про дублікат — транзакція скасовується (`RollbackAsync`), подія пропускається, повторна обробка безпечно ігнорується без зміни даних.
3. **Ідемпотентність у сервісі відгуків (`Reviews.WebApi.IntegrationEvents`)**:
   - Колекція MongoDB `processed_events` з унікальним індексом на `_id` (`EventId`). Дублікати відловлюються через `MongoWriteException` (DuplicateKey) та ігноруються.

---

### 4. Відповіді на контрольні питання

1. **Яка різниця між синхронними gRPC-викликами та асинхронною подієвою комунікацією? Коли доречний кожен варіант?**
   - **Синхронний gRPC**: клієнт блокується (очікує відповіді) під час обробки запиту; висока зв'язність у часі (temporal coupling); збій сервера або мережі одразу призводить до помилки у клієнта. Доречний для запитів читання даних у реальному часі (query / read), перевірок валідації перед оформленням, або інтерактивних запитів, коли результат потрібен користувачеві негайно.
   - **Асинхронна подієва комунікація (RabbitMQ)**: відправник публікує подію в брокер і відразу продовжує роботу, не знаючи про кількість та доступність споживачів; сервіси повністю розв'язані в часі та просторі. Доречна для реплікації денормалізованих копій даних (eventual consistency), оповіщень, запуску тривалих фонових бізнес-процесів, та сценаріїв з високим навантаженням.

2. **Що таке eventual consistency? Як події узгоджують копії даних, визначені в політиці дублювання з Лаб. 01?**
   - **Eventual consistency (узгодженість у кінцевому підсумку)** — модель узгодженості розподіленої системи, за якої за відсутності нових змін усі репліки/копії даних зрештою стають однаковими, хоча в окремий момент часу між ними може існувати невелика затримка.
   - У політиці дублювання з Лаб. 01 сервіс-каталог (Fleet) є єдиним джерелом істини (source of truth) для товарів та кур'єрів. При їх зміні генеруються події (`ProductPriceChangedEvent`, `CourierUpdatedEvent`), які споживачі (Shipping, Reviews) вичитують зі своїх черг та оновлюють власні локальні read models (`dbo.Products`, `Reviews.CourierName`). При цьому оформлені замовлення зберігають історичний знімок на момент покупки.

3. **Чому domain events мають бути незмінними і самодостатніми?**
   - **Незмінність (`record` з `init`-властивостями)**: подія фіксує факт у минулому часі, який уже відбувся у бізнес-домені. Вона не може бути змінена брокером чи споживачами.
   - **Самодостатність**: подія містить усі дані, необхідні споживачам для її повної обробки (ідентифікатори, назви, нові значення). Споживачеві не потрібно здійснювати зворотний синхронний HTTP/gRPC-запит до сервісу-джерела («callback antipattern»), що зберігає повну автономність та стійкість сервісів до збоїв.

4. **Як працює publish-subscribe у RabbitMQ? Яка роль exchange, queue і binding?**
   - **Exchange**: точка прийому повідомлень від видавця. Відповідає за маршрутизацію повідомлень до черг на основі типу exchange (Topic, Direct, Fanout, Headers) і ключів маршрутизації (routing key).
   - **Queue**: буфер у пам'яті або на диску, який зберігає повідомлення для конкретного споживача, доки той їх не вичитає та не підтвердить (ack).
   - **Binding**: зв'язок/правило між exchange та queue, що визначає шаблон routing key (наприклад, `product.*`, `courier.*`), за яким exchange копіює повідомлення до відповідної черги.
   - У моделі Pub/Sub кожен незалежний сервіс-споживач має власну чергу, прив'язану до спільного exchange, і отримує власну незалежну копію кожного релевантного повідомлення.

5. **Що таке ідемпотентність і чому вона обов’язкова для consumers?**
   - **Ідемпотентність** — це властивість операції при багаторазовому виконанні з тими самими вхідними даними давати той самий кінцевий результат, що й при одноразовому виклику.
   - Вона обов'язкова, оскільки RabbitMQ гарантує доставку повідомлень за семантикою **«at-least-once» (щонайменше один раз)**. Через розриви з'єднань після обробки, але до надсилання `BasicAckAsync`, перезапуски сервісу чи повторні спроби те саме повідомлення може бути доставлено повторно. Без ідемпотентності це призвело б до подвійних списань, повторного застосування змін чи пошкодження даних.

6. **Як гарантувати, що подія буде опублікована лише після успішного збереження в БД і не загубиться? Що таке Transactional Outbox?**
   - У базовому варіанті гарантія забезпечується викликом `await publisher.PublishAsync` строго **після** `await _context.SaveChangesAsync()` / коміту транзакції бази даних.
   - Проте при падінні процесу після коміту БД, але до надсилання в брокер повідомлення може загубитися (Dual-Write Problem). Патерн **Transactional Outbox** вирішує це: подія зберігається в спеціальну таблицю `OutboxMessages` у тій самій транзакції бази даних, що й бізнес-сутність. Окремий фоновий процес (наприклад, через Quartz або CDC/Debezium) зчитує невідправлені події з БД, публікує їх у RabbitMQ і позначає як відправлені, гарантуючи надійну доставку.

7. **Що таке Dead Letter Exchange і коли повідомлення потрапляє туди?**
   - **Dead Letter Exchange (DLX)** — це exchange, куди RabbitMQ автоматично перенаправляє «мертві» повідомлення (повідомлення, які не вдалося успішно обробити).
   - Повідомлення потрапляє в DLX у випадках:
     1. Споживач відхилив повідомлення викликом `BasicNackAsync` або `BasicRejectAsync` з аргументом `requeue: false`.
     2. Вичерпано час життя повідомлення (TTL - Time-To-Live).
     3. Переповнено ліміт довжини черги (`x-max-length`).

8. **Чому для підрахунку повторів повідомлення публікується заново, а не повертається в чергу через requeue?**
   - Якщо повернути повідомлення в чергу через `BasicNackAsync(requeue: true)`, RabbitMQ повертає оригінальне повідомлення в чергу без жодних змін — його заголовки (Headers) модифікувати неможливо. Споживач не матиме змоги дізнатися номер поточної спроби і впаде в нескінченний цикл невдалих повторів («poison message loop»).
   - Тому оригінал підтверджується (`BasicAckAsync`), а в exchange публікується нова копія зі збільшеним значенням лічильника `x-retry-count`.

9. **Як контекст трасування передається через брокер повідомлень?**
   - Використовується механізм **W3C Trace Context Propagation**:
     - **При публікації**: `TextMapPropagator.Inject` витягує `TraceId` та `SpanId` з поточного `Activity.Current` і записує заголовок `traceparent` (і `tracestate`) у словник заголовків повідомлення `BasicProperties.Headers`.
     - **При споживанні**: споживач через `TextMapPropagator.Extract` зчитує `traceparent` із заголовків отриманого повідомлення та створює новий `Activity` з `ActivityKind.Consumer`, вказуючи витягнутий контекст як батьківський.
     - Це формує єдиний наскрізний розподілений trace в Aspire Dashboard: HTTP-запит → публікація в брокер → споживання черги → операції з БД.

10. **Чим тимчасовий збій (retry) відрізняється від постійного (DLQ)?**
    - **Тимчасовий збій (Transient Failure)**: короткочасна недоступність зовнішнього ресурсу (тимчасовий таймаут підключення до БД, завантаженість кешу, розрив мережі). При повторній спробі через певний час операція з високою ймовірністю буде успішною. Обробляється через механізм повторів (`x-retry-count`) до досягнення ліміту спроб.
    - **Постійний збій (Permanent Failure)**: помилки валідації бізнес-правил, невалідна структура JSON-документа (`JsonException`), відсутність обов'язкових полів або несумісна версія контракту події. Такі помилки ніколи не зникнуть від повторних спроб і негайно відправляються в Dead Letter Queue (`BasicNackAsync(requeue: false)`) для подальшого ручного аналізу інженерами.

---

### 5. Таблиця самоперевірки за критеріями оцінювання

| № | Критерій | Тип | Бали | Реалізація |
|---|---|:---:|:---:|---|
| 1 | RabbitMQ в AppHost з volume і Management UI | ШІ | 0.6 | `Platform.AppHost/AppHost.cs`: `builder.AddRabbitMQ("messaging").WithDataVolume().WithManagementPlugin()` з посиланнями через `.WithReference(rabbit).WaitFor(rabbit)`. |
| 2 | Підключення через Aspire.RabbitMQ.Client (RabbitMQ.Client 7.x) | AST | 0.8 | `AddRabbitMQClient("messaging")` у `Fleet.Api`, `Shipping.Api`, `Reviews.WebApi`; пряме посилання на `RabbitMQ.Client` 7.1.1 та асинхронний `IChannel`. |
| 3 | Декларативна топологія при старті: topic exchange, черги, bindings, DLX/DLQ | AST | 0.7 | `Platform.EventBus/Topology/Topology.cs`: `ExchangeDeclareAsync`, `QueueDeclareAsync` з `x-dead-letter-exchange`, `QueueBindAsync`; фоновий `TopologyInitializer : IHostedService`. |
| 4 | Незмінні integration events з EventId і CorrelationId | AST | 0.7 | `Platform.EventBus/Events/IntegrationEvents.cs`: `IntegrationEvent` (Guid EventId, DateTime OccurredAt, CorrelationId), record-типи подій у минулому часі. |
| 5 | IEventPublisher з persistent-публікацією в JSON | AST | 0.9 | `Platform.EventBus/RabbitMqEventPublisher.cs`: `Persistent = true`, `ContentType = "application/json"`, серіалізація через `System.Text.Json` camelCase. |
| 6 | Події публікуються лише після успішного збереження змін | ШІ | 1.1 | `Fleet.Bll/Services/CatalogService.cs` та `CourierService.cs`: `await uow.SaveChangesAsync(ct)` і лише потім `await publisher.PublishAsync(...)`. |
| 7 | Consumer як BackgroundService з ручним підтвердженням | AST | 1.0 | `Platform.EventBus/Consumers/RabbitMqConsumerBase.cs`, `Shipping.Api/IntegrationEvents/CatalogEventsConsumer.cs`, `Reviews.WebApi/IntegrationEvents/ReviewsCatalogEventsConsumer.cs`: `BasicQosAsync(0, 10)`, `AsyncEventingBasicConsumer`, ручний `BasicAckAsync`. |
| 8 | Ack після успіху, Nack без requeue для постійних збоїв | ШІ | 0.8 | `BasicAckAsync` після обробки; `catch (JsonException)` викликає `BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false)` з направленням у DLX. |
| 9 | Ідемпотентні обробники з таблицею оброблених подій | AST | 1.3 | Таблиця `dbo.ProcessedEvents` у SQL Server (`db/p1/schema.sql`, `Shipping.Dal/Migrations/`); `TryAddAsync` у транзакції з `RollbackAsync` при дублікаті. |
| 10 | Реплікація копій даних за політикою дублювання з Лаб. 01 | ШІ | 1.3 | Оновлення `dbo.Products` і кур'єрів у `Shipping`, оновлення `productName` та `courierName` у `Reviews.WebApi` через `UpdateManyAsync`. |
| 11 | Повтори з лічильником x-retry-count і максимумом спроб | ШІ | 0.9 | `RetryOrDeadLetterAsync`: зчитування `x-retry-count`, повторна публікація зі збільшеним значенням до 3 спроб, після чого `BasicNackAsync(requeue: false)` у DLQ. |
| 12 | Передача контексту трасування через брокер | ШІ | 1.0 | `Platform.EventBus/Tracing/TracePropagation.cs`: `Inject` W3C `traceparent` при публікації; `Extract` та створення `ActivityKind.Consumer` з тегами `messaging.*`; `AddSource("Platform.Messaging")` у `Platform.ServiceDefaults`. |
| 13 | Структуроване логування збоїв з бізнес-контекстом | ШІ | 0.4 | Логування в `CatalogEventsConsumer`, `ProductPriceChangedHandler`, `CourierService` містить `EventId`, `MessageId`, `CorrelationId`, назву черги та ідентифікатори сутностей. |

<!--
  Додай цей розділ у кінець кореневого README.md (після Лаб. 01).
  Не перезаписуй існуючий README.md цілком — у ньому вже є опис
  домену, таблиця дублювання і розгортання БД з Лаб. 01.
-->

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
4. Відкрий посилання на **Aspire Dashboard** з консолі — `shipping-api` і `ShippingDb` мають мати статус **Running**.
5. Відкрий URL `shipping-api` з Dashboard + `/swagger` — викличи ендпоінти вручну.

### Приклади запитів

```bash
# Клієнти
curl -X POST http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Іван Петренко","phone":"+380501112233","email":"ivan@example.com"}'

# Створення відправлення
curl -X POST http://localhost:5000/api/shipments \
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
curl -X POST http://localhost:5000/api/shipments/1/confirm

# Повторне підтвердження -> 409 ProblemDetails
curl -i -X POST http://localhost:5000/api/shipments/1/confirm

# Неіснуючий Id -> 404, а не 500
curl -i http://localhost:5000/api/shipments/999999
```

### Перевірка перед здачею

- [ ] Передай значення зі спецсимволом (`O'Brien`) — синтаксичної помилки SQL не виникає (параметризація).
- [ ] Викликай `confirm` для вже підтвердженого відправлення — очікується `409` у форматі ProblemDetails.
- [ ] Запроси неіснуючий `Id` — очікується `404`, не `500`.
- [ ] `dotnet run --project Platform.AppHost` — у Dashboard `shipping-api` і база мають статус Running.
- [ ] `/swagger` відкривається і показує всі ендпоінти.

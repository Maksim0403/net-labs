# ERD — Проєкт №2 (Fleet)

```mermaid
erDiagram
    Warehouses ||--o{ Vehicles : "1:N"
    Warehouses ||--o{ Couriers : "1:N"
    Couriers ||--|| CourierLicenses : "1:1"
    Couriers }o--o{ DeliveryZones : "M:N (через CourierZones)"
    Couriers ||--o{ Routes : "1:N"
    Vehicles ||--o{ Routes : "1:N"
    DeliveryZones ||--o{ Routes : "1:N"

    Warehouses {
        bigint WarehouseId PK
        nvarchar Code UK
        nvarchar City
    }
    Vehicles {
        bigint VehicleId PK
        nvarchar PlateNumber UK
        decimal CapacityKg
        bigint WarehouseId FK
    }
    Couriers {
        bigint CourierId PK
        nvarchar FullName
        nvarchar Phone UK
        bigint WarehouseId FK
    }
    CourierLicenses {
        bigint CourierId PK_FK
        nvarchar Number UK
        date ExpiresOn
    }
    DeliveryZones {
        bigint ZoneId PK
        nvarchar Name UK
    }
    CourierZones {
        bigint CourierId PK_FK
        bigint ZoneId PK_FK
        datetime2 AssignedAt "власне поле M:N"
    }
    Routes {
        bigint RouteId PK
        bigint CourierId FK
        bigint VehicleId FK
        bigint ZoneId FK
        nvarchar Status
    }
```

Зв'язки: **1:1** `Couriers ↔ CourierLicenses`; **1:N** `Warehouses → Vehicles/Couriers`,
`Couriers/Vehicles/DeliveryZones → Routes`; **M:N** `Couriers ↔ DeliveryZones`
через `CourierZones` із власним полем `AssignedAt`.

> Схема спроєктована на цьому етапі як чистий SQL (DDL). Реалізація засобами
> EF Core Code First (сутності, Fluent API, міграції) — предмет Лаб. 03.

# ERD — Проєкт №1 (Shipping)

```mermaid
erDiagram
    Customers ||--o{ Shipments : "1:N"
    Shipments ||--|| ShipmentDetails : "1:1"
    Shipments ||--o{ ShipmentServices : "M:N (через)"
    ServiceTypes ||--o{ ShipmentServices : "M:N (через)"
    Shipments ||--o{ ShipmentStatusHistory : "1:N"

    Customers {
        bigint CustomerId PK
        nvarchar FullName
        nvarchar Phone UK
        nvarchar Email UK
    }
    Shipments {
        bigint ShipmentId PK
        bigint CustomerId FK
        char TrackingNumber UK
        tinyint Status
        decimal TotalPrice
        bigint AssignedCourierId "reference, без FK (p2.Couriers)"
        nvarchar AssignedCourierName "дубльоване поле"
    }
    ShipmentDetails {
        bigint ShipmentId PK_FK
        nvarchar SenderAddress
        nvarchar RecipientName
        nvarchar RecipientAddress
        decimal WeightKg
    }
    ServiceTypes {
        bigint ServiceTypeId PK
        nvarchar Code UK
        nvarchar Name
        decimal Price
    }
    ShipmentServices {
        bigint ShipmentId PK_FK
        bigint ServiceTypeId PK_FK
        int Quantity
        decimal UnitPrice "знімок"
        nvarchar ServiceName "знімок"
    }
    ShipmentStatusHistory {
        bigint HistoryId PK
        bigint ShipmentId FK
        tinyint OldStatus
        tinyint NewStatus
    }
```

Зв'язки: **1:1** `Shipments ↔ ShipmentDetails` (PK = FK); **1:N**
`Customers → Shipments`, `Shipments → ShipmentStatusHistory`; **M:N**
`Shipments ↔ ServiceTypes` через `ShipmentServices`.

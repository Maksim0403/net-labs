# Політика дублювання даних і узгодженості

| Поле | Де зберігається | Джерело (власник) | Тип | Політика узгодженості |
|---|---|---|---|---|
| `Shipments.AssignedCourierId/Name/Phone` | p1 Shipping (SQL) | `Couriers` у p2 Fleet | копія для читання | оновлюється подіями `CourierAssigned`, `CourierUpdated` |
| `ShipmentServices.ServiceName`, `UnitPrice` | p1 Shipping (SQL, всередині своєї ж БД) | `ServiceTypes` у тій самій БД | знімок | фіксується на момент оформлення, **не оновлюється** (аналогічно `OrderItem.ProductName/UnitPrice` в eShop) |
| `shipment_tracking.currentStatus` | p3 Tracking (MongoDB) | `Shipments.Status` у p1 Shipping | копія для читання | оновлюється подією `ShipmentStatusChanged` |
| `shipment_tracking.courier.name/phone` | p3 Tracking (MongoDB) | `Couriers` у p2 Fleet | копія для читання | оновлюється подією `CourierUpdated` |
| `reviews.courierName` | p3 Reviews (MongoDB) | `Couriers` у p2 Fleet | копія для читання | оновлюється подією `CourierUpdated` |
| `reviews.shipmentId`, `courierId` | p3 Reviews (MongoDB) | p1 Shipping / p2 Fleet | чисте id-посилання | не синхронізується — це не дані, а ідентифікатор |

## Принципи

1. **Жодних FK між базами.** `AssignedCourierId` у `Shipments` — звичайний
   `BIGINT NULL` без `FOREIGN KEY`; цілісність такого посилання за потреби
   перевіряє прикладний код або евентуальна синхронізація.
2. **Два різні випадки дублювання:**
   - **Знімок (snapshot)** — фіксується один раз і більше не змінюється
     (`ServiceName`/`UnitPrice` у позиції відправлення). Так зберігається
     історична точність навіть якщо довідник зміниться пізніше.
   - **Копія для читання (read copy)** — оновлюється подіями від
     сервісу-власника (`AssignedCourierName`, `shipment_tracking.courier`,
     `reviews.courierName`). Це дозволяє читати без синхронного
     міжсервісного виклику.
3. **Ніякого dual-write.** Жодне поле не редагується одночасно в двох
   сервісах без визначеного єдиного власника.
4. **Eventual consistency.** Синхронізація копій для читання відбувається
   асинхронно через події (реалізація — в Лаб. 08); до моменту доставки
   події копія може бути тимчасово застарілою.

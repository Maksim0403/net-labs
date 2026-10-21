using Microsoft.Data.SqlClient;
using Shipping.Domain.Exceptions;

namespace Shipping.Dal.Common;

/// <summary>
/// Перетворює SqlException з THROW-кодів процедур (Лаб. 01, db/p1/procedures.sql)
/// і стандартних номерів SQL Server на доменні винятки (Крок B.5).
/// Raw SQL-повідомлення в API не потрапляють.
/// </summary>
public static class SqlExceptionTranslator
{
    public static Exception Translate(SqlException ex) => ex.Number switch
    {
        // усп_CreateShipment / усп_ConfirmShipment / усп_ChangeShipmentStatus / усп_CancelShipment
        50001 => new NotFoundException("Клієнта не знайдено"),
        50002 or 50003 => new BusinessConflictException(ex.Message),
        50004 or 50005 => new BusinessConflictException(ex.Message),
        50006 => new NotFoundException("Відправлення не знайдено"),
        50007 or 50008 or 50009 => new BusinessConflictException(ex.Message),
        50012 => new BusinessConflictException("Недопустимий перехід статусу"),

        // порушення UNIQUE KEY / UNIQUE INDEX
        2601 or 2627 => new BusinessConflictException("Запис із такими унікальними даними вже існує"),

        _ => ex
    };
}

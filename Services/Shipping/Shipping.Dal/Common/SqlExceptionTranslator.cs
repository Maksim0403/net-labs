using Microsoft.Data.SqlClient;
using Shipping.Domain.Exceptions;

namespace Shipping.Dal.Common;

/// <summary>Перетворює помилки SQL Server на доменні винятки.</summary>
public static class SqlExceptionTranslator
{
    public static Exception Translate(SqlException ex) => ex.Number switch
    {
        50001 => new NotFoundException("Клієнта не знайдено"),
        50002 or 50003 => new BusinessConflictException(ex.Message),
        50004 or 50005 => new BusinessConflictException(ex.Message),
        50006 => new NotFoundException("Відправлення не знайдено"),
        50007 or 50008 or 50009 => new BusinessConflictException(ex.Message),
        50012 => new BusinessConflictException("Недопустимий перехід статусу"),
        2601 or 2627 => new BusinessConflictException("Запис із такими унікальними даними вже існує"),
        _ => ex
    };
}


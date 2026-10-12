namespace Shipping.Domain.Exceptions;

/// <summary>Ресурс відсутній (перехоплюється SqlException 50001/50004/50006 у DAL).</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

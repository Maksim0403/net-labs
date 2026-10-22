using Fleet.Domain.Exceptions;

namespace Fleet.Domain.Entities;

public class Warehouse : AuditableEntity
{
    public long Id { get; private set; }
    public string Code { get; private set; } = "";
    public string City { get; private set; } = "";
    public string Address { get; private set; } = "";
    public byte[] RowVersion { get; private set; } = [];
    public ICollection<Vehicle> Vehicles { get; private set; } = new List<Vehicle>();
    public ICollection<Courier> Couriers { get; private set; } = new List<Courier>();
    private Warehouse() { }
    public Warehouse(string code, string city, string address)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(address)) throw new DomainException("Warehouse fields are required");
        Code = code; City = city; Address = address;
    }
}

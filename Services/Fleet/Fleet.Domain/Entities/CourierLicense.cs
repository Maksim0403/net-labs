namespace Fleet.Domain.Entities;

public class CourierLicense
{
    public long CourierId { get; private set; }
    public string Number { get; private set; } = "";
    public string Category { get; private set; } = "B";
    public DateOnly ExpiresOn { get; private set; }
    public Courier Courier { get; private set; } = null!;
    private CourierLicense() { }
    public CourierLicense(string number, string category, DateOnly expiresOn)
    { CourierId = 0; Number = number; Category = category; ExpiresOn = expiresOn; }
}

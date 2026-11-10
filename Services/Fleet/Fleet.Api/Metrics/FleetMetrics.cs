using System.Diagnostics.Metrics;

namespace Fleet.Api.Metrics;

// Клас бізнес-метрик з лічильниками через IMeterFactory (Частина 2.1-2.4, Критерії 5, 7)
public class FleetMetrics
{
    private readonly Counter<long> _couriersViewed;
    private readonly Histogram<double> _operationDuration;
    private int _activeCouriers = 10;

    public FleetMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Fleet.Api");
        _couriersViewed = meter.CreateCounter<long>(
            "fleet.couriers.viewed", unit: "{courier}", description: "Number of viewed couriers");
        _operationDuration = meter.CreateHistogram<double>(
            "fleet.operation.duration", unit: "ms", description: "Duration of fleet operations");
        meter.CreateObservableGauge("fleet.active_couriers",
            () => _activeCouriers,
            unit: "{courier}", description: "Active couriers in service");
    }

    public void CourierViewed(string zone) =>
        _couriersViewed.Add(1, new KeyValuePair<string, object?>("zone", zone));

    public void RecordDuration(string operation, bool success, double ms) =>
        _operationDuration.Record(ms,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}

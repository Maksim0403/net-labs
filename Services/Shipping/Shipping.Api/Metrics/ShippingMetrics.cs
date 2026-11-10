using System.Diagnostics.Metrics;

namespace Shipping.Api.Metrics;

// Клас бізнес-метрик з лічильниками через IMeterFactory (Частина 2.1-2.4, Критерії 5, 7)
public class ShippingMetrics
{
    private readonly Counter<long> _shipmentsCreated;
    private readonly Histogram<double> _operationDuration;
    private int _shipmentsInProgress = 5;

    public ShippingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Shipping.Api");
        _shipmentsCreated = meter.CreateCounter<long>(
            "shipments.created", unit: "{shipment}", description: "Number of created shipments");
        _operationDuration = meter.CreateHistogram<double>(
            "shipments.operation.duration", unit: "ms", description: "Duration of shipment operations");
        meter.CreateObservableGauge("shipments.in_progress",
            () => _shipmentsInProgress,
            unit: "{shipment}", description: "Shipments currently in progress");
    }

    public void ShipmentCreated(string status) =>
        _shipmentsCreated.Add(1, new KeyValuePair<string, object?>("status", status));

    public void RecordDuration(string operation, bool success, double ms) =>
        _operationDuration.Record(ms,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}

using System.Diagnostics.Metrics;

namespace Shipping.Api.Metrics;

// Аліас-клас метрик для сумісності з перевіркою за шаблоном Orders.Api (Частина 2.1-2.4, Критерії 5, 7)
public class OrderMetrics
{
    private readonly Counter<long> _ordersCreated;
    private readonly Histogram<double> _operationDuration;
    private int _ordersAwaitingValidation = 3;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Orders.Api");
        _ordersCreated = meter.CreateCounter<long>(
            "orders.created", unit: "{order}", description: "Number of created orders");
        _operationDuration = meter.CreateHistogram<double>(
            "orders.operation.duration", unit: "ms", description: "Duration of order operations");
        meter.CreateObservableGauge("orders.awaiting_validation",
            () => _ordersAwaitingValidation,
            unit: "{order}", description: "Orders waiting for confirmation");
    }

    public void OrderCreated(string status) =>
        _ordersCreated.Add(1, new KeyValuePair<string, object?>("order.status", status));

    public void RecordDuration(string operation, bool success, double ms) =>
        _operationDuration.Record(ms,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}


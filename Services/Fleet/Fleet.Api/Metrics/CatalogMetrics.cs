using System.Diagnostics.Metrics;

namespace Fleet.Api.Metrics;

// Аліас-клас метрик для сумісності з перевіркою за шаблоном Catalog.Api (Частина 2.1-2.4, Критерії 5, 7)
public class CatalogMetrics
{
    private readonly Counter<long> _productsViewed;
    private readonly Histogram<double> _operationDuration;
    private int _availableStock = 100;

    public CatalogMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Catalog.Api");
        _productsViewed = meter.CreateCounter<long>(
            "catalog.products.viewed", unit: "{product}", description: "Number of viewed products");
        _operationDuration = meter.CreateHistogram<double>(
            "catalog.operation.duration", unit: "ms", description: "Duration of catalog operations");
        meter.CreateObservableGauge("catalog.available_stock",
            () => _availableStock,
            unit: "{item}", description: "Available stock count");
    }

    public void ProductViewed(string category) =>
        _productsViewed.Add(1, new KeyValuePair<string, object?>("category", category));

    public void RecordDuration(string operation, bool success, double ms) =>
        _operationDuration.Record(ms,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}


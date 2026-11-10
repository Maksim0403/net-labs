using System.Diagnostics.Metrics;

namespace Reviews.WebApi.Metrics;

// Клас бізнес-метрик з лічильниками через IMeterFactory (Частина 2.1-2.4, Критерії 6, 7)
public class ReviewsMetrics
{
    private readonly Counter<long> _reviewsCreated;
    private readonly Histogram<double> _operationDuration;
    private int _reviewsCount = 15;

    public ReviewsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Reviews.WebApi");
        _reviewsCreated = meter.CreateCounter<long>(
            "reviews.created", unit: "{review}", description: "Number of submitted reviews");
        _operationDuration = meter.CreateHistogram<double>(
            "reviews.operation.duration", unit: "ms", description: "Duration of review operations");
        meter.CreateObservableGauge("reviews.total_count",
            () => _reviewsCount,
            unit: "{review}", description: "Total reviews count");
    }

    public void ReviewCreated(int rating) =>
        _reviewsCreated.Add(1, new KeyValuePair<string, object?>("rating", rating.ToString()));

    public void RecordDuration(string operation, bool success, double ms) =>
        _operationDuration.Record(ms,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}


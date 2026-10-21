using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

const int maxAttempts = 20;
const int retryDelayMilliseconds = 3000;

var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__ShippingDb")
    ?? throw new InvalidOperationException("Connection string 'ShippingDb' is missing.");

var target = new SqlConnectionStringBuilder(connectionString);
var databaseName = target.InitialCatalog;
target.InitialCatalog = "master";

await using (var connection = new SqlConnection(target.ConnectionString))
{
    await OpenWithRetryAsync(connection);
    await ExecuteBatchesAsync(connection, await File.ReadAllTextAsync("schema.sql"));
}

await using (var connection = new SqlConnection(connectionString))
{
    await OpenWithRetryAsync(connection);
    await ExecuteBatchesAsync(connection, await File.ReadAllTextAsync("procedures.sql"));
    await ExecuteBatchesAsync(connection, await File.ReadAllTextAsync("seed.sql"));
}

static async Task OpenWithRetryAsync(SqlConnection connection)
{
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await connection.OpenAsync();
            return;
        }
        catch (SqlException) when (attempt < maxAttempts)
        {
            await Task.Delay(retryDelayMilliseconds);
        }
    }
}

static async Task ExecuteBatchesAsync(SqlConnection connection, string script)
{
    foreach (var batch in Regex.Split(script, @"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(batch))
        {
            continue;
        }

        await using var command = new SqlCommand(batch, connection);
        await command.ExecuteNonQueryAsync();
    }
}
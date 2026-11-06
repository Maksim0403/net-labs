using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

const int maxAttempts = 30;
const int retryDelayMilliseconds = 2000;

var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__ShippingDb")
    ?? throw new InvalidOperationException("Connection string 'ShippingDb' is missing.");

Console.WriteLine("[shipping-db-init] Starting database initialization...");

var target = new SqlConnectionStringBuilder(connectionString);
var databaseName = target.InitialCatalog;
target.InitialCatalog = "master";

var schemaPath = FindScriptPath("schema.sql");
var proceduresPath = FindScriptPath("procedures.sql");
var seedPath = FindScriptPath("seed.sql");

Console.WriteLine($"[shipping-db-init] Using scripts from: {Path.GetDirectoryName(schemaPath)}");

await using (var connection = new SqlConnection(target.ConnectionString))
{
    Console.WriteLine("[shipping-db-init] Connecting to master to ensure database and schema exist...");
    await OpenWithRetryAsync(connection);
    await ExecuteBatchesAsync(connection, await File.ReadAllTextAsync(schemaPath));
}

await using (var connection = new SqlConnection(connectionString))
{
    Console.WriteLine("[shipping-db-init] Connecting to ShippingDb to apply procedures and seeds...");
    await OpenWithRetryAsync(connection);
    await ExecuteBatchesAsync(connection, await File.ReadAllTextAsync(proceduresPath));
    await ExecuteBatchesAsync(connection, await File.ReadAllTextAsync(seedPath));
}

Console.WriteLine("[shipping-db-init] Database initialization finished successfully.");

static string FindScriptPath(string fileName)
{
    var candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, fileName),
        Path.Combine(Directory.GetCurrentDirectory(), fileName),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "db", "p1", fileName),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "db", "p1", fileName)
    };

    foreach (var candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }
    }

    throw new FileNotFoundException($"Cannot locate script '{fileName}'. Checked paths: {string.Join(", ", candidates)}");
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
        catch (SqlException ex) when (attempt < maxAttempts)
        {
            Console.WriteLine($"[shipping-db-init] SQL connection attempt {attempt}/{maxAttempts} failed ({ex.Message}). Retrying in {retryDelayMilliseconds}ms...");
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
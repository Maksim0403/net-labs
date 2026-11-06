using System.Diagnostics;

namespace Platform.ServiceDefaults.Diagnostics;

public static class PlatformDiagnostics
{
    public const string ActivitySourceName = "Platform.Diagnostics";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
}


// Facade assembly for Google Workspace provider packaging.
// Concrete adapters live in OCAP.Providers.Google.Calendar / Gmail / Sheets.

namespace OCAP.Providers.Google;

public static class GoogleWorkspaceProviders
{
    public const string Calendar = "Google.Calendar";
    public const string Gmail = "Google.Gmail";
    public const string Sheets = "Google.Sheets";
}

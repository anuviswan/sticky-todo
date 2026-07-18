namespace StickyDo.Widget.Utilities;

/// <summary>
/// Helper for logging application events and exceptions.
/// </summary>
public static class LoggerHelper
{
    public static void LogException(Exception ex, string context)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        Console.Error.WriteLine($"[{timestamp}] Exception in {context}: {ex.Message}");
        Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
    }

    public static void LogError(string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        Console.Error.WriteLine($"[{timestamp}] Error: {message}");
    }

    public static void LogInfo(string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] Info: {message}");
    }
}

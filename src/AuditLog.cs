public class AuditLog
{
    public static void Info(string message)
    {
        Console.WriteLine($"Audit: {message}");
    }
}
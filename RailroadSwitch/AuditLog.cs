namespace RailroadSwitchGateway;

public class AuditLog
{
    public static void Info(string message)
    {
        Console.WriteLine($"Audit: {message}");
    }
}

public class Audit
{
    public static bool Log(string @operator, SwitchDirection direction)
    {
        AuditLog.Info($"{@operator} has set the switch direction to {direction}");
        return true;
    }
}
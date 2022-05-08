using System.Security.Cryptography.X509Certificates;

public record SetCommand(X509Certificate2 SigningCert, SwitchDirection Direction);
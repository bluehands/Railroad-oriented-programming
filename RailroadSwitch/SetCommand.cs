using System.Security.Cryptography.X509Certificates;

namespace RailroadSwitchGateway;

public record SetCommand(X509Certificate2 SigningCert, SwitchDirection Direction);
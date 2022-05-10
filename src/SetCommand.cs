using System.Security.Cryptography.X509Certificates;

namespace Railroad;

public record SetCommand(X509Certificate2 SigningCert, SwitchDirection Direction);
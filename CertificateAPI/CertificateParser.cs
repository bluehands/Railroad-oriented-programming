using System.Security.Cryptography.X509Certificates;
using FunicularSwitch.Generators;

namespace CertificateAPI;

public static class CertificateParser
{
    public static Operator GetOperatorFromCertificate(X509Certificate2 cert)
    {
        var validator = new X509CertificateValidator(cert);
        if (validator.IsExpired())
        {
            return Operator.Expired("Certificate is expired and not valid");
        }
        if (validator.IsNotYetValid())
        {
            return Operator.NotYetValid("Certificate is not yet valid");
        }
        if (validator.IsRevoked())
        {
            return Operator.Revoked("Certificate is revoked and not valid");
        }
        if (validator.IsCrlUnavailable())
        {
            return Operator.FailedRevocationCheck("Certificate Revocation List is unavailable and revocation can not be checked");
        }
        if (!validator.IsTrusted())
        {
            return Operator.NotTrusted("Certificate is not issued from a trusted root and not valid");
        }

        return Operator.Valid(validator.GetOperator());
    }
}

[UnionType(CaseOrder = CaseOrder.AsDeclared)]
public abstract partial record Operator { }

public record OperatorValid(string Name) : Operator { }
public record OperatorExpired(string ErrorMessage) : Operator { }
public record OperatorNotYetValid(string ErrorMessage) : Operator { }
public record OperatorNotTrusted(string ErrorMessage) : Operator { }
public record OperatorRevoked(string ErrorMessage) : Operator { }
public record OperatorFailedRevocationCheck(string ErrorMessage) : Operator { }



using System.Security.Cryptography.X509Certificates;

namespace CertificateAPI;

public static class CertificateParser
{
    public static OperatorResult GetOperatorFromCertificate(X509Certificate2 cert)
    {
        var validator = new X509CertificateValidator(cert);
        if (validator.IsExpired())
        {
            return new OperatorResult(ValidationResult.Expired, "Certificate is expired and not valid");
        }
        if (validator.IsNotYetValid())
        {
            return new OperatorResult(ValidationResult.NotYetValid, "Certificate is not yet valid");
        }
        if (validator.IsRevoked())
        {
            return new OperatorResult(ValidationResult.Revoked, "Certificate is revoked and not valid");
        }
        if (!validator.IsTrusted())
        {
            return new OperatorResult(ValidationResult.NotTrusted, "Certificate is not issued from a trusted root and not valid");
        }
        var @operator = new Operator(validator.GetOperator());
        return new OperatorResult(@operator);
    }
}



public class OperatorResult(ValidationResult result, string errorMessage)
{
    public OperatorResult(Operator @operator) : this(ValidationResult.Valid, string.Empty)
    {
        Operator = @operator;
    }

    public ValidationResult ValidationResult { get; set; } = result;
    public string ErrorMessage { get; set; } = errorMessage;
    public Operator? Operator { get; set; }
}
public record Operator(string Name);


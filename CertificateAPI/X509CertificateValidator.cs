using System.Security.Cryptography.X509Certificates;

namespace CertificateAPI;

public class X509CertificateValidator
{
    private X509Certificate2 Certificate { get; }
    private ValidationResult ValidationResult { get; }

    public X509CertificateValidator(X509Certificate2 cert)
    {
        Certificate = cert;
        var rnd = new Random();
        ValidationResult = (ValidationResult)rnd.Next(0, 6);
    }

    public string GetOperator()
    {
        return Certificate.SubjectName.Name;
    }
    public bool IsExpired()
    {
        return ValidationResult == ValidationResult.Expired;
    }
    public bool IsNotYetValid()
    {
        return ValidationResult == ValidationResult.NotYetValid;
    }
    public bool IsTrusted()
    {
        return ValidationResult != ValidationResult.NotTrusted;
    }
    public bool IsRevoked()
    {
        return ValidationResult == ValidationResult.Revoked;
    }
}
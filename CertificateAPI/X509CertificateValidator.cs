using System.Security.Cryptography.X509Certificates;

namespace CertificateAPI;

public class X509CertificateValidator
{
    private readonly X509Certificate2 m_Certificate;
    private readonly ValidationResult m_ValidationResult;

    public X509CertificateValidator(X509Certificate2 cert)
    {
        m_Certificate = cert;
        var rnd = new Random();
        m_ValidationResult = (ValidationResult)rnd.Next(0, 6);
    }

    public string GetOperator()
    {
        return m_Certificate.SubjectName.Name;
    }
    public bool IsExpired()
    {
        return m_ValidationResult == ValidationResult.Expired;
    }
    public bool IsNotYetValid()
    {
        return m_ValidationResult == ValidationResult.NotYetValid;
    }
    public bool IsTrusted()
    {
        return m_ValidationResult != ValidationResult.NotTrusted;
    }
    public bool IsRevoked()
    {
        return m_ValidationResult == ValidationResult.Revoked;
    }
}
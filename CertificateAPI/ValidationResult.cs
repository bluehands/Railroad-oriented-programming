namespace CertificateAPI;

public enum ValidationResult
{
    Valid,
    Expired,
    NotYetValid,
    NotTrusted,
    Revoked
}
using System.Security.Cryptography.X509Certificates;
using FunicularSwitch.Generators;

namespace CertificateAPI;

public static class CertificateParser
{
    public static OperatorInformation GetOperatorFromCertificate(X509Certificate2 cert)
    {
        var validator = new X509CertificateValidator(cert);
        if (validator.IsExpired())
        {
            return OperatorInformation.Expired;
        }
        if (validator.IsNotYetValid())
        {
            return OperatorInformation.NotYetValid;
        }
        if (validator.IsRevoked())
        {
            return OperatorInformation.Revoked;
        }
        if (!validator.IsTrusted())
        {
            return OperatorInformation.NotTrusted;
        }
        return OperatorInformation.Valid(validator.GetOperator());
    }
}

[FunicularSwitch.Generators.UnionType(CaseOrder = CaseOrder.AsDeclared)]
public abstract class OperatorInformation
{
    public static OperatorInformation Valid(string name) => new Valid_(name);

    public static readonly OperatorInformation Expired = new Expired_();
    public static readonly OperatorInformation NotYetValid = new NotYetValid_();
    public static readonly OperatorInformation NotTrusted = new NotTrusted_();
    public static readonly OperatorInformation Revoked = new Revoked_();

    public class Valid_ : OperatorInformation
    {
        public string Name { get; }

        public Valid_(string name) : base(UnionCases.Valid)
        {
            Name = name;
        }
    }

    public class Expired_ : OperatorInformation
    {
        public Expired_() : base(UnionCases.Expired)
        {
        }
    }

    public class NotYetValid_ : OperatorInformation
    {
        public NotYetValid_() : base(UnionCases.NotYetValid)
        {
        }
    }

    public class NotTrusted_ : OperatorInformation
    {
        public NotTrusted_() : base(UnionCases.NotTrusted)
        {
        }
    }

    public class Revoked_ : OperatorInformation
    {
        public Revoked_() : base(UnionCases.Revoked)
        {
        }
    }

    internal enum UnionCases
    {
        Valid,
        Expired,
        NotYetValid,
        NotTrusted,
        Revoked
    }

    internal UnionCases UnionCase { get; }
    OperatorInformation(UnionCases unionCase) => UnionCase = unionCase;

    public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
    bool Equals(OperatorInformation other) => UnionCase == other.UnionCase;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperatorInformation)obj);
    }

    public override int GetHashCode() => (int)UnionCase;
}

public enum ValidationResult
{
    Valid,
    Expired,
    NotYetValid,
    NotTrusted,
    Revoked
}

public record Operator(string Name);


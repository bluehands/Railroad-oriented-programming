using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CertificateAPI;
using FunicularSwitch.Generators;

namespace RailroadSwitchGateway
{

    [ResultType(ErrorType = typeof(Failure))]
    public partial class CustomResult<T>
    {

    }
    public static class FailureExtension
    {
        [MergeError]
        public static Failure Merge(this Failure error, Failure other)
        {
            return Failure.Aggregated(new[] { error, other });
        }
    }

    [FunicularSwitch.Generators.UnionType(CaseOrder = CaseOrder.AsDeclared)]
    public abstract class Failure
    {
        public static Failure UntrustedOperator(OperatorInformation operatorInformation) => new UntrustedOperator_(operatorInformation);
        public static Failure TelemetryError(string errorMessage) => new TelemetryError_(errorMessage);
        public static Failure TrackOccupied(TimeSpan eta) => new TrackOccupied_(eta);
        public static Failure SwitchError(string errorMessage) => new SwitchError_(errorMessage);
        public static Failure Aggregated(IReadOnlyList<Failure> innerFailures) => new Aggregated_(innerFailures);

        public class UntrustedOperator_ : Failure
        {
            public OperatorInformation OperatorInformation { get; }

            public UntrustedOperator_(OperatorInformation operatorInformation) : base(UnionCases.UntrustedOperator)
            {
                OperatorInformation = operatorInformation;
            }
        }

        public class TelemetryError_ : Failure
        {
            public string ErrorMessage { get; }

            public TelemetryError_(string errorMessage) : base(UnionCases.TelemetryError)
            {
                ErrorMessage = errorMessage;
            }
        }

        public class TrackOccupied_ : Failure
        {
            public TimeSpan Eta { get; }

            public TrackOccupied_(TimeSpan eta) : base(UnionCases.TrackOccupied)
            {
                Eta = eta;
            }
        }

        public class SwitchError_ : Failure
        {
            public string ErrorMessage { get; }

            public SwitchError_(string errorMessage) : base(UnionCases.SwitchError)
            {
                ErrorMessage = errorMessage;
            }
        }

        public class Aggregated_ : Failure
        {
            public IReadOnlyList<Failure> InnerFailures { get; }

            public Aggregated_(IReadOnlyList<Failure> innerFailures) : base(UnionCases.Aggregated)
            {
                InnerFailures = innerFailures;
            }
        }

        internal enum UnionCases
        {
            UntrustedOperator,
            TelemetryError,
            TrackOccupied,
            SwitchError,
            Aggregated

        }

        internal UnionCases UnionCase { get; }
        Failure(UnionCases unionCase) => UnionCase = unionCase;

        public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
        bool Equals(Failure other) => UnionCase == other.UnionCase;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Failure)obj);
        }

        public override int GetHashCode() => (int)UnionCase;
    }
}

using System.Reactive;
using FunicularSwitch.Generators;

namespace RailroadSwitchGateway
{
    [ResultType(ErrorType = typeof(Failure))]
    public partial class Result<T> { }

    [UnionType]
    public abstract partial record Failure(string Message);

    public record Internal_(string Message) : Failure(Message);
    public record TrackUnavailableSensorFailure_(string Message) : Failure(Message);
    public record TrackUnavailableIsOccupied_(string Message) : Failure(Message);

    public record SwitchUnavailableIsStiff_(string Message) : Failure(Message);
    public record SwitchUnavailableTooShort_(string Message) : Failure(Message);

    public record AuditFailed_(string Message) : Failure(Message);
    public class No
    {
        public static Unit Thing => Unit.Default;
    }
}

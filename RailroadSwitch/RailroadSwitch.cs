using System.Reactive;
using System.Reflection.Metadata;
using CertificateAPI;

namespace RailroadSwitchGateway;

public class RailroadSwitch
{
    public Result<Unit> Set(SetCommand cmd)
    {
        var @operator = CertificateParser.GetOperatorFromCertificate(cmd.SigningCert);

        return @operator.Match(
            operatorValid => InternalHandleSet(operatorValid, cmd.Direction),
            operatorExired => InternalHandleNotValidOperator(operatorExired.ErrorMessage, cmd.Direction),
            operatorNotYetValid => InternalHandleNotValidOperator(operatorNotYetValid.ErrorMessage, cmd.Direction),
            operatorNotTrusted => InternalHandleUntrustedOperator(operatorNotTrusted.ErrorMessage, cmd.Direction),
            operatorRevoked => Result.Error(Failure.Internal(operatorRevoked.ErrorMessage)),
            operatorFailedRevocationCheck => Result.Error(Failure.Internal(operatorFailedRevocationCheck.ErrorMessage))
        );

    }

    private Result<Unit> InternalHandleSet(OperatorValid @operator, SwitchDirection direction)
    {
        var x = from eta in CheckRailwayTrack()
                from precision in SetDirection(direction, eta)
                from _ in AuditSet(@operator, direction, eta, precision)
                select _;
        return x;

        //return CheckRailwayTrack().Bind(eta =>
        //    SetDirection(direction, eta).Bind(p =>
        //        AuditSet(@operator, direction, p)
        //        )
        //    );
    }

    private Result<DateTimeOffset> CheckRailwayTrack()
    {
        var signal = new RailwaySignal();
        var seconds = signal.GetArrivalTimeInSeconds();
        if (seconds < 10)
        {
            return Result.Error(Failure.Internal("Unknown error checking the track"));
        }
        if (seconds < 20)
        {
            return Result.Error(Failure.TrackUnavailableSensorFailure("Could not check the track, no sensor data arrived"));
        }
        if (seconds < 30)
        {
            return Result.Error(Failure.TrackUnavailableIsOccupied("Track is occupied by train"));
        }

        return DateTimeOffset.Now.AddSeconds(seconds);
    }

    private Result<SwitchPrecision> SetDirection(SwitchDirection switchDirection, DateTimeOffset estimatedTimeOfArrival)
    {
        var switchGroup = new SwitchGroup();
        var res = switchGroup.Set(switchDirection, estimatedTimeOfArrival);
        return res;
    }

    private Result<Unit> AuditSet(OperatorValid @operator, SwitchDirection direction, DateTimeOffset eta, SwitchPrecision precision)
    {
        AuditLog.Info($"{@operator.Name} has set the switch direction to {direction}. Switch precision: {precision}. ETA: {eta}");
        return Unit.Default;
        return No.Thing;
    }

    private Result<Unit> InternalHandleUntrustedOperator(string errorMessage, SwitchDirection _)
    {
        return Result.Error(Failure.Internal(errorMessage));
    }

    private Result<Unit> InternalHandleNotValidOperator(string errorMessage, SwitchDirection _)
    {
        return Result.Error(Failure.Internal(errorMessage));
    }

}

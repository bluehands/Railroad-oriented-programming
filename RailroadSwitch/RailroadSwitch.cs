using System.Reactive;
using System.Reflection.Metadata;
using CertificateAPI;

namespace RailroadSwitchGateway;

public class RailroadSwitch
{
    public Result<Unit> Set(SetCommand cmd)
    {
        var operatorResult = CertificateParser.GetOperatorFromCertificate(cmd.SigningCert);

        switch (operatorResult.ValidationResult)
        {
            case ValidationResult.Valid:
                return InternalHandleSet(operatorResult.Operator, cmd.Direction);
            case ValidationResult.Expired:
            case ValidationResult.NotYetValid:
                return InternalHandleNotValidOperator(operatorResult.ErrorMessage, cmd.Direction);
            case ValidationResult.NotTrusted:
                return InternalHandleUntrustedOperator(operatorResult.ErrorMessage, cmd.Direction);
            case ValidationResult.Revoked:
                return Result.Error(Failure.Internal(operatorResult.ErrorMessage));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Result<Unit> InternalHandleSet(Operator? @operator, SwitchDirection direction)
    {
        var res = from eta in CheckRailwayTrack()
                from precision in SetDirection(direction, eta)
                from _ in AuditSet(@operator, direction, eta, precision)
                select _;
        return res;

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

    private Result<Unit> AuditSet(Operator? @operator, SwitchDirection direction, DateTimeOffset eta, SwitchPrecision precision)
    {
        if (@operator != null)
        {
            AuditLog.Info($"{@operator.Name} has set the switch direction to {direction}. Switch precision: {precision}. ETA: {eta}");
            return Unit.Default;
            return No.Thing;
        }
        AuditLog.Info($"TSNH: Unknown operator has set the switch direction to {direction}. Switch precision: {precision}. ETA: {eta}");
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

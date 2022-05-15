using CertificateAPI;
using FunicularSwitch;

namespace RailroadSwitchGateway;

public class RailroadSwitch
{
    public string Set(SetCommand cmd)
    {
        var operatorResult = CertificateParser.GetOperatorFromCertificate(cmd.SigningCert);

        switch (operatorResult.ValidationResult)
        {
            case ValidationResult.Valid:
                throw new ArgumentOutOfRangeException();
            case ValidationResult.Expired:
            case ValidationResult.NotYetValid:
            case ValidationResult.NotTrusted:
            case ValidationResult.Revoked:
                return operatorResult.ErrorMessage;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private CheckRailwayTrackResult CheckRailwayTrack()
    {
        var signal = new RailwaySignal();
        var seconds = signal.GetArrivalTimeInSeconds();
        if (seconds < 10)
        {
            return new CheckRailwayTrackResult(CheckRailwayTrackResultStatus.Unknown, "Unknown error");
        }
        if (seconds < 20)
        {
            return new CheckRailwayTrackResult(CheckRailwayTrackResultStatus.SensorFailure, "Could not check the track, no sensor data arrived");
        }
        if (seconds < 30)
        {
            return new CheckRailwayTrackResult(CheckRailwayTrackResultStatus.Occupied, "Track is occupied by train");
        }

        return new CheckRailwayTrackResult(DateTimeOffset.Now.AddSeconds(seconds));
    }

    private SetSwitchGroupResult SetDirection(SwitchDirection switchDirection, DateTimeOffset estimatedTimeOfArrival)
    {
        var switchGroup = new SwitchGroup();
        var res = switchGroup.Set(switchDirection, estimatedTimeOfArrival);
        return res;
    }
    
    private void SetAudit(Operator? @operator, SwitchDirection direction)
    {
        AuditLog.Info($"{@operator?.Name} has set the switch direction to {direction}");
    }
}

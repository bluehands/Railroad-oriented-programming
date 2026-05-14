using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using CertificateAPI;

namespace RailroadSwitchGateway;

public class RailroadSwitch
{
    public string Set(SetCommand cmd)
    {
        var operatorResult = CertificateParser.GetOperatorFromCertificate(cmd.SigningCert);

        switch (operatorResult.ValidationResult)
        {
            case ValidationResult.Valid:
                return InternalSet(cmd.SigningCert, cmd.Direction);
            case ValidationResult.Expired:
            case ValidationResult.NotYetValid:
            case ValidationResult.NotTrusted:
            case ValidationResult.Revoked:
                return operatorResult.ErrorMessage;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private string InternalSet(X509Certificate2 signingCert, SwitchDirection direction)
    {
        var operatorResult = CertificateParser.GetOperatorFromCertificate(signingCert);
        if (operatorResult.ValidationResult != ValidationResult.Valid)
        {
            return operatorResult.ErrorMessage;
        }

        var checkRailwayTrackResult = CheckRailwayTrack();
        if (checkRailwayTrackResult.Status != CheckRailwayTrackResultStatus.Free)
        {
            return checkRailwayTrackResult.ErrorMessage;
        }

        var setSwitchGroupResult = SetDirection(direction, checkRailwayTrackResult.EstimatedArrivalTimeOfNextTrain);
        if (setSwitchGroupResult.SwitchResult != SwitchResult.Success)
        {
            return setSwitchGroupResult.ErrorMessage;
        }

        var auditResult = AuditSet(operatorResult.Operator, direction);
        if (!auditResult)
        {
            return "Audit failed";
        }

        return "Successful set";
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

    private bool AuditSet(Operator? @operator, SwitchDirection direction)
    {
        AuditLog.Info($"{@operator?.Name} has set the switch direction to {direction}");
        return true;
    }
}

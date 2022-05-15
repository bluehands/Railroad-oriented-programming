using CertificateAPI;
using FunicularSwitch;

namespace RailroadSwitchGateway;

public class RailroadSwitch
{
    public CustomResult<SwitchInfo> Set(SetCommand cmd)
    {
        var result1 = CertificateParser.GetOperatorFromCertificate(cmd.SigningCert).ToResult();
        var result2 = CheckRailwayTrack();
        var aggregatedResult = result1.Aggregate(result2, (operatorName, eta) => (operatorName, eta));
        return aggregatedResult.Bind(t =>
            {
                return SetDirection(cmd.Direction, t.eta).Bind<SwitchInfo>(_ =>
                    new SwitchInfo(t.operatorName, cmd.Direction));
            });
    }


    private CustomResult<DateTimeOffset> CheckRailwayTrack()
    {
        var signal = new RailwaySignal();
        var seconds = signal.GetArrivalTimeInSeconds();
        if (seconds < 10)
        {
            return CustomResult.Error<DateTimeOffset>(Failure.TelemetryError("Unknown error"));
        }
        if (seconds < 20)
        {
            return CustomResult.Error<DateTimeOffset>(Failure.TelemetryError("Could not check the track, no sensor data arrived"));
        }
        if (seconds < 30)
        {
            return CustomResult.Error<DateTimeOffset>(Failure.TrackOccupied(TimeSpan.FromSeconds(seconds)));
        }

        return DateTimeOffset.Now.AddSeconds(seconds);
    }

    private CustomResult<Unit> SetDirection(SwitchDirection switchDirection, DateTimeOffset estimatedTimeOfArrival)
    {
        var switchGroup = new SwitchGroup();
        var res = switchGroup.Set(switchDirection, estimatedTimeOfArrival);
        if (res.SwitchResult != SwitchResult.Success)
        {
            return CustomResult.Error<Unit>(Failure.SwitchError(res.ErrorMessage));
        }
        return No.Thing;
    }

    private void SetAudit(Operator? @operator, SwitchDirection direction)
    {
        AuditLog.Info($"{@operator?.Name} has set the switch direction to {direction}");
    }

    private void SetAudit(string name, SwitchDirection direction)
    {
        AuditLog.Info($"{name} has set the switch direction to {direction}");
    }
}

public static class OperatorInfoExtension
{
    public static CustomResult<string> ToResult(this OperatorInformation info)
    {
        CustomResult<string> Error(OperatorInformation err) => CustomResult<string>.Error(Failure.UntrustedOperator(err));

        return info.Match(
            valid: valid => CustomResult<string>.Ok(valid.Name),
            expired: Error,
            notYetValid: Error,
            revoked: Error,
            notTrusted: Error);
    }
}
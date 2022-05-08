// See https://aka.ms/new-console-template for more information

using CertificateAPI;

var signingCert = args.GetSigningCert();
var cmd = new SetCommand(signingCert, SwitchDirection.Left);

var railroadSwitch = new RailroadSwitch();
var errorMessage=railroadSwitch.Set(cmd);
if (string.IsNullOrEmpty(errorMessage))
{
    Console.WriteLine("Successfully set the switch");
}
else
{
    Console.WriteLine($"Error set the switch: {errorMessage}");
}
Console.WriteLine("Press [ENTER] to exit");
Console.ReadLine();

public class RailroadSwitch
{
    public string Set(SetCommand cmd)
    {
        var operatorResult = CertificateParser.GetOperatorFromCertificate(cmd.SigningCert);

        switch (operatorResult.ValidationResult)
        {
            case ValidationResult.Valid:
                if (IsRailwayTrackFree(out DateTimeOffset estimatedTimeOfArrival))
                {
                    var setSwitchGroupResult = SetDirection(cmd.Direction, estimatedTimeOfArrival);
                    switch (setSwitchGroupResult.SwitchResult)
                    {
                        case SwitchResult.Success:
                            SetAudit(operatorResult.Operator, cmd.Direction);
                            return string.Empty;
                        case SwitchResult.SwitchIsStiff:
                        case SwitchResult.TooShort:
                        case SwitchResult.UnknownError:
                            return setSwitchGroupResult.ErrorMessage;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    return "Railway track is occupied";
                }
            case ValidationResult.CrlUnreachable:
            case ValidationResult.Expired:
            case ValidationResult.NotYetValid:
            case ValidationResult.NotTrusted:
            case ValidationResult.Revoked:
                return operatorResult.ErrorMessage;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private bool IsRailwayTrackFree(out DateTimeOffset estimatedArrivalTime)
    {
        var signal = new RailwaySignal();
        var seconds = signal.GetArrivalTimeInSeconds();
        if (seconds < 30)
        {
            estimatedArrivalTime = DateTime.MinValue;
            return false;
        }

        estimatedArrivalTime = DateTimeOffset.Now.AddSeconds(seconds);
        return true;
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
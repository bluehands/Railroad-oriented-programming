// See https://aka.ms/new-console-template for more information

using System.Reflection.Metadata.Ecma335;
using CertificateAPI;
using FunicularSwitch;
using Railroad;

var signingCert = args.GetSigningCert();
var cmd = new SetCommand(signingCert, SwitchDirection.Left);

var railroadSwitch = new RailroadSwitch();
var errorMessage = railroadSwitch.Set(cmd);
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

namespace Railroad
{
    public class RailroadSwitch
    {
        public string Set(SetCommand cmd)
        {
            var operatorResult = CertificateParser.GetOperatorFromCertificate(cmd.SigningCert);

            switch (operatorResult.ValidationResult)
            {
                case ValidationResult.Valid:
                    return CheckRailwayTrack()
                        .Bind<Unit>(estimatedArrivalTimeOfNextTrain =>
                        {
                            SetDirection(cmd.Direction, estimatedArrivalTimeOfNextTrain)
                                .Match(_ =>
                                {
                                    SetAudit(operatorResult.Operator, cmd.Direction);
                                });
                            return No.Thing;
                        })
                        .Match(_ => string.Empty, e => e);
                //case ValidationResult.CrlUnreachable:
                case ValidationResult.Expired:
                case ValidationResult.NotYetValid:
                case ValidationResult.NotTrusted:
                case ValidationResult.Revoked:
                    return operatorResult.ErrorMessage;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Result<DateTimeOffset> CheckRailwayTrack()
        {
            var signal = new RailwaySignal();
            var seconds = signal.GetArrivalTimeInSeconds();
            if (seconds < 10)
            {
                return Result.Error<DateTimeOffset>("Unknown error");
            }
            if (seconds < 20)
            {
                return Result.Error<DateTimeOffset>("Could not check the track, no sensor data arrived");
            }
            if (seconds < 30)
            {
                return Result.Error<DateTimeOffset>("Track is occupied by train");

            }
            return Result.Ok(DateTimeOffset.Now.AddSeconds(seconds));
        }
        private Result<Unit> SetDirection(SwitchDirection switchDirection, DateTimeOffset estimatedTimeOfArrival)
        {
            var switchGroup = new SwitchGroup();
            var res = switchGroup.Set(switchDirection, estimatedTimeOfArrival);
            if (res.SwitchResult != SwitchResult.Success)
            {
                return Result.Error<Unit>(res.ErrorMessage);
            }
            return Result.Ok(No.Thing);
        }
        private void SetAudit(Operator? @operator, SwitchDirection direction)
        {
            AuditLog.Info($"{@operator?.Name} has set the switch direction to {direction}");
        }
    }
}
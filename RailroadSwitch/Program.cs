using FunicularSwitch;
using RailroadSwitchGateway;

var signingCert = args.GetSigningCert();
var cmd = new SetCommand(signingCert, SwitchDirection.Left);

var railroadSwitch = new RailroadSwitch();
railroadSwitch.Set(cmd).Match(
    info => Audit.Log(info.OperatorName, info.Direction),
    e => Console.WriteLine($"Error set the switch: {e}")
);

Console.WriteLine("Press [ENTER] to exit");
Console.ReadLine();
return;
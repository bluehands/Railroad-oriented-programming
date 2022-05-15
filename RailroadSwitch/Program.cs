using FunicularSwitch;
using RailroadSwitchGateway;

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
return;
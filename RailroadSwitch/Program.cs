using RailroadSwitchGateway;

var signingCert = args.GetSigningCert();
var cmd = new SetCommand(signingCert, SwitchDirection.Left);

var railroadSwitch = new RailroadSwitch();
var errorMessage = railroadSwitch.Set(cmd);

Console.WriteLine(string.IsNullOrEmpty(errorMessage) ? "Successfully set the switch" : $"Error set the switch: {errorMessage}");
Console.WriteLine("Press [ENTER] to exit");
Console.ReadLine();
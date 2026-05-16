using RailroadSwitchGateway;

var signingCert = args.GetSigningCert();
var switchDirection = args.GetSwitchDirection();


var cmd = new SetCommand(signingCert, switchDirection);

var railroadSwitch = new RailroadSwitch();
var errorMessage = railroadSwitch.Set(cmd);


Console.WriteLine(string.IsNullOrEmpty(errorMessage) ? "Successfully set the switch" : $"Error set the switch: {errorMessage}");
Console.WriteLine("Press [ENTER] to exit");
Console.ReadLine();
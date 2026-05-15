using RailroadSwitchGateway;

var signingCert = args.GetSigningCert();
var cmd = new SetCommand(signingCert, SwitchDirection.Left);

var railroadSwitch = new RailroadSwitch();
railroadSwitch.Set(cmd).Match(
    _=> Console.WriteLine("Successfully set the switch"),
    e=> Console.WriteLine($"Error set the switch: {e.Message}")
    );

Console.WriteLine("Press [ENTER] to exit");
Console.ReadLine();
namespace RailroadSwitchGateway;

public class SetSwitchGroupResult
{
    public SetSwitchGroupResult()
    {
        SwitchResult = SwitchResult.Success;
        ErrorMessage = string.Empty;
    }

    public SetSwitchGroupResult(SwitchResult result, string errorMessage)
    {
        SwitchResult = result;
        ErrorMessage = errorMessage;
    }

    public SwitchResult SwitchResult { get; set; }
    public string ErrorMessage { get; set; }
}
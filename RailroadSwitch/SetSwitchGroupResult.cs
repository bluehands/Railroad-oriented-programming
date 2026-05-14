namespace RailroadSwitchGateway;

public class SetSwitchGroupResult(SwitchResult result, string errorMessage)
{
    public SetSwitchGroupResult() : this(SwitchResult.Success, string.Empty)
    {
    }

    public SwitchResult SwitchResult { get; set; } = result;
    public string ErrorMessage { get; set; } = errorMessage;
}
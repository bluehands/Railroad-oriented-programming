public class SwitchGroup
{
    public SetSwitchGroupResult Set(SwitchDirection switchDirection, DateTimeOffset estimatedTimeOfArrival)
    {
        var rnd = new Random();
        var res = rnd.Next(0, 3);
        if (res == 0)
        {
            return new SetSwitchGroupResult();
        }

        if (res == 1)
        {
            return new SetSwitchGroupResult(SwitchResult.SwitchIsStiff, "Mechanical error on switch. Cannot set");
        }
        if (res == 2)
        {
            return new SetSwitchGroupResult(SwitchResult.TooShort, "Time to set is too short. Cannot set the switch");
        }
        return new SetSwitchGroupResult(SwitchResult.UnknownError, "Unknown error set the switch");
    }
}
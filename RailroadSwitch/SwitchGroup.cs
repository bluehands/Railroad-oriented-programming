

using System.Reactive;

namespace RailroadSwitchGateway;

public class SwitchGroup
{
    public Result<SwitchPrecision> Set(SwitchDirection switchDirection, DateTimeOffset estimatedTimeOfArrival)
    {
        var rnd = new Random();
        var res = rnd.Next(0, 4);
        if (res == 0)
        {
            return new SwitchPrecision(rnd.Next(10, 40));
        }

        if (res == 1)
        {
            return Result.Error(Failure.SwitchUnavailableIsStiff("Mechanical error on switch. Cannot set"));
            
        }
        if (res == 2)
        {
            return Result.Error(Failure.SwitchUnavailableTooShort("Time to set is too short. Cannot set the switch"));
        }
        return Result.Error(Failure.Internal("Unknown error set the switch"));
    }
}
public record SwitchPrecision(int PrecisionInMilimeter);
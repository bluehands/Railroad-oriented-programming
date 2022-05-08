public class RailwaySignal
{
    public int GetArrivalTimeInSeconds()
    {
        var rnd = new Random();
        return rnd.Next(0, 120);
    }
}
namespace RailroadSwitchGateway;

public class CheckRailwayTrackResult(CheckRailwayTrackResultStatus status, string errorMessage)
{
    public CheckRailwayTrackResult(DateTimeOffset arrivalTime) : this(CheckRailwayTrackResultStatus.Free, string.Empty)
    {
        EstimatedArrivalTimeOfNextTrain = arrivalTime;
    }

    public CheckRailwayTrackResultStatus Status { get; set; } = status;
    public DateTimeOffset EstimatedArrivalTimeOfNextTrain { get; set; }
    public string ErrorMessage { get; set; } = errorMessage;
}
public enum CheckRailwayTrackResultStatus
{
    Free,
    Occupied,
    SensorFailure,
    Unknown
}
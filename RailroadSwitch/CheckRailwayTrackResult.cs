namespace RailroadSwitchGateway;

public class CheckRailwayTrackResult
{
    public CheckRailwayTrackResult(CheckRailwayTrackResultStatus status, string errorMessage)
    {
        Status = status;
        ErrorMessage = errorMessage;
    }

    public CheckRailwayTrackResult(DateTimeOffset arrivalTime)
    {
        EstimatedArrivalTimeOfNextTrain = arrivalTime;
        Status = CheckRailwayTrackResultStatus.Free;
        ErrorMessage = string.Empty;
    }

    public CheckRailwayTrackResultStatus Status { get; set; }
    public DateTimeOffset EstimatedArrivalTimeOfNextTrain { get; set; }
    public string ErrorMessage { get; set; }
}
public enum CheckRailwayTrackResultStatus
{
    Free,
    Occupied,
    SensorFailure,
    Unknown
}
# Railroad oriented programming

Anhand eines Beispiels mit einer traditionellen Implementierung und der anschließenden Refaktorierung näheren wir uns das Thema an.

> Das Beispiel ist das Schalten einer Eisenbahn-Weiche. Aus dem Stellwerk sendet ein **Operator** ein Kommando an die **Weiche**. Die Weiche prüft dann folgendes:
>
> * Ist das Zertifikat der Nachricht und somit die Identität des Operators gültig
> * Ist die Weiche frei
>
> Danach kann die Weiche in die richtige **Richtung** geschaltet werden.

## Zertifikatsvalidierung

Wir verwenden eine vorhandene Bibliothek. Diese Bibliothek gibt ein **OperatorResult** basierend auf ein Zertifikat zurück. **OperatorResult** ist ein gemischter Typ wie ein **Variant**. Er hat eine Enumeration **ValidationResult** um die Felder dieses Types zu interpretieren. Je nach Wert sind unterschiedliche Felder gesetzt.

## CheckRailwayTrack

Wir prüfen die Strecke. Hierzu wird mit Telemetrie geschaut, wann der nächste Zug kommt.

```csharp
private CheckRailwayTrackResult CheckRailwayTrack()
{
    var signal = new RailwaySignal();
    var seconds = signal.GetArrivalTimeInSeconds();
    if (seconds < 10)
    {
        return new CheckRailwayTrackResult(CheckRailwayTrackResultStatus.Unknown, "Unknown error");
    }
    if (seconds < 20)
    {
        return new CheckRailwayTrackResult(CheckRailwayTrackResultStatus.SensorFailure, "Could not check the track, no sensor data arrived");
    }
    if (seconds < 30)
    {
        return new CheckRailwayTrackResult(CheckRailwayTrackResultStatus.Occupied, "Track is occupied by train");
    }

    return new CheckRailwayTrackResult(DateTimeOffset.Now.AddSeconds(seconds));
}
```

Auch hier wird eine Variant Struktur verwendet. Somit haben wir folgenden Code der als nächster ausgeführt wird.

```csharp
var checkTrackResult = CheckRailwayTrack();
switch (checkTrackResult.Status)
{
    case CheckRailwayTrackResultStatus.Free:
        throw new ArgumentOutOfRangeException();
    case CheckRailwayTrackResultStatus.Occupied:
    case CheckRailwayTrackResultStatus.SensorFailure:
    case CheckRailwayTrackResultStatus.Unknown:
        return checkTrackResult.ErrorMessage;
    default:
        throw new ArgumentOutOfRangeException();
}
```

Wen die Strecke frei ist, dann kann die Weiche gestellt werden.

```csharp
private SetSwitchGroupResult SetDirection(SwitchDirection switchDirection, DateTimeOffset estimatedTimeOfArrival)
{
    var switchGroup = new SwitchGroup();
    var res = switchGroup.Set(switchDirection, estimatedTimeOfArrival);
    return res;
}
```

Somit haben wir im Gut-Fall:

```csharp
var setSwitchGroupResult = SetDirection(cmd.Direction, checkTrackResult.EstimatedArrivalTimeOfNextTrain);
switch (setSwitchGroupResult.SwitchResult)
{
    case SwitchResult.Success:
        throw new NotImplementedException();
    case SwitchResult.SwitchIsStiff:
    case SwitchResult.TooShort:
    case SwitchResult.UnknownError:
        return setSwitchGroupResult.ErrorMessage;
    default:
        throw new ArgumentOutOfRangeException();
}
```
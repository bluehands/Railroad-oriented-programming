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

## Weiche Stellen (Traditionell)

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

Somit haben wir im Gutfall:

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

Und jetzt haben wir die Möglichkeit, das erfolgreiche Schalten der Weiche zu auditieren.

```csharp
private void SetAudit(Operator? @operator, SwitchDirection direction)
{
    AuditLog.Info($"{@operator?.Name} has set the switch direction to {direction}");
}
```

Und wir geben als Ergebnis eine Leere Zeichenfolge, als Zeichen, dass kein Fehler passiert ist zurück.

```csharp
SetAudit(operatorResult.Operator, cmd.Direction);
return string.Empty;
```

> **Unser Code zeigt mehrere Probleme:**
>
> * Bei den Variant-Typen ist nicht ersichtlich, was ein gültiger Zustand ist. Der Compiler kann uns nicht helfen
> * Der Code ist unübersichtlich, weil der Happy-Path in den Switch-Cases versteckt ist
> * Wir haben zwei Belange vermischt: Wie verhält sich das Program im Gutfall und wie im Fehler-Fall.

## Option & Result

Wir führen jetzt einen **Result&lt;T&gt;** ein. Ein Type der das Ergebnis einer Operation ist. Im Gutfall ist das Ergebnis **T**, der Einfachheit halber im Fehlerfall **string**. Weiter unten werden wir eigene Fehlertypen einführen.

Hierzu benötigen wir das Packet Funicular Switch von Alexander Wiedemann. Das Projekt ist hier: <https://github.com/bluehands/Funicular-Switch>. Die Binaries sind als Nuget-Packet verfügbar: <https://www.nuget.org/packages/FunicularSwitch>.

Wir beginnen mit **CheckRailwayTrack**. Statt einem Variant-Typen geben wir **Result<DateTimeOffset>** zurück.

```csharp
private Result<DateTimeOffset> CheckRailwayTrack()
{
    var signal = new RailwaySignal();
    var seconds = signal.GetArrivalTimeInSeconds();
    if (seconds < 10)
    {
        return Result.Error<DateTimeOffset>("Unknown error");
    }
    if (seconds < 20)
    {
        return Result.Error<DateTimeOffset>("Could not check the track, no sensor data arrived");
    }
    if (seconds < 30)
    {
        return Result.Error<DateTimeOffset>("Track is occupied by train");

    }
    return Result.Ok(DateTimeOffset.Now.AddSeconds(seconds));
}
```

Es gibt auch eine Überladung, so dass man direkt das Ergebnis des Gutfalls zurück geben kann.

```csharp
return DateTimeOffset.Now.AddSeconds(seconds);
```

Im nächsten Schritt bauen wir **SetDirection** um. Hier haben wir im Gutfall kein Ergebnis. Da **void** nicht als generischer Parameter verwendet werden kann, wird analog zu F# **Unit** als "Nichts" verwendet.

```csharp
private Result<Unit> SetDirection(SwitchDirection switchDirection, DateTimeOffset estimatedTimeOfArrival)
{
    var switchGroup = new SwitchGroup();
    var res = switchGroup.Set(switchDirection, estimatedTimeOfArrival);
    if (res.SwitchResult != SwitchResult.Success)
    {
        return Result.Error<Unit>(res.ErrorMessage);
    }
    return Result.Ok(No.Thing);
}
```

oder auch wieder direkt

```csharp
return No.Thing;
```

Um von einer Operation zur nächsten zu kommen, gibt es **Map** und **Bind**. Map schachtelt das Ergebnis wieder in ein Result. Bind ist ein "flach geklopftes Bind". Die Namen kommen von F#, in anderen Sprachen heißt Bind "Flat Map". Um aus der Continuation auszusteigen, wird **Match** verwendet. Das ist ein **switch** über den Gut- und Schlechtfall.

Somit haben wir folgenden Code im "ValidationResult.Valid"-Case:

```fsharp
return CheckRailwayTrack().Bind<Unit>(eta =>
{
    return SetDirection(cmd.Direction, eta).Bind<Unit>(_ =>
    {
        SetAudit(operatorResult.Operator, cmd.Direction);
        return No.Thing;
    });
})
.Match(_ => string.Empty, e => e);
```

Störend ist immer noch die Verwendung von Variant-Typen. Wir wandeln diese in **Union Type** um.

## Union Types

Die Idee ist, eine Enumeration in einen Union-Type umzuwandeln. Hier zu verwenden wir das Projekt Switchyard <https://github.com/bluehands/Switchyard> als eine Visual Studio Erweiterung. Diese ist im Marketplace verfügbar: <https://marketplace.visualstudio.com/items?itemName=bluehands.Switchyard-Refactoring&ssr=false>.

Für eine klarere Bezeichnung brennen wir **ValidationResult** in **OperatorInformation** um. Switchyard bietet ein Befehl **Expand enum to union type** an. Somit erhalten wir einen Union Type.

Im gültigen Fall bei **Valid** möchten wir den Operator Name mitgeben. Hierzu refaktorieren wir mit ReSharper und führen noch mal **Expand enum to union type** aus.

Unsere **CertificateParser:GetOperatorFromCertificate** Implementierung sieht jetzt folgendermaßen aus:

```csharp
public static OperatorInformation GetOperatorFromCertificate(X509Certificate2 cert)
{
    var validator = new X509CertificateValidator(cert);
    if (validator.IsExpired())
    {
        return OperatorInformation.Expired;
    }
    if (validator.IsNotYetValid())
    {
        return OperatorInformation.NotYetValid;
    }
    if (validator.IsRevoked())
    {
        return OperatorInformation.Revoked;
    }
    if (!validator.IsTrusted())
    {
        return OperatorInformation.NotTrusted;
    }
    return OperatorInformation.Valid(validator.GetOperator());
}
```

Beim konsumieren verwenden wir jetzt Pattern-Matching. Unser switch sieht nun folgendermaßen aus:

```csharp
var operatorInfo = CertificateParser.GetOperatorFromCertificate(cmd.SigningCert);
switch (operatorInfo)
{
    case OperatorInformation.Expired_ expired:
    case OperatorInformation.NotTrusted_ notTrusted:
    case OperatorInformation.NotYetValid_ notYetValid:
    case OperatorInformation.Revoked_ revoked:
        return Result.Error<Unit>(operatorInfo.ToString());
    case OperatorInformation.Valid_ valid:
        return CheckRailwayTrack().Bind<Unit>(eta =>
            {
                return SetDirection(cmd.Direction, eta).Bind<Unit>(_ =>
                {
                    SetAudit(valid.Name, cmd.Direction);
                    return No.Thing;
                });
            })
            .Match(_ => string.Empty, e => e);
    default:
        throw new ArgumentOutOfRangeException(nameof(operatorInfo));
}
```

>Wir **erweitern jetzt die Fachlichkeit** aus der Praxis, in dem wir die Weiche trotzdem schalten möchten, wenn wir die aktuelle Revokationsliste nicht downloaden können. D.h. wenn die CRL nicht verfügbar ist, soll die Weiche trotzdem schalten.

>Wir sehen, dass der Code ist ungeschützt gegen Erweiterung des Switches ist. Bevor wir die Fachlichkeit erweitern, gehen wir dieses Thema an.

## Match

Der Union-Type von Switchyard bringt seinen einen "switch" mit. Das ist die **Match**-Methode.

```csharp
operatorInfo.Match(
valid => { },
expired => { },
notYetValid => { },
notTrusted => { },
revoked => { });
```

Wir lagern diesen "switch" in eine eigene Erweiterungs-Methode aus:

```csharp
public static class OperatorInfoExtension
{
    public static Result<string> ToResult(this OperatorInformation info)
    {
        return info.Match(
            valid: valid => Result<string>.Ok(valid.Name),
            expired: _ => Result<string>.Error(info.ToString()),
            notYetValid: _ => Result<string>.Error(info.ToString()),
            revoked: _ => Result<string>.Error(info.ToString()),
            notTrusted: _ => Result<string>.Error(info.ToString()));
    }
}
```

Um DRY zu berücksichtigen, gestallten wir es noch um:

```csharp
public static Result<string> ToResult(this OperatorInformation info)
{
    Result<string> Error(OperatorInformation err) => Result<string>.Error(err.ToString());

    return info.Match(
        valid: valid => Result<string>.Ok(valid.Name),
        expired: Error,
        notYetValid: Error,
        revoked: Error,
        notTrusted: Error);
}
```
> Wenn wir jetzt die Enumeration erweitern und z.B. **NoCrlAvailable** hinzufügen, dann bekommen wir automatisch Compiler-Errors und keine Laufzeitfehler. 

Da wir jetzt durchgängig **Result&lt;T&gt;** verwenden, können wir auch den Rückgabetype von Set in **Result&lt;string&gt;** umwandeln. Unsere Set-Methode sieht jetzt folgendermaßen aus:

```csharp
return CertificateParser.
    GetOperatorFromCertificate(cmd.SigningCert).ToResult().Bind(operatorName =>
    CheckRailwayTrack().Bind(eta =>
    {
        return SetDirection(cmd.Direction, eta).Bind<string>(_ =>
        {
            SetAudit(operatorName, cmd.Direction);
            return string.Empty;
        });
    })
);
```

Das Interpretieren des Ergebnisses ist jetzt beim Konsumenten:

```csharp
var railroadSwitch = new RailroadSwitch();
railroadSwitch.Set(cmd).Match(
    _ => Console.WriteLine("Successfully set the switch"),
    e => Console.WriteLine($"Error set the switch {e}"));
```

Bis jetzt haben wir die Fehler als Zeichenkette verpackt. Was wir benötigen, sind aber Fehler im Kontext unserer Domäne. Hierzu lassen wir uns die entsprechenden Typen generieren.

## Result mit eigenen Fehlertyp

Hierzu verwenden wir den **SourceGenerator** von Funicular-Switch über das Nuget-Packet <https://www.nuget.org/packages/FunicularSwitch.Generators/>. Dieser Generator kann die entsprechenden Methoden Bind, Map, Match und andere erzeugen.

Als erstes erzeugen wir uns einen **Union-Type** für die Domänen-Fehler mit Switchyard. Wir erzeugen einen **enum** mit erstmal einem Wert **UntrustedOperator** und erweitern ihn zu einem Union Type.

Den Union Type **UntrustedOperator** erzeugen wir mit **OperatorInformation**

```csharp
public static Failure UntrustedOperator(OperatorInformation operatorInformation) => new UntrustedOperator_(operatorInformation);
```

Danach definieren wir unseren **Fehlertyp** als **partielle Klasse**.

```csharp
[ResultType(ErrorType = typeof(Failure))]
public partial class CustomResult<T>
{

}
```

Jetzt können wir nach und nach statt **Result&lt;T&gt;** **CustomResult&lt;T&gt;** verwenden. Die Fehlermeldung erweitern wir zu einem Failure mit entsprechenden Typ.

Somit haben wir folgenden Code:

```csharp
[FunicularSwitch.Generators.UnionType(CaseOrder = CaseOrder.AsDeclared)]
public abstract class Failure
{
    public static Failure UntrustedOperator(OperatorInformation operatorInformation) => new UntrustedOperator_(operatorInformation);
    public static Failure SwitchError(string errorMessage) => new SwitchError_(errorMessage);
    public static Failure TelemetryError(string errorMessage) => new TelemetryError_(errorMessage);
    public static Failure TrackOccupied(TimeSpan eta) => new TrackOccupied_(eta);

    public class UntrustedOperator_ : Failure
    {
        public OperatorInformation OperatorInformation { get; }

        public UntrustedOperator_(OperatorInformation operatorInformation) : base(UnionCases.UntrustedOperator)
        {
            OperatorInformation = operatorInformation;
        }
    }

    public class SwitchError_ : Failure
    {
        public string ErrorMessage { get; }

        public SwitchError_(string errorMessage) : base(UnionCases.SwitchError)
        {
            ErrorMessage = errorMessage;
        }
    }

    public class TelemetryError_ : Failure
    {
        public string ErrorMessage { get; }

        public TelemetryError_(string errorMessage) : base(UnionCases.TelemetryError)
        {
            ErrorMessage = errorMessage;
        }
    }

    public class TrackOccupied_ : Failure
    {
        public TimeSpan Eta { get; }

        public TrackOccupied_(TimeSpan eta) : base(UnionCases.TrackOccupied)
        {
            Eta = eta;
        }
    }

    internal enum UnionCases
    {
        UntrustedOperator,
        SwitchError,
        TelemetryError,
        TrackOccupied
    }

    internal UnionCases UnionCase { get; }
    Failure(UnionCases unionCase) => UnionCase = unionCase;

    public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
    bool Equals(Failure other) => UnionCase == other.UnionCase;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Failure)obj);
    }

    public override int GetHashCode() => (int)UnionCase;
}
```

```csharp
private CustomResult<DateTimeOffset> CheckRailwayTrack()
{
    var signal = new RailwaySignal();
    var seconds = signal.GetArrivalTimeInSeconds();
    if (seconds < 10)
    {
        return CustomResult.Error<DateTimeOffset>(Failure.TelemetryError("Unknown error"));
    }
    if (seconds < 20)
    {
        return CustomResult.Error<DateTimeOffset>(Failure.TelemetryError("Could not check the track, no sensor data arrived"));
    }
    if (seconds < 30)
    {
        return CustomResult.Error<DateTimeOffset>(Failure.TrackOccupied(TimeSpan.FromSeconds(seconds)));

    }
    return DateTimeOffset.Now.AddSeconds(seconds);
}
```

```csharp
    private CustomResult<Unit> SetDirection(SwitchDirection switchDirection, DateTimeOffset estimatedTimeOfArrival)
    {
        var switchGroup = new SwitchGroup();
        var res = switchGroup.Set(switchDirection, estimatedTimeOfArrival);
        if (res.SwitchResult != SwitchResult.Success)
        {
            return CustomResult.Error<Unit>(Failure.SwitchError(res.ErrorMessage));
        }
        return No.Thing;
    }
```

```csharp
public CustomResult<string> Set(SetCommand cmd)
{
    return CertificateParser.
        GetOperatorFromCertificate(cmd.SigningCert).ToCustomResult().Bind(operatorName =>
            CheckRailwayTrack().Bind(eta =>
            {
                return SetDirection(cmd.Direction, eta).Bind<string>(_ =>
                {
                    SetAudit(operatorName, cmd.Direction);
                    return string.Empty;
                });
            })
        );
}
```

Wenn wir die **Set** anschauen, kann man sich die Frage stellen, warum das Ergebnis der Methode ein **string** ist? Bei genauer Betrachtung kann man erkennen, dass Audit eigentlich mit der **Set**-Operation nichts zu tun hat. Es ist "Seiteneffekt". **SetAudit** muss vom Konsumenten aufgerufen werden. Somit können wir als Ergebnis der Operation den neuen Zustand der Weiche zurückgeben und es dann auditieren oder eine Fehlermeldung loggen.

```csharp
public CustomResult<SwitchInfo> Set(SetCommand cmd)
{
    return CertificateParser.
        GetOperatorFromCertificate(cmd.SigningCert).ToCustomResult().Bind(operatorName =>
            CheckRailwayTrack().Bind(eta =>
            {
                return SetDirection(cmd.Direction, eta).Bind<SwitchInfo>(_ => 
                    new SwitchInfo(operatorName,cmd.Direction));
            })
        );
}
```

Und beim Konsumenten

```csharp
railroadSwitch.Set(cmd).Match(
    info => Audit.Log(info.OperatorName, info.Direction),
    e => Console.WriteLine($"Error set the switch: {e}")
);
```

## Parallele Ausführung und Aggregation

Bei der **Set-Methode** kann man erkennen, dass die **GetOperatorFromCertificate** und **CheckRailwayTrack** Operation unabhängig voneinander durchgeführt werden können. D.h. diese können parallel ausgeführt und deren Ergebnisse dann aggregiert werden.

Hierfür müssen wir unseren **CustomResult** dahingehend erweitern, dass er mehrere Failure aggregieren kann.

Wir erweitern Failure um ein **Aggregate** mit einer Liste von Failure-Einträgen.

```csharp
public static Failure Aggregated(IReadOnlyList<Failure> innerFailures) => new Aggregated_(innerFailures);
```

Wir müssen darüber hinaus das **Mergen** definieren. Funicular-Switch erwartet eine Methode mit dem Attribut **MergeError**.

```csharp
public static class FailureExtension
{
    [MergeError]
    public static Failure Merge(this Failure error, Failure other)
    {
        return Failure.Aggregated(new[] { error, other });
    }
}
```

Somit haben wir schließlich:

```csharp
public CustomResult<SwitchInfo> Set(SetCommand cmd)
{
    var result1 = CertificateParser.GetOperatorFromCertificate(cmd.SigningCert).ToCustomResult();
    var result2 = CheckRailwayTrack();
    var aggregatedResult = result1.Aggregate(result2, (operatorName, eta) => (operatorName, eta));
    return aggregatedResult.Bind(t =>
            {
                return SetDirection(cmd.Direction, t.eta).Bind<SwitchInfo>(_ =>
                    new SwitchInfo(t.operatorName, cmd.Direction));
            });
}
```

> Der zweite Parameter **Combine** dient dazu, das Ergebnis-Tuple mit benannten Argumente zu versehen.

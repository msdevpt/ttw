namespace Ttw.Core.Ttbins;

/// <summary>
/// The record tags, statuses and activity types used in a TTBIN file.
/// </summary>
public static class TtbinTags
{
    /// <summary>File header record.</summary>
    public const byte TagFileHeader = 0x20;

    /// <summary>Status record: the activity state and type.</summary>
    public const byte TagStatus = 0x21;

    /// <summary>GPS record: one position sample.</summary>
    public const byte TagGps = 0x22;

    /// <summary>Precision record: EHPE, EVPE and HDOP.</summary>
    public const byte TagPrecision = 0x23;

    /// <summary>Heart rate record.</summary>
    public const byte TagHeartRate = 0x25;

    /// <summary>Summary record: type, distance, duration and calories.</summary>
    public const byte TagSummary = 0x27;

    /// <summary>Pool size record.</summary>
    public const byte TagPoolSize = 0x2a;

    /// <summary>Wheel size record.</summary>
    public const byte TagWheelSize = 0x2b;

    /// <summary>Training setup record.</summary>
    public const byte TagTrainingSetup = 0x2d;

    /// <summary>Lap record.</summary>
    public const byte TagLap = 0x2f;

    /// <summary>Time to satellite fix record.</summary>
    public const byte TagTimeToFix = 0x30;

    /// <summary>Cycling cadence record.</summary>
    public const byte TagCyclingCadence = 0x31;

    /// <summary>Treadmill record.</summary>
    public const byte TagTreadmill = 0x32;

    /// <summary>Swim record.</summary>
    public const byte TagSwim = 0x34;

    /// <summary>Goal progress record.</summary>
    public const byte TagGoalProgress = 0x35;

    /// <summary>Unknown record.</summary>
    public const byte Tag37 = 0x37;

    /// <summary>Interval setup record.</summary>
    public const byte TagIntervalSetup = 0x39;

    /// <summary>Interval start record.</summary>
    public const byte TagIntervalStart = 0x3a;

    /// <summary>Interval finish record.</summary>
    public const byte TagIntervalFinish = 0x3b;

    /// <summary>Race setup record.</summary>
    public const byte TagRaceSetup = 0x3c;

    /// <summary>Race result record.</summary>
    public const byte TagRaceResult = 0x3d;

    /// <summary>Altitude update record.</summary>
    public const byte TagAltitudeUpdate = 0x3e;

    /// <summary>Heart rate recovery record.</summary>
    public const byte TagHeartRateRecovery = 0x3f;

    /// <summary>Indoor cycling record.</summary>
    public const byte TagIndoorCycling = 0x40;

    /// <summary>Gym record.</summary>
    public const byte TagGym = 0x41;

    /// <summary>Movement record.</summary>
    public const byte TagMovement = 0x42;

    /// <summary>Route description record.</summary>
    public const byte TagRouteDescription = 0x44;

    /// <summary>Elevation record.</summary>
    public const byte TagElevation = 0x47;

    /// <summary>Unknown record.</summary>
    public const byte Tag48 = 0x48;

    /// <summary>Battery record.</summary>
    public const byte TagBattery = 0x49;

    /// <summary>Fitness points record.</summary>
    public const byte TagFitnessPoints = 0x4a;

    /// <summary>Unknown variable length record.</summary>
    public const byte Tag4b = 0x4b;

    /// <summary>Workout record, variable length.</summary>
    public const byte TagWorkout = 0x4c;

    /// <summary>The watch is ready.</summary>
    public const byte StatusReady = 0x00;

    /// <summary>The watch is recording.</summary>
    public const byte StatusActive = 0x01;

    /// <summary>The watch is paused.</summary>
    public const byte StatusPaused = 0x02;

    /// <summary>The watch stopped recording.</summary>
    public const byte StatusStopped = 0x03;
}

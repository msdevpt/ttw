namespace Ttw.Core.Activities;

/// <summary>
/// A single GPS sample of an activity.
/// </summary>
public sealed class ActivityRecord
{
    /// <summary>Value used for every field that is not (yet) known.</summary>
    public const int Invalid = -9999;

    /// <summary>Timestamp of the sample, in UTC. Null when the watch did not record one.</summary>
    public DateTimeOffset? DateTime { get; set; }

    /// <summary>Latitude in degrees.</summary>
    public double Latitude { get; set; } = Invalid;

    /// <summary>Longitude in degrees.</summary>
    public double Longitude { get; set; } = Invalid;

    /// <summary>Latitude before any (optional) smoothing was applied.</summary>
    public double RawLatitude { get; set; } = Invalid;

    /// <summary>Longitude before any (optional) smoothing was applied.</summary>
    public double RawLongitude { get; set; } = Invalid;

    /// <summary>Average speed over the sample period, in m/s.</summary>
    public double Speed { get; set; } = Invalid;

    /// <summary>Heading in degrees.</summary>
    public double Heading { get; set; } = Invalid;

    /// <summary>Calories burned, in kCal.</summary>
    public double Calories { get; set; } = Invalid;

    /// <summary>Cumulative distance since the start of the activity, in metres.</summary>
    public double Distance { get; set; } = Invalid;

    /// <summary>Instantaneous speed at the sample time, in m/s.</summary>
    public double InstantSpeed { get; set; } = Invalid;

    /// <summary>Barometric elevation, in metres.</summary>
    public double Elevation1 { get; set; } = Invalid;

    /// <summary>GPS elevation, in metres, calibrated around 0.</summary>
    public double Elevation2 { get; set; } = Invalid;

    /// <summary>Combined elevation, in metres. Filled in by the elevation correction.</summary>
    public double DerivedElevation { get; set; } = Invalid;

    /// <summary>Cumulative elevation gain, in metres.</summary>
    public double Ascend { get; set; } = Invalid;

    /// <summary>Cumulative elevation loss, in metres.</summary>
    public double Descend { get; set; } = Invalid;

    /// <summary>Number of cycles: steps when running, crank rotations when cycling.</summary>
    public int Cycles { get; set; } = Invalid;

    /// <summary>Battery level at the time of the sample, in percent.</summary>
    public int BatteryLevel { get; set; } = Invalid;

    /// <summary>Heart rate, in bpm.</summary>
    public int HeartRate { get; set; } = Invalid;

    /// <summary>Temperature, in degrees Celsius.</summary>
    public int Temperature { get; set; } = Invalid;

    /// <summary>Estimated vertical precision error, in cm.</summary>
    public int Evpe { get; set; } = Invalid;

    /// <summary>Estimated horizontal precision error, in cm.</summary>
    public int Ehpe { get; set; } = Invalid;

    /// <summary>Horizontal dilution of precision.</summary>
    public int Hdop { get; set; } = Invalid;

    /// <summary>Cumulative fitness points of the day.</summary>
    public int FitnessPoints { get; set; } = Invalid;

    /// <summary>Movement state: 0 standing still, 1 reduced speed, 2 moving.</summary>
    public int MovementState { get; set; } = Invalid;

    /// <summary>Elevation status byte.</summary>
    public int ElevationStatus { get; set; } = Invalid;

    /// <summary>Unknown sensor byte 1.</summary>
    public int UnknownInt1 { get; set; }

    /// <summary>Unknown sensor byte 2.</summary>
    public int UnknownInt2 { get; set; }

    /// <summary>Unknown sensor byte 3.</summary>
    public int UnknownInt3 { get; set; }

    /// <summary>Unknown sensor byte 4.</summary>
    public int UnknownInt4 { get; set; }

    /// <summary>Unknown sensor byte 5.</summary>
    public int UnknownInt5 { get; set; }

    /// <summary>Unknown sensor byte 6.</summary>
    public int UnknownInt6 { get; set; }

    /// <summary>Unknown sensor value 1.</summary>
    public float UnknownFloat1 { get; set; }

    /// <summary>Unknown sensor value 2.</summary>
    public float UnknownFloat2 { get; set; }
}

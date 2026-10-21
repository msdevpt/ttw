using Ttw.Core.Ttbins;

namespace Ttw.Core.Activities;

/// <summary>
/// A decoded activity, as stored in a TTBIN file on the watch.
/// </summary>
public sealed class Activity
{
    /// <summary>The header of the TTBIN file the activity was read from.</summary>
    public TtbinHeader Header { get; set; } = new();

    /// <summary>Activity type reported by the summary record.</summary>
    public int SummaryType { get; set; }

    /// <summary>Activity type reported by the status records.</summary>
    public int SummaryActivity { get; set; }

    /// <summary>Duration of the activity, in seconds.</summary>
    public int SummaryDuration { get; set; }

    /// <summary>Calories burned during the activity.</summary>
    public int SummaryCalories { get; set; }

    /// <summary>Distance of the activity, in metres, as reported by the watch.</summary>
    public double SummaryDistance { get; set; }

    /// <summary>Battery level at the end of the activity, in percent.</summary>
    public int BatteryLevel { get; set; }

    /// <summary>
    /// Difference between the watch local time and UTC, in seconds, as stored in
    /// the file header. Negative values are kept as they are; the effective
    /// offset collapses to UTC in that case.
    /// </summary>
    public int TimeZoneSeconds { get; set; }

    /// <summary>
    /// Effective offset between the watch local time and UTC, in seconds. The
    /// watch reports whole hours only; a negative offset degenerates to UTC.
    /// </summary>
    public int LocalOffsetSeconds { get; set; }

    /// <summary>Display name of the time zone of the watch.</summary>
    public string TimeZoneDisplayName { get; set; } = "GMT";

    /// <summary>Fitness points at the start of the activity, -1 when unknown.</summary>
    public int FitnessPointsStart { get; set; } = -1;

    /// <summary>Fitness points at the end of the activity, -1 when unknown.</summary>
    public int FitnessPointsEnd { get; set; } = -1;

    /// <summary>Name of the workout that was performed, null when there was none.</summary>
    public string? Workout { get; set; }

    /// <summary>Description of the workout that was performed, null when there was none.</summary>
    public string? WorkoutDescription { get; set; }

    /// <summary>Name of the planned route that was followed.</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Time the watch needed to get a satellite fix, in seconds.</summary>
    public int SecondsToFix { get; set; }

    /// <summary>The track segments of the activity.</summary>
    public List<ActivitySegment> Segments { get; } = [];

    /// <summary>The waypoints marked during the activity.</summary>
    public List<ActivityRecord> Waypoints { get; } = [];

    /// <summary>Name or id of the device the activity was recorded with.</summary>
    public string DeviceName { get; set; } = "TomTom";

    /// <summary>Serial number of the device the activity was recorded with.</summary>
    public string DeviceSerialNumber { get; set; } = "Unknown";

    /// <summary>The activity type of this activity.</summary>
    public int ActivityType => SummaryActivity;

    /// <summary>The human readable description of the activity type.</summary>
    public string ActivityDescription => ActivityTypes.Describe(SummaryActivity);

    /// <summary>The number of track segments.</summary>
    public int SegmentCount => Segments.Count;

    /// <summary>
    /// The moment the activity started, in UTC, derived from the start time in
    /// the file header.
    /// </summary>
    public DateTimeOffset StartInstantUtc =>
        DateTimeOffset.FromUnixTimeSeconds(unchecked((long)Header.StartTime) - TimeZoneSeconds);

    /// <summary>
    /// The moment the activity started, as a wall clock time in the time zone of
    /// the watch.
    /// </summary>
    public DateTime StartDateTime =>
        DateTime.SpecifyKind(StartInstantUtc.UtcDateTime.AddSeconds(LocalOffsetSeconds), DateTimeKind.Unspecified);

    /// <summary>
    /// The wall clock time of the first recorded sample, in the time zone of the
    /// watch, or null when the activity holds no samples.
    /// </summary>
    public DateTime? FirstActiveRecordTime
    {
        get
        {
            if (Segments.Count == 0 || Segments[0].Records.Count == 0)
            {
                return null;
            }

            DateTimeOffset? first = Segments[0].Records[0].DateTime;
            if (first is null)
            {
                return null;
            }

            // Correct for a time zone offset that is not a multiple of half an hour.
            int correction = TimeZoneSeconds % 1800;
            if (correction > 900)
            {
                correction -= 1800;
            }

            return DateTime.SpecifyKind(
                first.Value.UtcDateTime.AddSeconds(correction).AddSeconds(LocalOffsetSeconds),
                DateTimeKind.Unspecified);
        }
    }

    /// <summary>The distance covered, in km, computed from the track.</summary>
    public double DistanceKm
    {
        get
        {
            double distance = 0.0;
            foreach (ActivitySegment segment in Segments)
            {
                distance += segment.DistanceKm;
            }

            return distance;
        }
    }

    /// <summary>The total number of cycles: steps when running, crank rotations when cycling.</summary>
    public int Cycles
    {
        get
        {
            int cycles = 0;
            foreach (ActivitySegment segment in Segments)
            {
                cycles += segment.Cycles;
            }

            return cycles;
        }
    }

    /// <summary>The number of cycles per minute, 0 when the duration is unknown.</summary>
    public int Pace
    {
        get
        {
            long duration = 0;
            foreach (ActivitySegment segment in Segments)
            {
                duration += segment.DurationSeconds;
            }

            if (duration == 0)
            {
                return 0;
            }

            return (int)Math.Floor((Cycles * 60.0 + 0.5) / duration + 0.5);
        }
    }

    /// <summary>The fitness points earned during the activity, -1 when undefined.</summary>
    public int FitnessPoints =>
        FitnessPointsEnd >= 0 && FitnessPointsStart >= 0
            ? FitnessPointsEnd - FitnessPointsStart
            : -1;

    /// <summary>True when at least one sample carries an elevation value.</summary>
    public bool HasHeightValues
    {
        get
        {
            foreach (ActivitySegment segment in Segments)
            {
                foreach (ActivityRecord record in segment.Records)
                {
                    if (record.Elevation1 != ActivityRecord.Invalid
                        || record.Elevation2 != ActivityRecord.Invalid)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Sets the effective local offset and its display name from the raw offset
    /// stored in the file header. The watch only reports whole hours; a negative
    /// offset collapses to UTC.
    /// </summary>
    public void SetTimeZoneDifference(int seconds)
    {
        TimeZoneSeconds = seconds;

        if (seconds >= 0)
        {
            int hours = (seconds + 1800) / 3600;
            LocalOffsetSeconds = hours * 3600;
            TimeZoneDisplayName = "GMT+" + hours.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + ":00";
        }
        else
        {
            LocalOffsetSeconds = 0;
            TimeZoneDisplayName = "GMT";
        }
    }

    /// <summary>
    /// Returns the wall clock time of a segment start in the time zone of the
    /// watch.
    /// </summary>
    public DateTime SegmentStartWallClock(ActivitySegment segment) =>
        segment.StartUtc is null
            ? default
            : DateTime.SpecifyKind(
                segment.StartUtc.Value.UtcDateTime.AddSeconds(LocalOffsetSeconds),
                DateTimeKind.Unspecified);
}

using Ttw.Core.Geography;

namespace Ttw.Core.Activities;

/// <summary>
/// A continuous part of an activity. Segments are separated by a pause/resume
/// of the watch.
/// </summary>
public sealed class ActivitySegment
{
    /// <summary>Value used when the heart rate recovery was not recorded.</summary>
    public const int HeartRateRecoveryUndefined = -9999;

    /// <summary>The samples of this segment, in recording order.</summary>
    public List<ActivityRecord> Records { get; } = [];

    /// <summary>
    /// Start of the segment as stored by the watch: the raw epoch seconds of the
    /// status record, which carry the watch local time. It is deliberately not
    /// converted to UTC; the writers add the local offset to obtain the wall
    /// clock time, exactly like the Java implementation does.
    /// </summary>
    public DateTimeOffset? StartUtc { get; set; }

    /// <summary>
    /// End of the segment as stored by the watch. See <see cref="StartUtc"/> for
    /// the local-time quirk.
    /// </summary>
    public DateTimeOffset? EndUtc { get; set; }

    /// <summary>Heart rate recovery in bpm/min, or <see cref="HeartRateRecoveryUndefined"/>.</summary>
    public int HeartRateRecovery { get; set; } = HeartRateRecoveryUndefined;

    /// <summary>Heart rate recovery score: 0 (none) up to 4 (excellent), -1 when unknown.</summary>
    public int HeartRateRecoveryScore { get; set; } = -1;

    /// <summary>
    /// Length of the segment in seconds. Zero when the segment was never closed.
    /// </summary>
    public long DurationSeconds
    {
        get
        {
            if (StartUtc is null)
            {
                return 0;
            }

            DateTimeOffset end = EndUtc ?? StartUtc.Value;
            return (long)(end - StartUtc.Value).TotalSeconds;
        }
    }

    /// <summary>
    /// Distance covered in this segment, in km. A sample without valid
    /// coordinates does not advance the previous point.
    /// </summary>
    public double DistanceKm
    {
        get
        {
            double distance = 0.0;
            ActivityRecord? previous = null;
            int index = 0;

            while (index < Records.Count && previous is null)
            {
                previous = Records[index];
                index++;
            }

            while (index < Records.Count)
            {
                ActivityRecord record = Records[index];
                if (HasCoordinates(record) && HasCoordinates(previous!))
                {
                    distance += GeoDistance.HaversineKm(
                        previous!.Latitude, previous.Longitude,
                        record.Latitude, record.Longitude);
                    previous = record;
                }

                index++;
            }

            return distance;
        }
    }

    /// <summary>Total number of cycles in this segment.</summary>
    public int Cycles
    {
        get
        {
            int cycles = 0;
            foreach (ActivityRecord record in Records)
            {
                cycles += record.Cycles;
            }

            return cycles;
        }
    }

    /// <summary>
    /// Adds a record unless it is equal to the previous one. Two records are
    /// equal when they carry the same timestamp; the raw coordinates are always
    /// equal because the watch never stores them.
    /// </summary>
    public void AddRecord(ActivityRecord record)
    {
        if (Records.Count > 0)
        {
            ActivityRecord last = Records[^1];
            if (last.DateTime == record.DateTime
                && last.RawLatitude == record.RawLatitude
                && last.RawLongitude == record.RawLongitude)
            {
                return;
            }
        }

        Records.Add(record);
    }

    private static bool HasCoordinates(ActivityRecord record) =>
        record.Latitude != ActivityRecord.Invalid
        && record.Longitude != ActivityRecord.Invalid;
}

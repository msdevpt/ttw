using System.Globalization;
using System.Xml.Linq;
using Ttw.Core.Activities;

namespace Ttw.Core.Exporting;

/// <summary>
/// Writes an activity as a Garmin Training Center XML file.
/// </summary>
public static class TcxWriter
{
    private static readonly XNamespace Tcx = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private const string SchemaLocation =
        "http://www.garmin.com/xmlschemas/ActivityExtension/v2 "
        + "https://www8.garmin.com/xmlschemas/ActivityExtensionv2.xsd "
        + "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 "
        + "https://www8.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd";

    /// <summary>
    /// Writes the activity as a TCX document.
    /// </summary>
    /// <param name="activity">The activity to write.</param>
    /// <param name="appName">Name of the application. It is not part of the output.</param>
    public static string Write(Activity activity, string appName)
    {
        ArgumentNullException.ThrowIfNull(activity);

        XElement root = new(
            Tcx + "TrainingCenterDatabase",
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
            new XAttribute(Xsi + "schemaLocation", SchemaLocation));

        XElement activityElement = new(
            Tcx + "Activity",
            new XAttribute("Sport", Sport(activity.ActivityType)));

        activityElement.Add(
            new XElement(Tcx + "Id", activity.StartDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture)));

        foreach (ActivitySegment segment in activity.Segments)
        {
            activityElement.Add(CreateLap(activity, segment));
        }

        root.Add(new XElement(Tcx + "Activities", activityElement));

        return XmlSerialization.Serialize(new XDocument(root));
    }

    private static XElement CreateLap(Activity activity, ActivitySegment segment)
    {
        XElement lap = new(
            Tcx + "Lap",
            new XAttribute("StartTime", activity.SegmentStartWallClock(segment).ToString(TimestampFormat, CultureInfo.InvariantCulture)));

        lap.Add(
            new XElement(
                Tcx + "TotalTimeSeconds",
                segment.DurationSeconds.ToString(CultureInfo.InvariantCulture)));
        lap.Add(
            new XElement(
                Tcx + "DistanceMeters",
                (segment.DistanceKm * 1000).ToString("F1", CultureInfo.InvariantCulture)));
        lap.Add(new XElement(Tcx + "Calories", "0"));
        lap.Add(new XElement(Tcx + "Intensity", "Active"));
        lap.Add(new XElement(Tcx + "TriggerMethod", "Manual"));

        XElement track = new(Tcx + "Track");

        foreach (ActivityRecord record in segment.Records)
        {
            if (!HasPosition(record))
            {
                continue;
            }

            track.Add(CreateTrackpoint(record));
        }

        lap.Add(track);

        return lap;
    }

    private static XElement CreateTrackpoint(ActivityRecord record)
    {
        XElement point = new(Tcx + "Trackpoint");

        if (record.DateTime is not null)
        {
            point.Add(
                new XElement(
                    Tcx + "Time",
                    record.DateTime.Value.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture)));
        }

        point.Add(
            new XElement(
                Tcx + "Position",
                new XElement(Tcx + "LatitudeDegrees", record.Latitude.ToString("F7", CultureInfo.InvariantCulture)),
                new XElement(Tcx + "LongitudeDegrees", record.Longitude.ToString("F7", CultureInfo.InvariantCulture))));

        if (record.DerivedElevation != ActivityRecord.Invalid)
        {
            point.Add(
                new XElement(
                    Tcx + "AltitudeMeters",
                    record.DerivedElevation.ToString("F1", CultureInfo.InvariantCulture)));
        }

        if (record.Distance != ActivityRecord.Invalid)
        {
            point.Add(
                new XElement(
                    Tcx + "DistanceMeters",
                    record.Distance.ToString("F1", CultureInfo.InvariantCulture)));
        }

        if (record.HeartRate != ActivityRecord.Invalid && record.HeartRate > 0)
        {
            point.Add(
                new XElement(
                    Tcx + "HeartRateBpm",
                    new XElement(Tcx + "Value", record.HeartRate.ToString(CultureInfo.InvariantCulture))));
        }

        return point;
    }

    private static string Sport(int activityType) => activityType switch
    {
        ActivityTypes.Running => "Running",
        ActivityTypes.TrailRunning => "Running",
        ActivityTypes.Cycling => "Biking",
        _ => "Other",
    };

    private static bool HasPosition(ActivityRecord record) =>
        record.DateTime is not null
        && record.Latitude != ActivityRecord.Invalid
        && record.Longitude != ActivityRecord.Invalid
        && record.Latitude != 0.0
        && record.Longitude != 0.0;
}

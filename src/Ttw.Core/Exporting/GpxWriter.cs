using System.Globalization;
using System.Xml.Linq;
using Ttw.Core.Activities;

namespace Ttw.Core.Exporting;

/// <summary>
/// Writes an activity as a GPX 1.1 file.
/// </summary>
public static class GpxWriter
{
    private static readonly XNamespace Gpx = "http://www.topografix.com/GPX/1/1";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace UGotMe = "http://tracklog.studioblueplanet.net/gpxextensions/v4";
    private static readonly XNamespace Gpxx = "http://www.garmin.com/xmlschemas/GpxExtensions/v3";
    private static readonly XNamespace Gpxtpx = "http://www.garmin.com/xmlschemas/TrackPointExtension/v1";

    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    /// <summary>
    /// Writes the activity as a GPX 1.1 document.
    /// </summary>
    /// <param name="activity">The activity to write.</param>
    /// <param name="appName">Name of the application, written into the file.</param>
    /// <param name="garminExtensions">Write the Garmin track point extensions.</param>
    /// <param name="uGotMeExtensions">Write the u-gotMe extensions.</param>
    public static string Write(Activity activity, string appName, bool garminExtensions, bool uGotMeExtensions)
    {
        ArgumentNullException.ThrowIfNull(activity);

        string creator = appName;
        if (garminExtensions && activity.HasHeightValues)
        {
            // Strava only uses the elevation data when the barometer is mentioned.
            creator += " with barometer";
        }

        XElement root = new(
            Gpx + "gpx",
            new XAttribute("creator", creator),
            new XAttribute("version", "1.1"),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName));

        if (uGotMeExtensions)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "u-gotMe", UGotMe.NamespaceName));
        }

        if (garminExtensions)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "gpxx", Gpxx.NamespaceName));
            root.Add(new XAttribute(XNamespace.Xmlns + "gpxtpx", Gpxtpx.NamespaceName));
        }

        root.Add(new XAttribute(Xsi + "schemaLocation", SchemaLocation(garminExtensions, uGotMeExtensions)));

        // The waypoints are direct children of the gpx element and come first.
        int waypointNumber = 0;
        foreach (ActivityRecord waypoint in activity.Waypoints)
        {
            root.Add(CreateWaypoint(waypoint, waypointNumber));
            waypointNumber++;
        }

        root.Add(CreateTrack(activity, appName, garminExtensions, uGotMeExtensions));

        if (uGotMeExtensions)
        {
            root.Add(CreateTrackExtensions(activity));
        }

        return XmlSerialization.Serialize(new XDocument(root));
    }

    private static string SchemaLocation(bool garminExtensions, bool uGotMeExtensions)
    {
        string location = "http://www.topografix.com/GPX/1/1 https://www.topografix.com/GPX/1/1/gpx.xsd";

        if (uGotMeExtensions)
        {
            location += " http://tracklog.studioblueplanet.net/gpxextensions/v4"
                + " https://tracklog.studioblueplanet.net/gpxextensions/v4/gpxextensions.xsd";
        }

        if (garminExtensions)
        {
            location += " http://www.garmin.com/xmlschemas/GpxExtensions/v3"
                + " https://www.garmin.com/xmlschemas/GpxExtensionsv3.xsd"
                + " http://www.garmin.com/xmlschemas/TrackPointExtension/v1"
                + " http://www.garmin.com/xmlschemas/TrackPointExtensionv1.xsd";
        }

        return location;
    }

    private static XElement CreateWaypoint(ActivityRecord waypoint, int number)
    {
        XElement element = new(
            Gpx + "wpt",
            new XAttribute("lat", Format(waypoint.Latitude, "F7")),
            new XAttribute("lon", Format(waypoint.Longitude, "F7")));

        if (waypoint.DateTime is not null)
        {
            element.Add(new XElement(Gpx + "time", FormatTimestamp(waypoint.DateTime.Value)));
        }

        element.Add(new XElement(Gpx + "name", string.Format(CultureInfo.InvariantCulture, "waypoint{0:00}", number)));
        element.Add(new XElement(Gpx + "desc", string.Format(CultureInfo.InvariantCulture, "waypoint{0:00}", number)));
        element.Add(new XElement(Gpx + "sym", "Waypoint"));
        element.Add(new XElement(Gpx + "ele", JavaNumber.ToString(waypoint.Elevation1)));

        return element;
    }

    private static XElement CreateTrack(
        Activity activity,
        string appName,
        bool garminExtensions,
        bool uGotMeExtensions)
    {
        string description = activity.ActivityDescription;

        XElement track = new(
            Gpx + "trk",
            new XElement(Gpx + "name", "Track - " + description),
            new XElement(
                Gpx + "desc",
                $"Created by: {appName}. Logged by: '{activity.DeviceName}' (serial: {activity.DeviceSerialNumber}). "
                + $"Logged as: {description}."));

        foreach (ActivitySegment segment in activity.Segments)
        {
            XElement segmentElement = new(Gpx + "trkseg");

            foreach (ActivityRecord record in segment.Records)
            {
                if (!HasPosition(record))
                {
                    continue;
                }

                segmentElement.Add(CreateTrackPoint(record, garminExtensions, uGotMeExtensions));
            }

            if (uGotMeExtensions)
            {
                XElement? segmentExtensions = CreateSegmentExtensions(segment);
                if (segmentExtensions is not null)
                {
                    segmentElement.Add(segmentExtensions);
                }
            }

            track.Add(segmentElement);
        }

        return track;
    }

    private static XElement CreateTrackPoint(
        ActivityRecord record,
        bool garminExtensions,
        bool uGotMeExtensions)
    {
        XElement point = new(
            Gpx + "trkpt",
            new XAttribute("lat", Format(record.Latitude, "F7")),
            new XAttribute("lon", Format(record.Longitude, "F7")));

        if (record.DerivedElevation != ActivityRecord.Invalid)
        {
            point.Add(new XElement(Gpx + "ele", Format(record.DerivedElevation, "F1")));
        }

        if (record.DateTime is not null)
        {
            point.Add(new XElement(Gpx + "time", FormatTimestamp(record.DateTime.Value)));
        }

        if (record.Hdop != ActivityRecord.Invalid)
        {
            point.Add(new XElement(Gpx + "hdop", record.Hdop.ToString(CultureInfo.InvariantCulture)));
        }

        if (garminExtensions || uGotMeExtensions)
        {
            XElement extensions = new(Gpx + "extensions");

            if (garminExtensions && record.HeartRate != ActivityRecord.Invalid && record.HeartRate > 0)
            {
                extensions.Add(
                    new XElement(
                        Gpxtpx + "TrackPointExtension",
                        new XElement(Gpxtpx + "hr", record.HeartRate.ToString(CultureInfo.InvariantCulture))));
            }

            if (uGotMeExtensions)
            {
                XElement trackPoint = new(UGotMe + "trackpointExtension");

                trackPoint.Add(new XElement(UGotMe + "speed", Format(record.Speed, "F2")));
                trackPoint.Add(new XElement(UGotMe + "course", Format(record.Heading, "F2")));

                if (record.Temperature != ActivityRecord.Invalid)
                {
                    trackPoint.Add(
                        new XElement(UGotMe + "temp", record.Temperature.ToString(CultureInfo.InvariantCulture)));
                }

                if (record.HeartRate != ActivityRecord.Invalid && record.HeartRate > 0)
                {
                    trackPoint.Add(
                        new XElement(UGotMe + "hr", record.HeartRate.ToString(CultureInfo.InvariantCulture)));
                }

                if (record.Ehpe != ActivityRecord.Invalid)
                {
                    trackPoint.Add(new XElement(UGotMe + "ehpe", record.Ehpe.ToString(CultureInfo.InvariantCulture)));
                }

                if (record.Evpe != ActivityRecord.Invalid)
                {
                    trackPoint.Add(new XElement(UGotMe + "evpe", record.Evpe.ToString(CultureInfo.InvariantCulture)));
                }

                if (record.Ascend != ActivityRecord.Invalid)
                {
                    trackPoint.Add(new XElement(UGotMe + "asc", Format(record.Ascend, "F1")));
                }

                if (record.Descend != ActivityRecord.Invalid)
                {
                    trackPoint.Add(new XElement(UGotMe + "desc", Format(record.Descend, "F1")));
                }

                extensions.Add(trackPoint);
            }

            point.Add(extensions);
        }

        return point;
    }

    private static XElement? CreateSegmentExtensions(ActivitySegment segment)
    {
        bool hasRecovery = segment.HeartRateRecovery != ActivitySegment.HeartRateRecoveryUndefined;
        bool hasScore = segment.HeartRateRecoveryScore >= 0;

        if (!hasRecovery && !hasScore)
        {
            return null;
        }

        XElement extension = new(UGotMe + "tracksegmentExtension");

        if (hasRecovery)
        {
            extension.Add(
                new XElement(
                    UGotMe + "hrRecovery",
                    segment.HeartRateRecovery.ToString(CultureInfo.InvariantCulture)));
        }

        if (hasScore)
        {
            extension.Add(
                new XElement(
                    UGotMe + "hrRecoveryScore",
                    segment.HeartRateRecoveryScore.ToString(CultureInfo.InvariantCulture)));
        }

        return new XElement(Gpx + "extensions", extension);
    }

    private static XElement CreateTrackExtensions(Activity activity)
    {
        XElement extension = new(UGotMe + "trackExtension");

        extension.Add(new XElement(UGotMe + "device", activity.DeviceName));
        extension.Add(new XElement(UGotMe + "activity", activity.ActivityDescription));

        if (activity.FitnessPoints != -1)
        {
            extension.Add(
                new XElement(
                    UGotMe + "tomtomFitnessPoints",
                    activity.FitnessPoints.ToString(CultureInfo.InvariantCulture)));
        }

        if (activity.ActivityDescription == "Running")
        {
            extension.Add(
                new XElement(UGotMe + "steps", activity.Cycles.ToString(CultureInfo.InvariantCulture)));
            extension.Add(
                new XElement(UGotMe + "pace_permin", activity.Pace.ToString(CultureInfo.InvariantCulture)));
        }

        if (activity.Workout is not null)
        {
            extension.Add(new XElement(UGotMe + "tomtomWorkout", activity.Workout));
            extension.Add(new XElement(UGotMe + "tomtomWorkoutDescription", activity.WorkoutDescription ?? string.Empty));
        }

        extension.Add(new XElement(UGotMe + "smoothing", "false"));
        extension.Add(new XElement(UGotMe + "compression", "false"));
        extension.Add(
            new XElement(UGotMe + "totalCycles", activity.Cycles.ToString(CultureInfo.InvariantCulture)));

        if (!string.IsNullOrEmpty(activity.Route))
        {
            extension.Add(new XElement(UGotMe + "routeName", activity.Route));
        }

        return new XElement(Gpx + "extensions", extension);
    }

    private static bool HasPosition(ActivityRecord record) =>
        record.DateTime is not null
        && record.Latitude != ActivityRecord.Invalid
        && record.Longitude != ActivityRecord.Invalid
        && record.Latitude != 0.0
        && record.Longitude != 0.0;

    private static string Format(double value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    private static string FormatTimestamp(DateTimeOffset instant) =>
        instant.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture);
}

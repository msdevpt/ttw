using System.Globalization;
using System.Text;
using Ttw.Core.Activities;

namespace Ttw.Core.Exporting;

/// <summary>
/// Writes an activity as comma separated values.
/// </summary>
public static class CsvWriter
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

    private static readonly string[] Columns =
    [
        "dateTime", "lat", "lon", "dist", "cycles", "heading", "speed", "intSpeed",
        "movement", "calories", "status", "ele1", "ele2", "eleCorr", "cumEle1", "cumEle2",
        "heartRate", "ehpe", "evpe", "hdop", "battery",
        "unknownInt1", "unknownInt2", "unknownInt3", "unknownInt4", "unknownInt5", "unknownInt6",
        "unknownFloat1", "unknownFloat2",
    ];

    /// <summary>
    /// Writes the activity as a CSV document.
    /// </summary>
    public static string Write(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        StringBuilder builder = new();

        builder.Append("Summary\n");
        builder.Append("Type, ").Append(activity.SummaryType.ToString(CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("Distance, ").Append(JavaNumber.ToString(activity.SummaryDistance)).Append('\n');
        builder.Append("Duration, ").Append(activity.SummaryDuration.ToString(CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("Calories, ").Append(activity.SummaryCalories.ToString(CultureInfo.InvariantCulture)).Append('\n');

        for (int index = 0; index < activity.Segments.Count; index++)
        {
            ActivitySegment segment = activity.Segments[index];

            builder.Append('\n');

            if (index == 0)
            {
                builder.Append(string.Join(", ", Columns)).Append('\n');
            }

            foreach (ActivityRecord record in segment.Records)
            {
                AppendRecord(builder, record);
            }
        }

        return builder.ToString();
    }

    private static void AppendRecord(StringBuilder builder, ActivityRecord record)
    {
        if (record.DateTime is not null)
        {
            builder.Append(record.DateTime.Value.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture));
        }
        else
        {
            builder.Append("----");
        }

        builder.Append(',').Append(JavaNumber.ToString(record.Latitude));
        builder.Append(',').Append(JavaNumber.ToString(record.Longitude));
        builder.Append(',').Append(JavaNumber.ToString(record.Distance));
        builder.Append(',').Append(record.Cycles.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(JavaNumber.ToString(record.Heading));
        builder.Append(',').Append(JavaNumber.ToString(record.Speed));
        builder.Append(',').Append(JavaNumber.ToString(record.InstantSpeed));
        builder.Append(',').Append(record.MovementState.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(JavaNumber.ToString(record.Calories));
        builder.Append(',').Append(record.ElevationStatus.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(JavaNumber.ToString(record.Elevation1));
        builder.Append(',').Append(JavaNumber.ToString(record.Elevation2));
        builder.Append(',').Append(JavaNumber.ToString(record.DerivedElevation));
        builder.Append(',').Append(JavaNumber.ToString(record.Ascend));
        builder.Append(',').Append(JavaNumber.ToString(record.Descend));
        builder.Append(',').Append(record.HeartRate.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.Ehpe.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.Evpe.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.Hdop.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.BatteryLevel.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.UnknownInt1.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.UnknownInt2.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.UnknownInt3.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.UnknownInt4.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.UnknownInt5.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(record.UnknownInt6.ToString(CultureInfo.InvariantCulture));
        builder.Append(',').Append(JavaNumber.ToString(record.UnknownFloat1));
        builder.Append(',').Append(JavaNumber.ToString(record.UnknownFloat2));
        builder.Append(",\n");
    }
}

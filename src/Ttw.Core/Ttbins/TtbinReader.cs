using System.Text;
using Ttw.Core.Activities;
using Ttw.Core.Geography;

namespace Ttw.Core.Ttbins;

/// <summary>
/// Reads a TTBIN file as it is stored on the watch and decodes it into an
/// <see cref="Activity"/>.
/// </summary>
public static class TtbinReader
{
    /// <summary>
    /// A pause shorter than this many milliseconds is marked as a waypoint
    /// instead of starting a new segment.
    /// </summary>
    private const int WaypointLogTimeoutMs = 5000;

    /// <summary>Timestamp value the watch uses for an invalid record.</summary>
    private const uint InvalidTimestamp = 0xFFFFFFFF;

    /// <summary>
    /// Reads a TTBIN file and returns the activity it holds. Throws when the
    /// file is malformed; the caller treats that as an undecodable file.
    /// </summary>
    public static Activity Read(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        (TtbinHeader header, int position) = ReadHeader(data);

        Activity activity = new() { Header = header };
        activity.SetTimeZoneDifference(unchecked((int)header.LocalTimeOffset));

        ReaderState state = new(activity);

        while (position < data.Length)
        {
            byte tag = data[position];
            position++;

            int length = header.GetLength(tag);
            byte[] record;

            if (length == TtbinHeader.VariableLength)
            {
                if (position + 2 > data.Length)
                {
                    break;
                }

                int variableLength = ReadUInt16(data, position);
                position += 2;
                length = variableLength + 1;

                if (position + length - 1 > data.Length)
                {
                    throw new InvalidDataException("Truncated variable length record in TTBIN file.");
                }

                record = new byte[length];
                record[0] = tag;
                Buffer.BlockCopy(data, position, record, 1, length - 1);
                position += length - 1;
            }
            else if (length > 0)
            {
                if (position + length - 1 > data.Length)
                {
                    throw new InvalidDataException("Truncated record in TTBIN file.");
                }

                record = new byte[length];
                record[0] = tag;
                Buffer.BlockCopy(data, position, record, 1, length - 1);
                position += length - 1;
            }
            else
            {
                // Unknown tag: the rest of the file cannot be interpreted.
                break;
            }

            state.ParseRecord(record);
        }

        if (activity.HasHeightValues)
        {
            CorrectElevation(activity);
        }

        return activity;
    }

    /// <summary>
    /// Reads the file header. All values are little endian.
    /// </summary>
    private static (TtbinHeader Header, int Position) ReadHeader(byte[] data)
    {
        TtbinHeader header = new();
        int position = 0;

        header.Tag = data[position++];
        header.FileVersion = ReadUInt16(data, position);
        position += 2;

        if (header.FileVersion <= 0x09)
        {
            header.FirmwareVersion = [data[position], data[position + 1], data[position + 2]];
            position += 3;
        }
        else
        {
            // Version 0x0A and later store the firmware version as three words.
            header.FirmwareVersion =
            [
                ReadUInt16(data, position),
                ReadUInt16(data, position + 2),
                ReadUInt16(data, position + 4),
            ];
            position += 6;
        }

        header.ProductId = ReadUInt16(data, position);
        position += 2;

        header.StartTime = ReadUInt32(data, position);
        position += 4;

        header.SoftwareVersion = ReadLatinString(data, position, 16);
        position += 16;

        header.GpsFirmwareVersion = ReadLatinString(data, position, 80);
        position += 80;

        header.WatchTime = ReadUInt32(data, position);
        position += 4;

        header.LocalTimeOffset = ReadUInt32(data, position);
        position += 4;

        header.Reserved = data[position++];
        header.RecordLengthCount = data[position++];

        for (int i = 0; i < header.RecordLengthCount; i++)
        {
            if (position + TtbinHeader.LengthItemLength > data.Length)
            {
                throw new InvalidDataException("Truncated record length table in TTBIN file.");
            }

            byte tag = data[position];
            int length = ReadUInt16(data, position + 1);
            position += TtbinHeader.LengthItemLength;
            header.RecordLengths.Add((tag, length));
        }

        return (header, position);
    }

    /// <summary>
    /// Calculates the corrected elevation. Elevation 1 is the absolute elevation
    /// and elevation 2 the relative one; the first samples of a segment are
    /// unreliable and are left out of the average.
    /// </summary>
    private static void CorrectElevation(Activity activity)
    {
        double sum1 = 0.0;
        double sum2 = 0.0;
        int count1 = 0;
        int count2 = 0;

        foreach (ActivitySegment segment in activity.Segments)
        {
            if (segment.Records.Count <= 5)
            {
                continue;
            }

            for (int i = 5; i < segment.Records.Count; i++)
            {
                ActivityRecord record = segment.Records[i];
                if (record.Elevation1 != ActivityRecord.Invalid)
                {
                    sum1 += record.Elevation1;
                    count1++;
                }

                if (record.Elevation2 != ActivityRecord.Invalid)
                {
                    sum2 += record.Elevation2;
                    count2++;
                }
            }
        }

        if (count1 == 0 || count2 == 0)
        {
            return;
        }

        double average1 = sum1 / count1;
        double average2 = sum2 / count2;

        foreach (ActivitySegment segment in activity.Segments)
        {
            foreach (ActivityRecord record in segment.Records)
            {
                record.DerivedElevation = record.Elevation2 != ActivityRecord.Invalid
                    ? record.Elevation2 + (average1 - average2)
                    : ActivityRecord.Invalid;
            }
        }
    }

    private static int ReadUInt16(byte[] data, int offset) =>
        data[offset] | (data[offset + 1] << 8);

    private static uint ReadUInt32(byte[] data, int offset) =>
        (uint)(data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));

    /// <summary>
    /// Reads a string the way the watch stores it: bytes are read as signed and
    /// only the positive ones are kept, so the result is plain ASCII without
    /// any padding.
    /// </summary>
    private static string ReadLatinString(byte[] data, int offset, int count)
    {
        StringBuilder builder = new(count);

        for (int i = 0; i < count; i++)
        {
            sbyte value = (sbyte)data[offset + i];
            if (value > 0)
            {
                builder.Append((char)value);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Holds the state that spans several records while a file is read.
    /// </summary>
    private sealed class ReaderState(Activity activity)
    {
        private double _previousLatitude;
        private double _previousLongitude;
        private long _timeOfPause;
        private ActivityRecord? _currentRecord;
        private ActivitySegment? _currentSegment;

        public void ParseRecord(byte[] record)
        {
            switch (record[0])
            {
                case TtbinTags.TagSummary:
                    ParseSummary(record);
                    break;
                case TtbinTags.TagStatus:
                    ParseStatus(record);
                    break;
                case TtbinTags.TagTimeToFix:
                    activity.SecondsToFix = record.Length > 2 ? ReadUInt16(record, 1) : 0;
                    break;
                case TtbinTags.TagBattery:
                    activity.BatteryLevel = record.Length > 1 ? record[1] : 0;
                    break;
                case TtbinTags.TagWorkout:
                    ParseWorkout(record);
                    break;
                case TtbinTags.TagHeartRateRecovery:
                    ParseHeartRateRecovery(record);
                    break;
                case TtbinTags.TagRouteDescription:
                    ParseRouteDescription(record);
                    break;
                case TtbinTags.TagGps:
                    ParseGps(record);
                    break;
                case TtbinTags.TagGym:
                    ParseGym(record);
                    break;
                case TtbinTags.TagElevation:
                    ParseElevation(record);
                    break;
                case TtbinTags.TagHeartRate:
                    ParseHeartRate(record);
                    break;
                case TtbinTags.TagPrecision:
                    ParsePrecision(record);
                    break;
                case TtbinTags.TagFitnessPoints:
                    ParseFitnessPoints(record);
                    break;
                default:
                    // Movement (0x42), 0x37, 0x48 and 0x4b carry no usable data.
                    break;
            }
        }

        private void ParseSummary(byte[] record)
        {
            activity.SummaryType = record[1];
            activity.SummaryDistance = BitConverter.Int32BitsToSingle(unchecked((int)ReadUInt32(record, 2)));
            activity.SummaryDuration = unchecked((int)ReadUInt32(record, 6));
            activity.SummaryCalories = ReadUInt16(record, 10);
        }

        private void ParseStatus(byte[] record)
        {
            byte status = record[1];
            byte type = record[2];
            int timestamp = unchecked((int)ReadUInt32(record, 3));

            activity.SummaryActivity = type;

            if (status == TtbinTags.StatusActive)
            {
                bool startNewSegment;

                if (_timeOfPause != 0)
                {
                    // A pause followed quickly by a resume marks a waypoint.
                    if ((timestamp - _timeOfPause) * 1000 <= WaypointLogTimeoutMs)
                    {
                        if (_currentRecord is not null)
                        {
                            activity.Waypoints.Add(_currentRecord);
                        }

                        startNewSegment = false;
                    }
                    else
                    {
                        startNewSegment = true;
                    }
                }
                else
                {
                    startNewSegment = true;
                }

                if (startNewSegment)
                {
                    _currentSegment = new ActivitySegment
                    {
                        StartUtc = DateTimeOffset.FromUnixTimeSeconds(timestamp),
                    };
                    activity.Segments.Add(_currentSegment);
                }
            }

            if (status == TtbinTags.StatusPaused)
            {
                if (_currentSegment is not null)
                {
                    _currentSegment.EndUtc = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                }

                _timeOfPause = timestamp;
            }
        }

        private void ParseGps(byte[] record)
        {
            int latitude = unchecked((int)ReadUInt32(record, 1));
            int longitude = unchecked((int)ReadUInt32(record, 5));
            int heading = ReadUInt16(record, 9);
            int speed = ReadUInt16(record, 11);
            uint timestamp = ReadUInt32(record, 13);
            int calories = ReadUInt16(record, 17);
            float instantSpeed = BitConverter.Int32BitsToSingle(unchecked((int)ReadUInt32(record, 19)));
            float cumulativeDistance = BitConverter.Int32BitsToSingle(unchecked((int)ReadUInt32(record, 23)));
            int cycles = record[27];

            if (timestamp == InvalidTimestamp)
            {
                return;
            }

            double latitudeDegrees = latitude / 1.0E7;
            double longitudeDegrees = longitude / 1.0E7;

            if (GeoDistance.HaversineKm(
                    _previousLatitude, _previousLongitude, latitudeDegrees, longitudeDegrees) < 0.0)
            {
                return;
            }

            ActivityRecord newRecord = new()
            {
                DateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp),
                Latitude = latitudeDegrees,
                Longitude = longitudeDegrees,
                Heading = heading / 100.0,
                Speed = speed / 100.0,
                Calories = calories,
                InstantSpeed = instantSpeed,
                Distance = cumulativeDistance,
                Cycles = cycles,
                BatteryLevel = activity.BatteryLevel,
            };

            _currentRecord = newRecord;
            _currentSegment?.AddRecord(newRecord);

            _previousLatitude = latitudeDegrees;
            _previousLongitude = longitudeDegrees;
        }

        private void ParseGym(byte[] record)
        {
            long timestamp = ReadUInt32(record, 1);
            int calories = ReadUInt16(record, 5);
            uint cycles = ReadUInt32(record, 7);

            // The gym timestamp is in watch local time.
            timestamp -= activity.TimeZoneSeconds;

            if (timestamp == InvalidTimestamp)
            {
                return;
            }

            _currentRecord = new ActivityRecord
            {
                DateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp),
                Calories = calories,
                Cycles = unchecked((int)cycles),
            };
        }

        private void ParseHeartRate(byte[] record)
        {
            if (_currentRecord is null)
            {
                return;
            }

            int heartRate = record[1];
            int timestamp = unchecked((int)ReadUInt32(record, 3));

            // The heart rate record carries the watch local time.
            long utcMilliseconds = (long)(timestamp - activity.TimeZoneSeconds) * 1000L;

            if (_currentRecord.DateTime is null)
            {
                _currentRecord.HeartRate = heartRate;
                return;
            }

            // The heart rate timestamp normally matches the GPS timestamp but
            // may lag it by a second or two.
            long recordMilliseconds =
                (_currentRecord.DateTime.Value.UtcDateTime - DateTimeOffset.UnixEpoch.UtcDateTime).Ticks
                / TimeSpan.TicksPerMillisecond;

            if (recordMilliseconds - utcMilliseconds <= 2000)
            {
                _currentRecord.HeartRate = heartRate;
            }
        }

        private void ParseElevation(byte[] record)
        {
            if (_currentRecord is null)
            {
                return;
            }

            byte status = record[1];
            int elevation1 = unchecked((short)ReadUInt16(record, 2));
            int elevation2 = unchecked((short)ReadUInt16(record, 4));
            int cumulativeAscend = ReadUInt16(record, 6);
            int cumulativeDescend = ReadUInt16(record, 8);

            if (elevation1 == 32767)
            {
                elevation1 = ActivityRecord.Invalid;
            }

            if (elevation2 == 32767)
            {
                elevation2 = ActivityRecord.Invalid;
            }

            if (cumulativeAscend == 65535)
            {
                cumulativeAscend = ActivityRecord.Invalid;
            }

            if (cumulativeDescend == 65535)
            {
                cumulativeDescend = ActivityRecord.Invalid;
            }

            _currentRecord.Elevation1 = elevation1;
            _currentRecord.Elevation2 = elevation2;
            _currentRecord.Ascend = cumulativeAscend;
            _currentRecord.Descend = cumulativeDescend;
            _currentRecord.ElevationStatus = status;
        }

        private void ParsePrecision(byte[] record)
        {
            if (_currentRecord is null)
            {
                return;
            }

            _currentRecord.Evpe = ReadUInt16(record, 1);
            _currentRecord.Ehpe = ReadUInt16(record, 3);
            _currentRecord.Hdop = record[5];
            _currentRecord.UnknownInt1 = record[6];
            _currentRecord.UnknownInt2 = record[7];
            _currentRecord.UnknownInt3 = record[8];
            _currentRecord.UnknownInt4 = record[9];
            _currentRecord.UnknownInt5 = record[10];
            _currentRecord.UnknownInt6 = record[11];
        }

        private void ParseFitnessPoints(byte[] record)
        {
            int timestamp = unchecked((int)ReadUInt32(record, 1));
            int points = ReadUInt16(record, 5);

            if (activity.FitnessPointsStart < 0)
            {
                activity.FitnessPointsStart = points;
            }

            activity.FitnessPointsEnd = points;

            if (_currentRecord is null)
            {
                return;
            }

            if (_currentRecord.DateTime is null)
            {
                _currentRecord.FitnessPoints = points;
                return;
            }

            long recordMilliseconds =
                (_currentRecord.DateTime.Value.UtcDateTime - DateTimeOffset.UnixEpoch.UtcDateTime).Ticks / TimeSpan.TicksPerMillisecond;
            long pointsMilliseconds = (long)timestamp * 1000L;

            if (recordMilliseconds - pointsMilliseconds <= 2000)
            {
                _currentRecord.FitnessPoints = points;
            }
        }

        private void ParseHeartRateRecovery(byte[] record)
        {
            if (_currentSegment is null)
            {
                return;
            }

            int score = unchecked((int)ReadUInt32(record, 1));
            int recovery = unchecked((int)ReadUInt32(record, 5));

            _currentSegment.HeartRateRecovery = recovery;
            _currentSegment.HeartRateRecoveryScore = score;
        }

        private void ParseRouteDescription(byte[] record)
        {
            if (record.Length < 101)
            {
                return;
            }

            activity.Route = ReadLatinString(record, 21, 80);
        }

        private void ParseWorkout(byte[] record)
        {
            try
            {
                int index = 24 + 5;
                int length = record[index];
                index++;

                activity.Workout = Encoding.UTF8.GetString(record, index, length);
                index += length;

                index += 5;
                length = record[index];
                index++;

                activity.WorkoutDescription = Encoding.UTF8.GetString(record, index, length);
            }
            catch (ArgumentOutOfRangeException)
            {
                // The record is too short to hold a workout; leave it unknown.
            }
        }
    }
}

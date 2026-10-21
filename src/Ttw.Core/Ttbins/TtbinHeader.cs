namespace Ttw.Core.Ttbins;

/// <summary>
/// The header of a TTBIN file. It describes the records the file contains and
/// the length of each of them.
/// </summary>
public sealed class TtbinHeader
{
    /// <summary>Header bytes, without the variable record length array.</summary>
    public const int HeaderLength = 118;

    /// <summary>Bytes in one record length item: one tag byte and one length word.</summary>
    public const int LengthItemLength = 3;

    /// <summary>Record length of a variable length record.</summary>
    public const int VariableLength = 0xFFFF;

    /// <summary>The record tag of the header.</summary>
    public byte Tag { get; set; }

    /// <summary>The file format version.</summary>
    public int FileVersion { get; set; }

    /// <summary>The three firmware version components.</summary>
    public int[] FirmwareVersion { get; set; } = [0, 0, 0];

    /// <summary>The product id of the watch.</summary>
    public int ProductId { get; set; }

    /// <summary>The start time of the activity, in epoch seconds, watch local time.</summary>
    public uint StartTime { get; set; }

    /// <summary>The software version of the watch.</summary>
    public string SoftwareVersion { get; set; } = string.Empty;

    /// <summary>The GPS firmware version of the watch.</summary>
    public string GpsFirmwareVersion { get; set; } = string.Empty;

    /// <summary>The current watch time, in epoch seconds, watch local time.</summary>
    public uint WatchTime { get; set; }

    /// <summary>The difference between watch local time and UTC, in seconds.</summary>
    public uint LocalTimeOffset { get; set; }

    /// <summary>Reserved byte.</summary>
    public int Reserved { get; set; }

    /// <summary>The number of record length items that follow.</summary>
    public int RecordLengthCount { get; set; }

    /// <summary>The record tags the file supports, with their length.</summary>
    public List<(byte Tag, int Length)> RecordLengths { get; } = [];

    /// <summary>
    /// Returns the length of the record identified by the tag, or -1 when the
    /// tag is not listed in the header.
    /// </summary>
    public int GetLength(byte tag)
    {
        foreach ((byte recordTag, int length) in RecordLengths)
        {
            if (recordTag == tag)
            {
                return length;
            }
        }

        return -1;
    }
}

using Ttw.Core.Exporting;
using Ttw.Core.Ttbins;

namespace Ttw.Tests;

public class CsvWriterTests
{
    private static string[] Lines()
    {
        var activity = TtbinReader.Read(TestResources.ReadBytes("test.ttbin"));
        return CsvWriter.Write(activity).Split('\n');
    }

    [Fact]
    public void WritesTheSummaryBlock()
    {
        string[] lines = Lines();

        Assert.Equal("Summary", lines[0]);
        Assert.Equal("Type, 8", lines[1]);
        Assert.Equal("Distance, 3773.177978515625", lines[2]);
        Assert.Equal("Duration, 4243", lines[3]);
        Assert.Equal("Calories, 0", lines[4]);
        Assert.Equal(string.Empty, lines[5]);
    }

    [Fact]
    public void WritesTheColumnHeader()
    {
        string[] lines = Lines();

        Assert.Equal(
            "dateTime, lat, lon, dist, cycles, heading, speed, intSpeed, movement, calories, status, "
            + "ele1, ele2, eleCorr, cumEle1, cumEle2, heartRate, ehpe, evpe, hdop, battery, "
            + "unknownInt1, unknownInt2, unknownInt3, unknownInt4, unknownInt5, unknownInt6, "
            + "unknownFloat1, unknownFloat2",
            lines[6]);
    }

    [Fact]
    public void WritesTheRecordsOfTheTrack()
    {
        string[] lines = Lines();

        Assert.Equal(
            "2019-09-20 13:16:39,53.2204905,6.6565074,0.0,0,282.64,0.24,0.0,-9999,0.0,0,"
            + "-9999.0,-9999.0,-9999.0,-9999.0,-9999.0,-9999,578,574,3,97,3,17,12,19,48,47,0.0,0.0,",
            lines[7]);
    }
}

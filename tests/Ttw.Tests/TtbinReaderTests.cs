using Ttw.Core.Activities;
using Ttw.Core.Ttbins;

namespace Ttw.Tests;

public class TtbinReaderTests
{
    private static Activity ReadTest() => TtbinReader.Read(TestResources.ReadBytes("test.ttbin"));

    private static Activity ReadCycles() => TtbinReader.Read(TestResources.ReadBytes("test_cycles.ttbin"));

    [Fact]
    public void ReadsTheSummaryOfTheActivity()
    {
        Activity activity = ReadTest();

        Assert.Equal(79, activity.BatteryLevel);
        Assert.Equal(8, activity.SummaryActivity);
        Assert.Equal(8, activity.SummaryType);
        Assert.Equal(3773.17798, activity.SummaryDistance, 5);
        Assert.Equal(4243, activity.SummaryDuration);
        Assert.Equal(0, activity.SummaryCalories);
    }

    [Fact]
    public void ReadsTheTimeZoneOfTheWatch()
    {
        Activity activity = ReadTest();

        Assert.Equal(7200, activity.TimeZoneSeconds);
        Assert.Equal(7200, activity.LocalOffsetSeconds);
        Assert.Equal("GMT+02:00", activity.TimeZoneDisplayName);
    }

    [Fact]
    public void ReadsTheActivityType()
    {
        Activity activity = ReadTest();

        Assert.Equal(8, activity.ActivityType);
        Assert.Equal("Freestyle", activity.ActivityDescription);
    }

    [Fact]
    public void KeepsTheDefaultDeviceInformation()
    {
        Activity activity = ReadTest();

        Assert.Equal("TomTom", activity.DeviceName);
        Assert.Equal("Unknown", activity.DeviceSerialNumber);
    }

    [Fact]
    public void ComputesTheDistanceOfTheTrack()
    {
        Activity activity = ReadTest();

        Assert.Equal(3.80562, activity.DistanceKm, 5);
    }

    [Fact]
    public void ReadsTheStartOfTheActivity()
    {
        Activity activity = ReadTest();

        Assert.Equal(new DateTime(2019, 9, 20, 15, 16, 39), activity.FirstActiveRecordTime);
        Assert.Equal(new DateTime(2019, 9, 20, 15, 16, 38), activity.StartDateTime);
        Assert.Equal(new DateOnly(2019, 9, 20), DateOnly.FromDateTime(activity.StartDateTime));
    }

    [Fact]
    public void ReadsTheFitnessPointsAndTheTrackStructure()
    {
        Activity activity = ReadTest();

        Assert.Equal(0, activity.FitnessPoints);
        Assert.Equal(1, activity.SegmentCount);
        Assert.Equal(4243, activity.Segments[0].Records.Count);
        Assert.Equal(string.Empty, activity.Route);
        Assert.True(activity.HasHeightValues);
    }

    [Fact]
    public void ReadsCyclesAndPace()
    {
        Activity activity = ReadCycles();

        Assert.Equal(177, activity.Pace);
        Assert.Equal(2082, activity.Cycles);
    }

    [Fact]
    public void ReadsTheCyclesActivity()
    {
        Activity activity = ReadCycles();

        Assert.Equal(-10800, activity.TimeZoneSeconds);
        Assert.Equal(0, activity.LocalOffsetSeconds);
        Assert.Equal("GMT", activity.TimeZoneDisplayName);
        Assert.Equal(522, activity.FitnessPoints);
        Assert.Equal(1, activity.SegmentCount);
        Assert.Equal(706, activity.Segments[0].Records.Count);
        Assert.Equal("Running", activity.ActivityDescription);
        Assert.False(activity.HasHeightValues);
    }

    [Fact]
    public void ReadsTheFirstRecordOfTheTrack()
    {
        Activity activity = ReadTest();
        ActivityRecord record = activity.Segments[0].Records[0];

        Assert.Equal(new DateTimeOffset(2019, 9, 20, 13, 16, 39, TimeSpan.Zero), record.DateTime);
        Assert.Equal(53.2204905, record.Latitude, 7);
        Assert.Equal(6.6565074, record.Longitude, 7);
        Assert.Equal(282.64, record.Heading, 5);
        Assert.Equal(0.24, record.Speed, 5);
        Assert.Equal(3, record.Hdop);
        Assert.Equal(578, record.Ehpe);
        Assert.Equal(574, record.Evpe);
    }

    [Fact]
    public void RejectsGarbage()
    {
        Assert.ThrowsAny<Exception>(() => TtbinReader.Read([0x01, 0x02, 0x03]));
    }
}

using System.Xml.Linq;
using Ttw.Core.Exporting;
using Ttw.Core.Ttbins;

namespace Ttw.Tests;

public class GpxWriterTests
{
    [Fact]
    public void WritesTheSameDocumentAsTheReferenceImplementation()
    {
        var activity = TtbinReader.Read(TestResources.ReadBytes("test_cycles.ttbin"));

        string gpx = GpxWriter.Write(activity, "testapp", garminExtensions: false, uGotMeExtensions: true);

        XDocument expected = XDocument.Parse(TestResources.ReadText("test6-none.gpx"));
        XDocument actual = XDocument.Parse(gpx);

        XmlCanonical.AssertEquivalent(expected, actual);
    }

    [Fact]
    public void WritesTheTrackPointsOfTheActivity()
    {
        var activity = TtbinReader.Read(TestResources.ReadBytes("test_cycles.ttbin"));

        string gpx = GpxWriter.Write(activity, "testapp", garminExtensions: false, uGotMeExtensions: true);
        XDocument document = XDocument.Parse(gpx);

        XNamespace ns = "http://www.topografix.com/GPX/1/1";

        Assert.Equal(706, document.Descendants(ns + "trkpt").Count());
        Assert.Single(document.Descendants(ns + "trkseg"));
        Assert.Equal("Track - Running", document.Descendants(ns + "name").Single().Value);
    }

    [Fact]
    public void WritesTheGarminExtensionsWhenAsked()
    {
        var activity = TtbinReader.Read(TestResources.ReadBytes("test_cycles.ttbin"));

        string gpx = GpxWriter.Write(activity, "testapp", garminExtensions: true, uGotMeExtensions: false);
        XDocument document = XDocument.Parse(gpx);

        XNamespace gpxtpx = "http://www.garmin.com/xmlschemas/TrackPointExtension/v1";
        XNamespace uGotMe = "http://tracklog.studioblueplanet.net/gpxextensions/v4";

        Assert.Equal("testapp", document.Root!.Attribute("creator")!.Value);
        Assert.NotEmpty(document.Descendants(gpxtpx + "TrackPointExtension"));
        Assert.Empty(document.Descendants(uGotMe + "trackpointExtension"));
    }

    [Fact]
    public void MentionsTheBarometerWhenTheTrackHasHeights()
    {
        var activity = TtbinReader.Read(TestResources.ReadBytes("test.ttbin"));

        string gpx = GpxWriter.Write(activity, "testapp", garminExtensions: true, uGotMeExtensions: false);
        XDocument document = XDocument.Parse(gpx);

        Assert.Equal("testapp with barometer", document.Root!.Attribute("creator")!.Value);
    }
}

using System.Xml.Linq;
using Ttw.Core.Exporting;
using Ttw.Core.Ttbins;

namespace Ttw.Tests;

public class TcxWriterTests
{
    [Fact]
    public void WritesTheSameDocumentAsTheReferenceImplementation()
    {
        var activity = TtbinReader.Read(TestResources.ReadBytes("test.ttbin"));

        string tcx = TcxWriter.Write(activity, "test app");

        XDocument expected = XDocument.Parse(TestResources.ReadText("test7.tcx"));
        XDocument actual = XDocument.Parse(tcx);

        XmlCanonical.AssertEquivalent(expected, actual);
    }

    [Fact]
    public void WritesTheActivitySummary()
    {
        var activity = TtbinReader.Read(TestResources.ReadBytes("test.ttbin"));

        string tcx = TcxWriter.Write(activity, "test app");
        XDocument document = XDocument.Parse(tcx);

        XNamespace ns = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";

        XElement activityElement = document.Descendants(ns + "Activity").Single();
        Assert.Equal("Other", activityElement.Attribute("Sport")!.Value);
        Assert.Equal("2019-09-20T15:16:38Z", document.Descendants(ns + "Id").Single().Value);
        Assert.Equal("4243", document.Descendants(ns + "TotalTimeSeconds").Single().Value);
        Assert.Equal("3805.6", document.Descendants(ns + "DistanceMeters").First().Value);
    }
}

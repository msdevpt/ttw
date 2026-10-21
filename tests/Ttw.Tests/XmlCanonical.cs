using System.Xml.Linq;

namespace Ttw.Tests;

/// <summary>
/// Compares two XML documents by their content instead of their layout: the
/// Java implementation and .NET format whitespace and the XML declaration
/// differently, so a byte comparison would fail on formatting alone.
/// </summary>
internal static class XmlCanonical
{
    /// <summary>
    /// Asserts that two elements are equivalent: same name, same attributes
    /// (in any order), same text and the same child elements in the same order.
    /// </summary>
    public static void AssertEquivalent(XElement expected, XElement actual, string path = "/")
    {
        Assert.Equal(expected.Name, actual.Name);

        Dictionary<string, string> expectedAttributes = Attributes(expected);
        Dictionary<string, string> actualAttributes = Attributes(actual);
        Assert.Equal(expectedAttributes, actualAttributes);

        Assert.Equal(Text(expected), Text(actual));

        List<XElement> expectedChildren = expected.Elements().ToList();
        List<XElement> actualChildren = actual.Elements().ToList();
        Assert.Equal(expectedChildren.Count, actualChildren.Count);

        for (int index = 0; index < expectedChildren.Count; index++)
        {
            AssertEquivalent(
                expectedChildren[index],
                actualChildren[index],
                path + expectedChildren[index].Name.LocalName + "[" + index + "]");
        }
    }

    /// <summary>
    /// Asserts that two documents are equivalent.
    /// </summary>
    public static void AssertEquivalent(XDocument expected, XDocument actual)
    {
        Assert.NotNull(expected.Root);
        Assert.NotNull(actual.Root);
        AssertEquivalent(expected.Root!, actual.Root!);
    }

    private static Dictionary<string, string> Attributes(XElement element) =>
        element.Attributes()
            .Where(attribute => !attribute.IsNamespaceDeclaration)
            .ToDictionary(attribute => attribute.Name.ToString(), attribute => attribute.Value);

    private static string Text(XElement element) =>
        string.Concat(
                element.Nodes()
                    .OfType<XText>()
                    .Where(text => !string.IsNullOrWhiteSpace(text.Value))
                    .Select(text => text.Value))
            .Trim();
}

using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Ttw.Core.Exporting;

/// <summary>
/// Serialises XML documents in the layout the export files use.
/// </summary>
internal static class XmlSerialization
{
    /// <summary>
    /// Serialises a document with two space indentation and a UTF-8 declaration
    /// without byte order mark.
    /// </summary>
    public static string Serialize(XDocument document)
    {
        XmlWriterSettings settings = new()
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false),
        };

        using StringWriter stringWriter = new(CultureInfo.InvariantCulture);
        using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
        {
            document.Save(writer);
        }

        return stringWriter.ToString();
    }
}

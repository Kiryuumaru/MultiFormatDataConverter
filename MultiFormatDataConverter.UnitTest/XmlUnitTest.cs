using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;
using Xunit;

namespace MultiFormatDataConverter.UnitTest;

public class XmlUnitTest
{
    [Fact]
    public void ToXml()
    {
        // Arrange
        var xmlString = DataSamples.XmlSample;
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);
        var xDoc = XDocument.Parse(xmlString);

        // Act
        var converted_XmlDocument_ToXDocument = xmlDoc.ToXDocument();
        var converted_XDocument_ToXmlDocument = xDoc.ToXmlDocument();

        // Round-trip
        var roundTrip_XmlDocument_ToXDocument = converted_XmlDocument_ToXDocument.ToXmlDocument();
        var roundTrip_XDocument_ToXmlDocument = converted_XDocument_ToXmlDocument.ToXDocument();

        // Assert
        Assert.NotNull(converted_XmlDocument_ToXDocument);
        Assert.NotNull(converted_XDocument_ToXmlDocument);
        Assert.NotNull(roundTrip_XmlDocument_ToXDocument);
        Assert.NotNull(roundTrip_XDocument_ToXmlDocument);

        // Compare XML by string representation (ignoring whitespace)
        static string NormalizeXml(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.OuterXml.Replace("\r", "").Replace("\n", "").Replace("  ", "");
        }

        Assert.Equal(
            NormalizeXml(xmlDoc.OuterXml),
            NormalizeXml(roundTrip_XmlDocument_ToXDocument.OuterXml)
        );
        Assert.Equal(
            NormalizeXml(xDoc.ToString()),
            NormalizeXml(roundTrip_XDocument_ToXmlDocument.ToString())
        );
    }

    [Fact]
    public void ToJson()
    {
        // Arrange
        var xmlString = DataSamples.XmlSample;
        var expectedXmlDoccument = new XmlDocument();
        expectedXmlDoccument.LoadXml(xmlString);
        var expectedXDocument = XDocument.Parse(xmlString);

        // Act
        var converted_XmlDocument_ToJsonNode = expectedXmlDoccument.ToJsonNode();
        var converted_XmlDocument_ToJsonObject = expectedXmlDoccument.ToJsonObject();
        var converted_XmlDocument_ToJsonDocument = expectedXmlDoccument.ToJsonDocument();

        var converted_XDocument_ToJsonNode = expectedXDocument.ToJsonNode();
        var converted_XDocument_ToJsonObject = expectedXDocument.ToJsonObject();
        var converted_XDocument_ToJsonDocument = expectedXDocument.ToJsonDocument();

        var roundTrip_XmlDocument_ToJsonNode = converted_XmlDocument_ToJsonNode?.ToXmlDocument();
        var roundTrip_XmlDocument_ToJsonObject = converted_XmlDocument_ToJsonObject?.ToXmlDocument();
        var roundTrip_XmlDocument_ToJsonDocument = converted_XmlDocument_ToJsonDocument?.ToXmlDocument();

        var roundTrip_XDocument_ToJsonNode = converted_XDocument_ToJsonNode?.ToXDocument();
        var roundTrip_XDocument_ToJsonObject = converted_XDocument_ToJsonObject?.ToXDocument();
        var roundTrip_XDocument_ToJsonDocument = converted_XDocument_ToJsonDocument?.ToXDocument();

        // Assert
        Assert.NotNull(converted_XmlDocument_ToJsonNode);
        Assert.NotNull(converted_XmlDocument_ToJsonObject);
        Assert.NotNull(converted_XmlDocument_ToJsonDocument);

        Assert.NotNull(converted_XDocument_ToJsonNode);
        Assert.NotNull(converted_XDocument_ToJsonObject);
        Assert.NotNull(converted_XDocument_ToJsonDocument);

        Assert.NotNull(roundTrip_XmlDocument_ToJsonNode);
        Assert.NotNull(roundTrip_XmlDocument_ToJsonObject);
        Assert.NotNull(roundTrip_XmlDocument_ToJsonDocument);

        Assert.NotNull(roundTrip_XDocument_ToJsonNode);
        Assert.NotNull(roundTrip_XDocument_ToJsonObject);
        Assert.NotNull(roundTrip_XDocument_ToJsonDocument);

        XmlValidator.AreEqual(expectedXmlDoccument, roundTrip_XmlDocument_ToJsonNode!);
        XmlValidator.AreEqual(expectedXmlDoccument, roundTrip_XmlDocument_ToJsonObject!);
        XmlValidator.AreEqual(expectedXmlDoccument, roundTrip_XmlDocument_ToJsonDocument!);

        XmlValidator.AreEqual(expectedXDocument, roundTrip_XDocument_ToJsonNode!);
        XmlValidator.AreEqual(expectedXDocument, roundTrip_XDocument_ToJsonObject!);
        XmlValidator.AreEqual(expectedXDocument, roundTrip_XDocument_ToJsonDocument!);
    }

    [Fact]
    public void ToYaml()
    {
        // Arrange
        var xmlString = DataSamples.XmlSample;
        var expectedXmlDoccument = new XmlDocument();
        expectedXmlDoccument.LoadXml(xmlString);
        var expectedXDocument = XDocument.Parse(xmlString);

        // Act
        var converted_XmlDocument_ToYaml = expectedXmlDoccument.ToYamlStream();
        var converted_XDocument_ToYaml = expectedXDocument.ToYamlStream();

        var roundTrip_XmlDocument_ToYaml = converted_XmlDocument_ToYaml?.ToXmlDocument();
        var roundTrip_XDocument_ToYaml = converted_XDocument_ToYaml?.ToXDocument();

        // Assert
        Assert.NotNull(converted_XmlDocument_ToYaml);
        Assert.NotNull(converted_XDocument_ToYaml);

        Assert.NotNull(roundTrip_XmlDocument_ToYaml);
        Assert.NotNull(roundTrip_XDocument_ToYaml);

        XmlValidator.AreEqual(expectedXmlDoccument, roundTrip_XmlDocument_ToYaml, true);
        XmlValidator.AreEqual(expectedXDocument, roundTrip_XDocument_ToYaml, true);
    }
}

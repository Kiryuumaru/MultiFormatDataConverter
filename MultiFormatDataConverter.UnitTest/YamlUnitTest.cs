using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.RepresentationModel;

namespace MultiFormatDataConverter.UnitTest;

public class YamlUnitTest
{
    [Fact]
    public void ToJson()
    {
        // Arrange
        var yamlString = DataSamples.YamlSample;
        var expectedYamlStream = new YamlStream();
        using (var reader = new StringReader(yamlString))
            expectedYamlStream.Load(reader);

        // Act
        var converted_YamlStream_ToJsonNodeArray = expectedYamlStream.ToJsonNodeArray();
        var converted_YamlStream_ToJsonObjectArray = expectedYamlStream.ToJsonObjectArray();
        var converted_YamlStream_ToJsonDocumentArray = expectedYamlStream.ToJsonDocumentArray();
        var converted_YamlStream_ToJsonObject = expectedYamlStream.ToJsonObject();
        var converted_YamlStream_ToJsonDocument = expectedYamlStream.ToJsonDocument();

        var roundTrip_YamlStream_ToJsonNodeArray = converted_YamlStream_ToJsonNodeArray?.ToYamlStream();
        var roundTrip_YamlStream_ToJsonObjectArray = converted_YamlStream_ToJsonObjectArray?.ToYamlStream();
        var roundTrip_YamlStream_ToJsonDocumentArray = converted_YamlStream_ToJsonDocumentArray?.ToYamlStream();
        var roundTrip_YamlStream_ToJsonObject = converted_YamlStream_ToJsonObject?.ToYamlStream();
        var roundTrip_YamlStream_ToJsonDocument = converted_YamlStream_ToJsonDocument?.ToYamlStream();

        // Assert
        Assert.NotNull(converted_YamlStream_ToJsonNodeArray);
        Assert.NotNull(converted_YamlStream_ToJsonObjectArray);
        Assert.NotNull(converted_YamlStream_ToJsonDocumentArray);
        Assert.NotNull(converted_YamlStream_ToJsonObject);
        Assert.NotNull(converted_YamlStream_ToJsonDocument);

        Assert.NotNull(roundTrip_YamlStream_ToJsonNodeArray);
        Assert.NotNull(roundTrip_YamlStream_ToJsonObjectArray);
        Assert.NotNull(roundTrip_YamlStream_ToJsonDocumentArray);
        Assert.NotNull(roundTrip_YamlStream_ToJsonObject);
        Assert.NotNull(roundTrip_YamlStream_ToJsonDocument);

        YamlValidator.AreEqual(expectedYamlStream, roundTrip_YamlStream_ToJsonNodeArray!);
        YamlValidator.AreEqual(expectedYamlStream, roundTrip_YamlStream_ToJsonObjectArray!);
        YamlValidator.AreEqual(expectedYamlStream, roundTrip_YamlStream_ToJsonDocumentArray!);
        YamlValidator.AreEqual(expectedYamlStream, roundTrip_YamlStream_ToJsonObject!);
        YamlValidator.AreEqual(expectedYamlStream, roundTrip_YamlStream_ToJsonDocument!);
    }

    [Fact]
    public void ToXml()
    {
        // Arrange
        var yamlString = DataSamples.YamlSample;
        var expectedYamlStream = new YamlStream();
        using (var reader = new StringReader(yamlString))
            expectedYamlStream.Load(reader);

        // Act
        var converted_YamlStream_ToXmlDocumentArray = expectedYamlStream.ToXmlDocumentArray();
        var converted_YamlStream_ToXDocumentArray = expectedYamlStream.ToXDocumentArray();
        var converted_YamlStream_ToXmlDocument = expectedYamlStream.ToXmlDocument();
        var converted_YamlStream_ToXDocument = expectedYamlStream.ToXDocument();

        var roundTrip_YamlStream_ToXmlDocumentArray = converted_YamlStream_ToXmlDocumentArray?.ToYamlStream();
        var roundTrip_YamlStream_ToXDocumentArray = converted_YamlStream_ToXDocumentArray?.ToYamlStream();
        var roundTrip_YamlStream_ToXmlDocument = converted_YamlStream_ToXmlDocument?.ToYamlStream();
        var roundTrip_YamlStream_ToXDocument = converted_YamlStream_ToXDocument?.ToYamlStream();

        // Assert
        Assert.NotNull(converted_YamlStream_ToXmlDocumentArray);
        Assert.NotNull(converted_YamlStream_ToXDocumentArray);
        Assert.NotNull(converted_YamlStream_ToXmlDocument);
        Assert.NotNull(converted_YamlStream_ToXDocument);

        Assert.NotNull(roundTrip_YamlStream_ToXmlDocumentArray);
        Assert.NotNull(roundTrip_YamlStream_ToXDocumentArray);
        Assert.NotNull(roundTrip_YamlStream_ToXmlDocument);
        Assert.NotNull(roundTrip_YamlStream_ToXDocument);

        YamlValidator.AreEqual(expectedYamlStream, roundTrip_YamlStream_ToXmlDocumentArray!, true);
        YamlValidator.AreEqual(expectedYamlStream, roundTrip_YamlStream_ToXDocumentArray!, true);
        YamlValidator.AreEqual(expectedYamlStream, roundTrip_YamlStream_ToXmlDocument!, true);
        YamlValidator.AreEqual(expectedYamlStream, roundTrip_YamlStream_ToXDocument!, true);
    }
}

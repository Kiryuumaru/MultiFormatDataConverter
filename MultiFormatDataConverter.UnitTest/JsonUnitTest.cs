using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.RepresentationModel;

namespace MultiFormatDataConverter.UnitTest;

public class JsonUnitTest
{
    [Fact]
    public async Task ToJson()
    {
        // Arrange
        var jsonString = DataSamples.JsonSample;
        var expectedJsonDocument = JsonSerializer.Deserialize<JsonDocument>(jsonString);
        var expectedJsonObject = JsonSerializer.Deserialize<JsonObject>(jsonString);
        var expectedJsonNode = JsonSerializer.Deserialize<JsonNode>(jsonString);
        var expectedJsonArray = new JsonArray(expectedJsonObject?.DeepClone(), expectedJsonObject?.DeepClone());

        // Act
        var converted_JsonDocument_ToJsonNode = await expectedJsonDocument?.ToJsonNode()!;
        var converted_JsonDocument_ToJsonObject = await expectedJsonDocument?.ToJsonObject()!;
        var converted_JsonObject_ToJsonDocument = await expectedJsonObject?.ToJsonDocument()!;
        var converted_JsonNode_ToJsonDocument = await expectedJsonNode?.ToJsonDocument()!;
        var converted_JsonArray_ToJsonDocument = await expectedJsonArray?.ToJsonDocument()!;

        var roundTrip_JsonDocument_ToJsonNode = await converted_JsonDocument_ToJsonNode?.ToJsonDocument()!;
        var roundTrip_JsonDocument_ToJsonObject = await converted_JsonDocument_ToJsonObject?.ToJsonDocument()!;
        var roundTrip_JsonObject_ToJsonDocument = await converted_JsonObject_ToJsonDocument?.ToJsonObject()!;
        var roundTrip_JsonNode_ToJsonDocument = await converted_JsonNode_ToJsonDocument?.ToJsonNode()!;
        var roundTrip_JsonArray_ToJsonDocument = await converted_JsonArray_ToJsonDocument?.ToJsonArray()!;

        var roundTrip_JsonDocument_ToJsonNode_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonDocument_ToJsonNode!.RootElement.GetRawText())!;
        var roundTrip_JsonDocument_ToJsonObject_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonDocument_ToJsonObject!.RootElement.GetRawText())!;
        var roundTrip_JsonObject_ToJsonDocument_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonObject_ToJsonDocument!.ToString())!;
        var roundTrip_JsonNode_ToJsonDocument_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonNode_ToJsonDocument!.ToString())!;
        var roundTrip_JsonArray_ToJsonDocument_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonArray_ToJsonDocument!.ToString())!;

        // Assert
        Assert.NotNull(converted_JsonDocument_ToJsonNode);
        Assert.NotNull(converted_JsonDocument_ToJsonObject);
        Assert.NotNull(converted_JsonObject_ToJsonDocument);
        Assert.NotNull(converted_JsonNode_ToJsonDocument);
        Assert.NotNull(converted_JsonArray_ToJsonDocument);

        Assert.NotNull(roundTrip_JsonDocument_ToJsonNode);
        Assert.NotNull(roundTrip_JsonDocument_ToJsonObject);
        Assert.NotNull(roundTrip_JsonObject_ToJsonDocument);
        Assert.NotNull(roundTrip_JsonNode_ToJsonDocument);
        Assert.NotNull(roundTrip_JsonArray_ToJsonDocument);

        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonDocument_ToJsonNode_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonDocument_ToJsonObject_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonObject_ToJsonDocument_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonNode_ToJsonDocument_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonArray, roundTrip_JsonArray_ToJsonDocument_ToTest));
    }

    [Fact]
    public void ToYaml()
    {
        // Arrange
        var jsonString = DataSamples.JsonSample;
        var expectedJsonDocument = JsonSerializer.Deserialize<JsonDocument>(jsonString);
        var expectedJsonObject = JsonSerializer.Deserialize<JsonObject>(jsonString);
        var expectedJsonNode = JsonSerializer.Deserialize<JsonNode>(jsonString);
        var expectedJsonArray = new JsonArray(expectedJsonObject?.DeepClone(), expectedJsonObject?.DeepClone());

        // Act
        var converted_JsonDocument_ToYaml = expectedJsonDocument?.ToYamlStream()!;
        var converted_JsonObject_ToYaml = expectedJsonObject?.ToYamlStream()!;
        var converted_JsonNode_ToYaml = expectedJsonNode?.ToYamlStream()!;
        var converted_JsonArray_ToYaml = expectedJsonArray?.ToYamlStream()!;

        var roundTrip_JsonDocument_ToYaml = converted_JsonDocument_ToYaml?.ToJsonNodeArray()!;
        var roundTrip_JsonObject_ToYaml = converted_JsonObject_ToYaml?.ToJsonNodeArray()!;
        var roundTrip_JsonNode_ToYaml = converted_JsonNode_ToYaml?.ToJsonNodeArray()!;
        var roundTrip_JsonArray_ToYaml = converted_JsonArray_ToYaml?.ToJsonNodeArray()!;

        // Assert
        Assert.NotNull(converted_JsonDocument_ToYaml);
        Assert.NotNull(converted_JsonObject_ToYaml);
        Assert.NotNull(converted_JsonNode_ToYaml);
        Assert.NotNull(converted_JsonArray_ToYaml);

        Assert.Single(roundTrip_JsonDocument_ToYaml);
        Assert.Single(roundTrip_JsonObject_ToYaml);
        Assert.Single(roundTrip_JsonNode_ToYaml);
        Assert.Single(roundTrip_JsonArray_ToYaml);

        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonDocument_ToYaml[0]);
        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonObject_ToYaml[0]);
        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonNode_ToYaml[0]);
        JsonValidator.AreEqual(expectedJsonArray, roundTrip_JsonArray_ToYaml[0]);
    }

    [Fact]
    public void ToXml()
    {
        // Arrange
        var jsonString = DataSamples.JsonSample;
        var expectedJsonDocument = JsonSerializer.Deserialize<JsonDocument>(jsonString);
        var expectedJsonObject = JsonSerializer.Deserialize<JsonObject>(jsonString);
        var expectedJsonNode = JsonSerializer.Deserialize<JsonNode>(jsonString);
        var expectedJsonArray = new JsonArray(expectedJsonObject?.DeepClone(), expectedJsonObject?.DeepClone());

        // Act
        var converted_JsonDocument_ToXmlDocument = expectedJsonDocument?.ToXmlDocument()!;
        var converted_JsonObject_ToXmlDocument = expectedJsonObject?.ToXmlDocument()!;
        var converted_JsonNode_ToXmlDocument = expectedJsonNode?.ToXmlDocument()!;
        var converted_JsonArray_ToXmlDocument = expectedJsonArray?.ToXmlDocument()!;

        var roundTrip_JsonDocument_ToXmlDocument = converted_JsonDocument_ToXmlDocument?.ToJsonNode()!;
        var roundTrip_JsonObject_ToXmlDocument = converted_JsonObject_ToXmlDocument?.ToJsonNode()!;
        var roundTrip_JsonNode_ToXmlDocument = converted_JsonNode_ToXmlDocument?.ToJsonNode()!;
        var roundTrip_JsonArray_ToXmlDocument = converted_JsonArray_ToXmlDocument?.ToJsonNode()!;

        // Assert
        Assert.NotNull(converted_JsonDocument_ToXmlDocument);
        Assert.NotNull(converted_JsonObject_ToXmlDocument);
        Assert.NotNull(converted_JsonNode_ToXmlDocument);
        Assert.NotNull(converted_JsonArray_ToXmlDocument);

        Assert.NotNull(roundTrip_JsonDocument_ToXmlDocument);
        Assert.NotNull(roundTrip_JsonObject_ToXmlDocument);
        Assert.NotNull(roundTrip_JsonNode_ToXmlDocument);
        Assert.NotNull(roundTrip_JsonArray_ToXmlDocument);

        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonDocument_ToXmlDocument);
        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonObject_ToXmlDocument);
        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonNode_ToXmlDocument);
        JsonValidator.AreEqual(expectedJsonArray, roundTrip_JsonArray_ToXmlDocument);
    }
}

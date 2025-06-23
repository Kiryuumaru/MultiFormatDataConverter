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
        var expectedJsonElement = expectedJsonDocument!.RootElement;
        var expectedJsonElementArray = JsonDocument.Parse("[" + jsonString + "," + jsonString + "]").RootElement;

        // Act
        var converted_JsonDocument_ToJsonNode = await expectedJsonDocument?.ToJsonNode()!;
        var converted_JsonDocument_ToJsonObject = await expectedJsonDocument?.ToJsonObject()!;
        var converted_JsonObject_ToJsonDocument = await expectedJsonObject?.ToJsonDocument()!;
        var converted_JsonNode_ToJsonDocument = await expectedJsonNode?.ToJsonDocument()!;
        var converted_JsonArray_ToJsonDocument = await expectedJsonArray?.ToJsonDocument()!;

        var converted_JsonElement_ToJsonNode = await expectedJsonElement.ToJsonNode();
        var converted_JsonElement_ToJsonObject = await expectedJsonElement.ToJsonObject();
        var converted_JsonElementArray_ToJsonArray = await expectedJsonElementArray.ToJsonArray();

        var converted_JsonNode_ToJsonElement = await expectedJsonNode!.ToJsonElement();
        var converted_JsonObject_ToJsonElement = await expectedJsonObject!.ToJsonElement();
        var converted_JsonArray_ToJsonElement = await expectedJsonArray!.ToJsonElement();

        var roundTrip_JsonDocument_ToJsonNode = await converted_JsonDocument_ToJsonNode?.ToJsonDocument()!;
        var roundTrip_JsonDocument_ToJsonObject = await converted_JsonDocument_ToJsonObject?.ToJsonDocument()!;
        var roundTrip_JsonObject_ToJsonDocument = await converted_JsonObject_ToJsonDocument?.ToJsonObject()!;
        var roundTrip_JsonNode_ToJsonDocument = await converted_JsonNode_ToJsonDocument?.ToJsonNode()!;
        var roundTrip_JsonArray_ToJsonDocument = await converted_JsonArray_ToJsonDocument?.ToJsonArray()!;

        var roundTrip_JsonElement_ToJsonNode_ToJsonElement = await converted_JsonElement_ToJsonNode.ToJsonElement();
        var roundTrip_JsonElement_ToJsonObject_ToJsonElement = await converted_JsonElement_ToJsonObject.ToJsonElement();
        var roundTrip_JsonElementArray_ToJsonArray_ToJsonElement = await converted_JsonElementArray_ToJsonArray.ToJsonElement();

        var roundTrip_JsonNode_ToJsonElement_ToJsonNode = await converted_JsonNode_ToJsonElement.ToJsonNode();
        var roundTrip_JsonObject_ToJsonElement_ToJsonObject = await converted_JsonObject_ToJsonElement.ToJsonObject();
        var roundTrip_JsonArray_ToJsonElement_ToJsonArray = await converted_JsonArray_ToJsonElement.ToJsonArray();

        var roundTrip_JsonDocument_ToJsonNode_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonDocument_ToJsonNode!.RootElement.GetRawText())!;
        var roundTrip_JsonDocument_ToJsonObject_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonDocument_ToJsonObject!.RootElement.GetRawText())!;
        var roundTrip_JsonObject_ToJsonDocument_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonObject_ToJsonDocument!.ToString())!;
        var roundTrip_JsonNode_ToJsonDocument_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonNode_ToJsonDocument!.ToString())!;
        var roundTrip_JsonArray_ToJsonDocument_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonArray_ToJsonDocument!.ToString())!;

        var roundTrip_JsonElement_ToJsonNode_ToJsonElement_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonElement_ToJsonNode_ToJsonElement!.GetRawText())!;
        var roundTrip_JsonElement_ToJsonObject_ToJsonObject_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonElement_ToJsonObject_ToJsonElement!.ToString())!;
        var roundTrip_JsonElementArray_ToJsonArray_ToJsonElement_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonElementArray_ToJsonArray_ToJsonElement!.ToString())!;

        var roundTrip_JsonNode_ToJsonElement_ToJsonNode_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonNode_ToJsonElement_ToJsonNode!.ToString())!;
        var roundTrip_JsonObject_ToJsonElement_ToJsonObject_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonObject_ToJsonElement_ToJsonObject!.ToString())!;
        var roundTrip_JsonArray_ToJsonElement_ToJsonArray_ToTest = JsonSerializer.Deserialize<JsonNode>(roundTrip_JsonArray_ToJsonElement_ToJsonArray!.ToString())!;

        // Assert
        Assert.NotNull(converted_JsonDocument_ToJsonNode);
        Assert.NotNull(converted_JsonDocument_ToJsonObject);
        Assert.NotNull(converted_JsonObject_ToJsonDocument);
        Assert.NotNull(converted_JsonNode_ToJsonDocument);
        Assert.NotNull(converted_JsonArray_ToJsonDocument);

        Assert.NotNull(converted_JsonElement_ToJsonNode);
        Assert.NotNull(converted_JsonElement_ToJsonObject);
        Assert.NotNull(converted_JsonElementArray_ToJsonArray);

        Assert.NotNull(roundTrip_JsonDocument_ToJsonNode);
        Assert.NotNull(roundTrip_JsonDocument_ToJsonObject);
        Assert.NotNull(roundTrip_JsonObject_ToJsonDocument);
        Assert.NotNull(roundTrip_JsonNode_ToJsonDocument);
        Assert.NotNull(roundTrip_JsonArray_ToJsonDocument);

        Assert.NotNull(roundTrip_JsonNode_ToJsonElement_ToJsonNode);
        Assert.NotNull(roundTrip_JsonObject_ToJsonElement_ToJsonObject);
        Assert.NotNull(roundTrip_JsonArray_ToJsonElement_ToJsonArray);

        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonDocument_ToJsonNode_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonDocument_ToJsonObject_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonObject_ToJsonDocument_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonNode_ToJsonDocument_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonArray, roundTrip_JsonArray_ToJsonDocument_ToTest));

        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonElement_ToJsonNode_ToJsonElement_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonObject, roundTrip_JsonElement_ToJsonObject_ToJsonObject_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonArray, roundTrip_JsonElementArray_ToJsonArray_ToJsonElement_ToTest));

        Assert.True(JsonNode.DeepEquals(expectedJsonNode, roundTrip_JsonNode_ToJsonElement_ToJsonNode_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonObject, roundTrip_JsonObject_ToJsonElement_ToJsonObject_ToTest));
        Assert.True(JsonNode.DeepEquals(expectedJsonArray, roundTrip_JsonArray_ToJsonElement_ToJsonArray_ToTest));
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

        var converted_JsonDocument_ToXDocument = expectedJsonDocument?.ToXDocument()!;
        var converted_JsonObject_ToXDocument = expectedJsonObject?.ToXDocument()!;
        var converted_JsonNode_ToXDocument = expectedJsonNode?.ToXDocument()!;
        var converted_JsonArray_ToXDocument = expectedJsonArray?.ToXDocument()!;

        var roundTrip_JsonDocument_ToXmlDocument = converted_JsonDocument_ToXmlDocument?.ToJsonNode()!;
        var roundTrip_JsonObject_ToXmlDocument = converted_JsonObject_ToXmlDocument?.ToJsonNode()!;
        var roundTrip_JsonNode_ToXmlDocument = converted_JsonNode_ToXmlDocument?.ToJsonNode()!;
        var roundTrip_JsonArray_ToXmlDocument = converted_JsonArray_ToXmlDocument?.ToJsonNode()!;

        var roundTrip_JsonDocument_ToXDocument = converted_JsonDocument_ToXDocument?.ToJsonNode()!;
        var roundTrip_JsonObject_ToXDocument = converted_JsonObject_ToXDocument?.ToJsonNode()!;
        var roundTrip_JsonNode_ToXDocument = converted_JsonNode_ToXDocument?.ToJsonNode()!;
        var roundTrip_JsonArray_ToXDocument = converted_JsonArray_ToXmlDocument?.ToJsonNode()!;

        // Assert
        Assert.NotNull(converted_JsonDocument_ToXmlDocument);
        Assert.NotNull(converted_JsonObject_ToXmlDocument);
        Assert.NotNull(converted_JsonNode_ToXmlDocument);
        Assert.NotNull(converted_JsonArray_ToXmlDocument);

        Assert.NotNull(converted_JsonDocument_ToXDocument);
        Assert.NotNull(converted_JsonObject_ToXDocument);
        Assert.NotNull(converted_JsonNode_ToXDocument);
        Assert.NotNull(converted_JsonArray_ToXDocument);

        Assert.NotNull(roundTrip_JsonDocument_ToXmlDocument);
        Assert.NotNull(roundTrip_JsonObject_ToXmlDocument);
        Assert.NotNull(roundTrip_JsonNode_ToXmlDocument);
        Assert.NotNull(roundTrip_JsonArray_ToXmlDocument);

        Assert.NotNull(roundTrip_JsonDocument_ToXDocument);
        Assert.NotNull(roundTrip_JsonObject_ToXDocument);
        Assert.NotNull(roundTrip_JsonNode_ToXDocument);
        Assert.NotNull(roundTrip_JsonArray_ToXDocument);

        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonDocument_ToXmlDocument, true);
        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonObject_ToXmlDocument, true);
        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonNode_ToXmlDocument, true);
        JsonValidator.AreEqual(expectedJsonArray, roundTrip_JsonArray_ToXmlDocument[0], true);

        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonDocument_ToXDocument, true);
        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonObject_ToXDocument, true);
        JsonValidator.AreEqual(expectedJsonNode, roundTrip_JsonNode_ToXDocument, true);
        JsonValidator.AreEqual(expectedJsonArray, roundTrip_JsonArray_ToXDocument[0], true);
    }
}

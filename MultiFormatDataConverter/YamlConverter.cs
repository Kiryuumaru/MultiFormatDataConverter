using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace MultiFormatDataConverter;

/// <summary>
/// Provides conversion utilities for transforming YAML data to JSON formats.
/// </summary>
public static class YamlConverter
{
    #region ToJson

    /// <summary>
    /// Converts a YAML stream to an array of <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>An array of <see cref="JsonNode"/> representing each document in the YAML stream.</returns>
    public static JsonNode?[] ToJsonNodeArray(this YamlStream yamlStream)
    {
        static JsonNode? ConvertYamlNodeToJsonNode(YamlNode? node)
        {
            if (node == null) return null;

            switch (node)
            {
                case YamlScalarNode scalarNode:
                    if (bool.TryParse(scalarNode.Value, out var boolVal))
                        return JsonValue.Create(boolVal);
                    if (int.TryParse(scalarNode.Value, out var intVal))
                        return JsonValue.Create(intVal);
                    if (long.TryParse(scalarNode.Value, out var longVal))
                        return JsonValue.Create(longVal);
                    if (double.TryParse(scalarNode.Value, out var doubleVal))
                        return JsonValue.Create(doubleVal);
                    if (decimal.TryParse(scalarNode.Value, out var decimalVal))
                        return JsonValue.Create(decimalVal);
                    if (DateTime.TryParse(scalarNode.Value, out var dateTimeVal))
                        return JsonValue.Create(dateTimeVal);
                    if (DateTimeOffset.TryParse(scalarNode.Value, out var dateTimeOffsetVal))
                        return JsonValue.Create(dateTimeOffsetVal);
                    return JsonValue.Create(scalarNode.Value);

                case YamlSequenceNode sequenceNode:
                    return CreateJsonArray(sequenceNode);

                case YamlMappingNode mappingNode:
                    return CreateJsonObject(mappingNode);

                default:
                    return null;
            }
        }

        static JsonArray CreateJsonArray(YamlSequenceNode sequenceNode)
        {
            var jsonArray = new JsonArray();
            foreach (var child in sequenceNode.Children)
            {
                jsonArray.Add(ConvertYamlNodeToJsonNode(child));
            }
            return jsonArray;
        }

        static JsonObject CreateJsonObject(YamlMappingNode mappingNode)
        {
            var jsonObject = new JsonObject();
            foreach (var entry in mappingNode.Children)
            {
                jsonObject.Add((entry.Key as YamlScalarNode)!.Value!, ConvertYamlNodeToJsonNode(entry.Value));
            }
            return jsonObject;
        }

        var documentCount = yamlStream.Documents.Count;
        var jsonNodes = new JsonNode?[documentCount];

        for (int i = 0; i < documentCount; i++)
        {
            var yamlDoc = yamlStream.Documents[i];
            jsonNodes[i] = ConvertYamlNodeToJsonNode(yamlDoc.RootNode);
        }

        return jsonNodes;
    }

    /// <summary>
    /// Converts a YAML stream to an array of <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>An array of <see cref="JsonObject"/> representing each document in the YAML stream.</returns>
    public static JsonObject?[] ToJsonObjectArray(this YamlStream yamlStream)
    {
        var jsonNodes = yamlStream.ToJsonNodeArray();
        return [.. jsonNodes.Select(i => i as JsonObject)];
    }

    /// <summary>
    /// Converts a YAML stream to an array of <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>An array of <see cref="JsonDocument"/> representing each document in the YAML stream.</returns>
    public static JsonDocument?[] ToJsonDocumentArray(this YamlStream yamlStream)
    {
        var jsonNodes = yamlStream.ToJsonNodeArray();
        var result = new JsonDocument?[jsonNodes.Length];
        for (int i = 0; i < jsonNodes.Length; i++)
        {
            if (jsonNodes[i] is JsonNode jsonNode)
            {
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                jsonNode.WriteTo(writer, new JsonSerializerOptions { WriteIndented = true });
                stream.Seek(0, SeekOrigin.Begin);
                result[i] = JsonDocument.Parse(stream);
            }
            else
            {
                result[i] = null;
            }
        }
        return result;
    }

    /// <summary>
    /// Converts a YAML stream to a single JsonObject.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>A JsonObject representing the first document in the YAML stream, or an empty JsonObject if no documents exist.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the YAML stream contains multiple documents.</exception>
    public static JsonObject? ToJsonObject(this YamlStream yamlStream)
    {
        if (yamlStream.Documents.Count > 1)
        {
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToJsonObjectArray)} to convert all documents.");
        }

        var objects = ToJsonObjectArray(yamlStream);
        return objects.Length > 0 ? objects[0] : [];
    }

    /// <summary>
    /// Converts a YAML stream to a single JsonDocument.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>A JsonDocument representing the first document in the YAML stream, or an empty JsonDocument if no documents exist.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the YAML stream contains multiple documents.</exception>
    public static JsonDocument? ToJsonDocument(this YamlStream yamlStream)
    {
        if (yamlStream.Documents.Count > 1)
        {
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToJsonDocumentArray)} to convert all documents.");
        }

        var documents = ToJsonDocumentArray(yamlStream);
        return documents.Length > 0 ? documents[0] : JsonDocument.Parse("{}");
    }

    #endregion

    #region ToXml

    /// <summary>
    /// Converts a YAML stream to an array of XmlDocument objects.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>An array of XmlDocument objects representing each document in the YAML stream.</returns>
    public static System.Xml.XmlDocument[] ToXmlDocumentArray(this YamlStream yamlStream)
    {
        var result = new System.Xml.XmlDocument[yamlStream.Documents.Count];

        for (int i = 0; i < yamlStream.Documents.Count; i++)
        {
            var xmlDoc = new System.Xml.XmlDocument();
            var rootElement = xmlDoc.CreateElement("root");
            xmlDoc.AppendChild(rootElement);

            ConvertYamlNodeToXml(yamlStream.Documents[i].RootNode, rootElement, xmlDoc);
            result[i] = xmlDoc;
        }

        return result;
    }

    /// <summary>
    /// Converts a YAML stream to a single XmlDocument.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>An XmlDocument representing the first document in the YAML stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the YAML stream contains multiple documents.</exception>
    public static System.Xml.XmlDocument ToXmlDocument(this YamlStream yamlStream)
    {
        if (yamlStream.Documents.Count > 1)
        {
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToXmlDocumentArray)} to convert all documents.");
        }

        var documents = ToXmlDocumentArray(yamlStream);
        return documents.Length > 0 ? documents[0] : new System.Xml.XmlDocument();
    }

    private static void ConvertYamlNodeToXml(YamlNode? yamlNode, System.Xml.XmlElement parentElement, System.Xml.XmlDocument xmlDoc)
    {
        if (yamlNode == null) return;

        switch (yamlNode)
        {
            case YamlScalarNode scalarNode:
                parentElement.InnerText = scalarNode.Value ?? string.Empty;
                break;

            case YamlSequenceNode sequenceNode:
                foreach (var childNode in sequenceNode.Children)
                {
                    var itemElement = xmlDoc.CreateElement("item");
                    parentElement.AppendChild(itemElement);
                    ConvertYamlNodeToXml(childNode, itemElement, xmlDoc);
                }
                break;

            case YamlMappingNode mappingNode:
                foreach (var entry in mappingNode.Children)
                {
                    if (entry.Key is YamlScalarNode keyNode)
                    {
                        var elementName = SanitizeXmlElementName(keyNode.Value ?? "element");
                        var childElement = xmlDoc.CreateElement(elementName);
                        parentElement.AppendChild(childElement);
                        ConvertYamlNodeToXml(entry.Value, childElement, xmlDoc);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Sanitizes a string to be used as an XML element name.
    /// </summary>
    /// <param name="name">The string to sanitize.</param>
    /// <returns>A valid XML element name.</returns>
    private static string SanitizeXmlElementName(string name)
    {
        // XML element names must start with a letter or underscore and cannot contain spaces or special characters
        if (string.IsNullOrEmpty(name))
            return "element";

        // Replace invalid characters with underscores
        var sanitized = Regex.Replace(name, @"[^\w\-\.]", "_");

        // Ensure it starts with a letter or underscore
        if (!char.IsLetter(sanitized[0]) && sanitized[0] != '_')
            sanitized = "_" + sanitized;

        return sanitized;
    }

    /// <summary>
    /// Converts a YAML string to an XmlDocument.
    /// </summary>
    /// <param name="yamlString">The YAML string to convert.</param>
    /// <returns>An XmlDocument representing the YAML data.</returns>
    public static System.Xml.XmlDocument YamlStringToXmlDocument(string yamlString)
    {
        var yamlStream = new YamlStream();
        using var reader = new StringReader(yamlString);
        yamlStream.Load(reader);
        return yamlStream.ToXmlDocument();
    }

    #endregion
}
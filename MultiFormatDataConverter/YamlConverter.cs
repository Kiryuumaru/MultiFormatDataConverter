using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

#if NETSTANDARD
using ArgumentNullException = MultiFormatDataConverter.Polyfill.ArgumentNullException;
#else
using ArgumentNullException = System.ArgumentNullException;
#endif

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
        ArgumentNullException.ThrowIfNull(yamlStream);

        static JsonNode? ConvertYamlNodeToJsonNode(YamlNode? node)
        {
            if (node == null) return null;

            return node switch
            {
                YamlScalarNode scalarNode => JsonConverter.CreateJsonValue(scalarNode),
                YamlSequenceNode sequenceNode => CreateJsonArray(sequenceNode),
                YamlMappingNode mappingNode => CreateJsonObject(mappingNode),
                _ => null,
            };
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
        ArgumentNullException.ThrowIfNull(yamlStream);

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
        ArgumentNullException.ThrowIfNull(yamlStream);

        var jsonNodes = yamlStream.ToJsonNodeArray();
        var result = new JsonDocument?[jsonNodes.Length];
        for (int i = 0; i < jsonNodes.Length; i++)
        {
            if (jsonNodes[i] is JsonNode jsonNode)
            {
                using var stream = new MemoryStream();
                using var utf8JsonWriter = new Utf8JsonWriter(stream);
                jsonNode.WriteTo(utf8JsonWriter, new JsonSerializerOptions { WriteIndented = true });
                utf8JsonWriter.Flush();
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
        ArgumentNullException.ThrowIfNull(yamlStream);

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
        ArgumentNullException.ThrowIfNull(yamlStream);

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
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <returns>An array of XmlDocument objects representing each document in the YAML stream.</returns>
    public static XmlDocument[] ToXmlDocumentArray(this YamlStream yamlStream, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        var result = new XmlDocument[yamlStream.Documents.Count];

        for (int i = 0; i < yamlStream.Documents.Count; i++)
        {
            var xmlDocument = new XmlDocument();
            var rootElement = xmlDocument.CreateElement(rootElementName);
            xmlDocument.AppendChild(rootElement);

            ConvertYamlNodeToXml(xmlDocument, rootElement, rootElementName, yamlStream.Documents[i].RootNode, XmlConverter.GetMappingStrategy(xmlMappingStrategy));
            result[i] = xmlDocument;
        }

        return result;
    }

    /// <summary>
    /// Converts a YAML stream to a single XmlDocument.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <returns>An XmlDocument representing the first document in the YAML stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the YAML stream contains multiple documents.</exception>
    public static XmlDocument ToXmlDocument(this YamlStream yamlStream, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        if (yamlStream.Documents.Count > 1)
        {
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToXmlDocumentArray)} to convert all documents.");
        }

        var documents = ToXmlDocumentArray(yamlStream, rootElementName, XmlConverter.GetMappingStrategy(xmlMappingStrategy));
        return documents.Length > 0 ? documents[0] : new XmlDocument();
    }

    /// <summary>
    /// Converts a YAML stream to an array of XDocument objects.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <returns>An array of XDocument objects representing each document in the YAML stream.</returns>
    public static XDocument[] ToXDocumentArray(this YamlStream yamlStream, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        var result = new XDocument[yamlStream.Documents.Count];

        for (int i = 0; i < yamlStream.Documents.Count; i++)
        {
            var xDocument = new XDocument();
            var rootElement = new XElement(rootElementName);
            xDocument.Add(rootElement);

            ConvertYamlNodeToXElement(yamlStream.Documents[i].RootNode, rootElement, rootElementName, XmlConverter.GetMappingStrategy(xmlMappingStrategy));
            result[i] = xDocument;
        }

        return result;
    }

    /// <summary>
    /// Converts a YAML stream to a single XDocument.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <returns>An XDocument representing the first document in the YAML stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the YAML stream contains multiple documents.</exception>
    public static XDocument ToXDocument(this YamlStream yamlStream, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        if (yamlStream.Documents.Count > 1)
        {
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToXDocumentArray)} to convert all documents.");
        }

        var documents = ToXDocumentArray(yamlStream, rootElementName, XmlConverter.GetMappingStrategy(xmlMappingStrategy));
        return documents.Length > 0 ? documents[0] : new XDocument(new XElement(rootElementName));
    }

    private static void ConvertYamlNodeToXml(XmlDocument xmlDocument, XmlElement parentElement, string elementName, YamlNode? yamlNode, Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy)
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
                    var itemElement = xmlDocument.CreateElement(elementName);
                    parentElement.AppendChild(itemElement);
                    ConvertYamlNodeToXml(xmlDocument, itemElement, elementName, childNode, xmlMappingStrategy);
                }
                break;

            case YamlMappingNode mappingNode:
                foreach (var entry in mappingNode.Children)
                {
                    if (entry.Key is YamlScalarNode keyNode)
                    {
                        var validXmlName = XmlConverter.MakeValidXmlName(keyNode.Value ?? "element");
                        if (entry.Value is YamlSequenceNode)
                        {
                            ConvertYamlNodeToXml(xmlDocument, parentElement, validXmlName, entry.Value, xmlMappingStrategy);
                        }
                        else
                        {
                            var childElement = xmlDocument.CreateElement(validXmlName);
                            parentElement.AppendChild(childElement);
                            ConvertYamlNodeToXml(xmlDocument, childElement, validXmlName, entry.Value, xmlMappingStrategy);
                        }
                    }
                }
                break;
        }
    }

    private static void ConvertYamlNodeToXElement(YamlNode? yamlNode, XElement parentElement, string elementName, Func<string, (bool IsAttribute, string Name)> xmlMappingStrategy)
    {
        if (yamlNode == null) return;

        switch (yamlNode)
        {
            case YamlScalarNode scalarNode:
                parentElement.Value = scalarNode.Value ?? string.Empty;
                break;

            case YamlSequenceNode sequenceNode:
                foreach (var childNode in sequenceNode.Children)
                {
                    var itemElement = new XElement(elementName);
                    parentElement.Add(itemElement);
                    ConvertYamlNodeToXElement(childNode, itemElement, elementName, xmlMappingStrategy);
                }
                break;

            case YamlMappingNode mappingNode:
                foreach (var entry in mappingNode.Children)
                {
                    if (entry.Key is YamlScalarNode keyNode)
                    {
                        var validXmlName = XmlConverter.MakeValidXmlName(keyNode.Value ?? "element");
                        if (entry.Value is YamlSequenceNode)
                        {
                            ConvertYamlNodeToXElement(entry.Value, parentElement, validXmlName, xmlMappingStrategy);
                        }
                        else
                        {
                            var childElement = new XElement(validXmlName);
                            parentElement.Add(childElement);
                            ConvertYamlNodeToXElement(entry.Value, childElement, validXmlName, xmlMappingStrategy);
                        }
                    }
                }
                break;
        }
    }

    #endregion

    #region Helpers

    internal static YamlScalarNode CreateYamlScalarNode(object? value)
    {
        if (value is JsonValue jsonValue)
        {
#if NET8_0_OR_GREATER
            if (jsonValue.TryGetValue(out bool boolValue))
                return new YamlScalarNode(boolValue.ToString());
            if (jsonValue.TryGetValue(out byte byteValue))
                return new YamlScalarNode(byteValue.ToString());
            if (jsonValue.TryGetValue(out int intValue))
                return new YamlScalarNode(intValue.ToString());
            if (jsonValue.TryGetValue(out long longValue))
                return new YamlScalarNode(longValue.ToString());
            if (jsonValue.TryGetValue(out double doubleValue))
                return new YamlScalarNode(doubleValue.ToString());
            if (jsonValue.TryGetValue(out decimal decimalValue))
                return new YamlScalarNode(decimalValue.ToString());
#endif
            return new YamlScalarNode(jsonValue.ToString());
        }
        else if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.True)
                return new YamlScalarNode("true");
            else if (jsonElement.ValueKind == JsonValueKind.False)
                return new YamlScalarNode("false");
            else if (jsonElement.ValueKind == JsonValueKind.Number)
            {
                if (jsonElement.TryGetByte(out var byteVal))
                    return new YamlScalarNode(byteVal.ToString());
                else if (jsonElement.TryGetInt32(out var intVal))
                    return new YamlScalarNode(intVal.ToString());
                else if (jsonElement.TryGetInt64(out var longVal))
                    return new YamlScalarNode(longVal.ToString());
                else if (jsonElement.TryGetDouble(out var doubleVal))
                    return new YamlScalarNode(doubleVal.ToString());
                else if (jsonElement.TryGetDecimal(out var decimalVal))
                    return new YamlScalarNode(decimalVal.ToString());
            }
            else if (jsonElement.ValueKind == JsonValueKind.String)
                return new YamlScalarNode(jsonElement.GetString());
            else if (jsonElement.ValueKind == JsonValueKind.Null)
                return new YamlScalarNode();
        }
        else if (value is string str)
            return new YamlScalarNode(str);
        else if (value is bool boolVal)
            return new YamlScalarNode(boolVal.ToString());
        else if (value is int intVal)
            return new YamlScalarNode(intVal.ToString());
        else if (value is long longVal)
            return new YamlScalarNode(longVal.ToString());
        else if (value is double doubleVal)
            return new YamlScalarNode(doubleVal.ToString());
        else if (value is decimal decimalVal)
            return new YamlScalarNode(decimalVal.ToString());
        else if (value is DateTime dateTimeVal)
            return new YamlScalarNode(dateTimeVal.ToString("o"));
        else if (value is DateTimeOffset dateTimeOffsetVal)
            return new YamlScalarNode(dateTimeOffsetVal.ToString("o"));

        return new YamlScalarNode();
    }

    #endregion
}
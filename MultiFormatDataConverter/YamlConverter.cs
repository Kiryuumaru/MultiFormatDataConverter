using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
/// Provides conversion utilities for transforming YAML data to other formats.
/// </summary>
public static class YamlConverter
{
    #region ToJson

    /// <summary>
    /// Converts a YAML stream to an array of <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>An array of <see cref="JsonNode"/> representing each document in the YAML stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
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
    /// Converts a YAML stream to an XML string asynchronously.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional function to determine if a property should be an attribute and its XML name.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation with the XML string result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
    public static async Task<string> ToXmlString(this YamlStream yamlStream, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        // Convert directly to XmlDocument
        var xmlDoc = ToXmlDocument(yamlStream, rootElementName, xmlMappingStrategy);

        // Convert to string
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);

        xmlDoc.Save(writer);

#if NET8_0_OR_GREATER
        await writer.FlushAsync(cancellationToken);
#else
        await writer.FlushAsync();
#endif

        stream.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(stream);
#if NET7_0_OR_GREATER
        return await reader.ReadToEndAsync(cancellationToken);
#else
        return await reader.ReadToEndAsync();
#endif
    }

    /// <summary>
    /// Converts a YAML stream to an array of XmlDocument objects.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional function to determine if a property should be an attribute and its XML name.</param>
    /// <returns>An array of XmlDocument objects representing each document in the YAML stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
    public static XmlDocument[] ToXmlDocumentArray(this YamlStream yamlStream, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        var result = new XmlDocument[yamlStream.Documents.Count];
        var mappingStrategy = XmlConverter.GetMappingStrategy(xmlMappingStrategy);

        for (int i = 0; i < yamlStream.Documents.Count; i++)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null));
            var rootElement = xmlDocument.CreateElement(rootElementName);
            xmlDocument.AppendChild(rootElement);

            ConvertYamlNodeToXml(xmlDocument, rootElement, yamlStream.Documents[i].RootNode, rootElementName, mappingStrategy);
            result[i] = xmlDocument;
        }

        return result;
    }

    /// <summary>
    /// Converts a YAML stream to a single XmlDocument.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional function to determine if a property should be an attribute and its XML name.</param>
    /// <returns>An XmlDocument representing the first document in the YAML stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the YAML stream contains multiple documents.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
    public static XmlDocument ToXmlDocument(this YamlStream yamlStream, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        if (yamlStream.Documents.Count > 1)
        {
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToXmlDocumentArray)} to convert all documents.");
        }

        if (yamlStream.Documents.Count == 0)
        {
            var emptyDoc = new XmlDocument();
            emptyDoc.AppendChild(emptyDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            emptyDoc.AppendChild(emptyDoc.CreateElement(rootElementName));
            return emptyDoc;
        }

        var xmlDocument = new XmlDocument();
        xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null));
        var rootElement = xmlDocument.CreateElement(rootElementName);
        xmlDocument.AppendChild(rootElement);

        ConvertYamlNodeToXml(xmlDocument, rootElement, yamlStream.Documents[0].RootNode, rootElementName,
            XmlConverter.GetMappingStrategy(xmlMappingStrategy));

        return xmlDocument;
    }

    /// <summary>
    /// Converts a YAML stream to an array of XDocument objects.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional function to determine if a property should be an attribute and its XML name.</param>
    /// <returns>An array of XDocument objects representing each document in the YAML stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
    public static XDocument[] ToXDocumentArray(this YamlStream yamlStream, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        var result = new XDocument[yamlStream.Documents.Count];
        var mappingStrategy = XmlConverter.GetMappingStrategy(xmlMappingStrategy);

        for (int i = 0; i < yamlStream.Documents.Count; i++)
        {
            var xDocument = new XDocument(new XDeclaration("1.0", "utf-8", null));
            var rootElement = new XElement(rootElementName);
            xDocument.Add(rootElement);

            ConvertYamlNodeToXElement(yamlStream.Documents[i].RootNode, rootElement, rootElementName, mappingStrategy);
            result[i] = xDocument;
        }

        return result;
    }

    /// <summary>
    /// Converts a YAML stream to a single XDocument.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional function to determine if a property should be an attribute and its XML name.</param>
    /// <returns>An XDocument representing the first document in the YAML stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the YAML stream contains multiple documents.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null.</exception>
    public static XDocument ToXDocument(this YamlStream yamlStream, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        if (yamlStream.Documents.Count > 1)
        {
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToXDocumentArray)} to convert all documents.");
        }

        if (yamlStream.Documents.Count == 0)
        {
            return new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(rootElementName));
        }

        var xDocument = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var rootElement = new XElement(rootElementName);
        xDocument.Add(rootElement);

        ConvertYamlNodeToXElement(yamlStream.Documents[0].RootNode, rootElement, rootElementName,
            XmlConverter.GetMappingStrategy(xmlMappingStrategy));

        return xDocument;
    }

    /// <summary>
    /// Converts a YAML node to XML elements within the given parent XML element.
    /// </summary>
    /// <param name="xmlDocument">The XML document that will contain the converted content.</param>
    /// <param name="parentElement">The parent XML element to add content to.</param>
    /// <param name="yamlNode">The YAML node to convert.</param>
    /// <param name="xmlMappingStrategy">Strategy to determine if a property should be an attribute and its XML name.</param>
    private static void ConvertYamlNodeToXml(XmlDocument xmlDocument, XmlElement parentElement,
        YamlNode? yamlNode, string elementName, Func<string, (bool IsAttribute, string Name)> xmlMappingStrategy)
    {
        if (yamlNode == null) return;

        switch (yamlNode)
        {
            case YamlScalarNode scalarNode:
                XmlConverter.CreateXmlInnerText(parentElement, scalarNode.Value);
                break;

            case YamlSequenceNode sequenceNode:
                ProcessYamlSequenceForXml(xmlDocument, parentElement, sequenceNode, elementName, xmlMappingStrategy);
                break;

            case YamlMappingNode mappingNode:
                ProcessYamlMappingForXml(xmlDocument, parentElement, mappingNode, xmlMappingStrategy);
                break;
        }
    }

    /// <summary>
    /// Processes a YAML sequence node and adds its content to an XML element.
    /// </summary>
    private static void ProcessYamlSequenceForXml(XmlDocument xmlDocument, XmlElement parentElement,
        YamlSequenceNode sequenceNode, string elementName, Func<string, (bool IsAttribute, string Name)> xmlMappingStrategy)
    {
        foreach (var item in sequenceNode.Children)
        {
            var itemElement = xmlDocument.CreateElement(elementName);
            parentElement.AppendChild(itemElement);
            ConvertYamlNodeToXml(xmlDocument, itemElement, item, elementName, xmlMappingStrategy);
        }
    }

    /// <summary>
    /// Processes a YAML mapping node and adds its content to an XML element.
    /// </summary>
    private static void ProcessYamlMappingForXml(XmlDocument xmlDocument, XmlElement parentElement,
        YamlMappingNode mappingNode, Func<string, (bool IsAttribute, string Name)> xmlMappingStrategy)
    {
        var allNamedNs = new Dictionary<string, YamlNode>();
        var allNs = new List<(string name, YamlNode value)>();
        var preProps = new List<(bool isAttribute, string name, YamlNode value)>();

        // First pass: collect namespace declarations and preprocess properties
        foreach (var entry in mappingNode.Children)
        {
            if (entry.Key is YamlScalarNode keyNode && !string.IsNullOrEmpty(keyNode.Value))
            {
                var (isAttribute, name) = xmlMappingStrategy(keyNode.Value!);

                if (isAttribute && name.StartsWith("xmlns"))
                {
                    if (entry.Value is YamlScalarNode valueScalarNode)
                    {
                        XmlConverter.ProcessNamespaceAttribute(name, valueScalarNode, allNamedNs, allNs);
                    }
                }
                else
                {
                    preProps.Add((isAttribute, name, entry.Value));
                }
            }
        }

        // Add namespace attributes
        foreach (var (name, value) in allNs)
        {
            if (value is YamlScalarNode valueNode)
            {
                XmlConverter.CreateXmlAttribute(parentElement, name, valueNode.Value, null);
            }
        }

        // Second pass: process properties with namespaces
        foreach (var (isAttribute, name, value) in preProps)
        {
            var validXmlName = XmlConverter.MakeValidXmlName(name);
            string? nsUsed = null;
            string? nsPreName = null;
            string? nsPostName = null;

            if (validXmlName.Contains(':'))
            {
                var nameSplit = validXmlName.Split(':');
                if (nameSplit.Length == 2)
                {
                    nsPreName = nameSplit[0];
                    nsPostName = nameSplit[1];

                    if (allNamedNs.TryGetValue(nsPreName, out var nsValue) && nsValue is YamlScalarNode nsScalarNode)
                    {
                        nsUsed = nsScalarNode.Value;
                    }
                }
            }

            if (isAttribute)
            {
                if (value is YamlScalarNode scalarNode)
                {
                    XmlConverter.CreateXmlAttribute(parentElement, validXmlName, scalarNode.Value, nsUsed);
                }
            }
            else
            {
                if (value is YamlSequenceNode sequenceNode)
                {
                    // For sequences, we maintain the same element name
                    ProcessYamlSequenceForXml(xmlDocument, parentElement, sequenceNode, validXmlName, xmlMappingStrategy);
                }
                else
                {
                    XmlElement childElement;

                    // Fix: Properly handle namespaced elements
                    if (nsUsed != null && nsPostName != null)
                    {
                        // Create element with namespace
                        childElement = xmlDocument.CreateElement(nsPreName, nsPostName, nsUsed);
                    }
                    else
                    {
                        childElement = xmlDocument.CreateElement(validXmlName);
                    }

                    parentElement.AppendChild(childElement);
                    ConvertYamlNodeToXml(xmlDocument, childElement, value, validXmlName, xmlMappingStrategy);
                }
            }
        }
    }

    /// <summary>
    /// Converts a YAML node to XElement objects within the given parent XElement.
    /// </summary>
    /// <param name="yamlNode">The YAML node to convert.</param>
    /// <param name="parentElement">The parent XElement to add content to.</param>
    /// <param name="xmlMappingStrategy">Strategy to determine if a property should be an attribute and its XML name.</param>
    private static void ConvertYamlNodeToXElement(YamlNode? yamlNode, XElement parentElement, string elementName,
        Func<string, (bool IsAttribute, string Name)> xmlMappingStrategy)
    {
        if (yamlNode == null) return;

        switch (yamlNode)
        {
            case YamlScalarNode scalarNode:
                XmlConverter.CreateXInnerText(parentElement, scalarNode.Value);
                break;

            case YamlSequenceNode sequenceNode:
                ProcessYamlSequenceForXElement(parentElement, sequenceNode, elementName, xmlMappingStrategy);
                break;

            case YamlMappingNode mappingNode:
                ProcessYamlMappingForXElement(parentElement, mappingNode, xmlMappingStrategy);
                break;
        }
    }

    /// <summary>
    /// Processes a YAML sequence node and adds its content to an XElement.
    /// </summary>
    private static void ProcessYamlSequenceForXElement(XElement parentElement, YamlSequenceNode sequenceNode, string elementName,
        Func<string, (bool IsAttribute, string Name)> xmlMappingStrategy)
    {
        foreach (var item in sequenceNode.Children)
        {
            var itemElement = new XElement(elementName);
            parentElement.Add(itemElement);
            ConvertYamlNodeToXElement(item, itemElement, elementName, xmlMappingStrategy);
        }
    }

    /// <summary>
    /// Processes a YAML mapping node and adds its content to an XElement.
    /// </summary>
    private static void ProcessYamlMappingForXElement(XElement parentElement, YamlMappingNode mappingNode,
        Func<string, (bool IsAttribute, string Name)> xmlMappingStrategy)
    {
        var allNamedNs = new Dictionary<string, YamlNode>();
        var allNs = new List<(string name, YamlNode value)>();
        var preProps = new List<(bool isAttribute, string name, YamlNode value)>();

        // First pass: collect namespace declarations and preprocess properties
        foreach (var entry in mappingNode.Children)
        {
            if (entry.Key is YamlScalarNode keyNode && !string.IsNullOrEmpty(keyNode.Value))
            {
                var (isAttribute, name) = xmlMappingStrategy(keyNode.Value!);

                if (isAttribute && name.StartsWith("xmlns"))
                {
                    if (entry.Value is YamlScalarNode valueScalarNode)
                    {
                        XmlConverter.ProcessNamespaceAttribute(name, valueScalarNode, allNamedNs, allNs);
                    }
                }
                else
                {
                    preProps.Add((isAttribute, name, entry.Value));
                }
            }
        }

        // Add namespace attributes
        foreach (var (name, value) in allNs)
        {
            if (value is YamlScalarNode valueNode)
            {
                XmlConverter.CreateXAttribute(parentElement, name, valueNode.Value, null);
            }
        }

        // Second pass: process properties with namespaces
        foreach (var (isAttribute, name, value) in preProps)
        {
            var validXmlName = XmlConverter.MakeValidXmlName(name);
            string? nsUsed = null;
            string? nsPreName = null;
            string? nsPostName = null;

            if (validXmlName.Contains(':'))
            {
                var nameSplit = validXmlName.Split(':');
                if (nameSplit.Length == 2)
                {
                    nsPreName = nameSplit[0];
                    nsPostName = nameSplit[1];

                    if (allNamedNs.TryGetValue(nsPreName, out var nsValue) && nsValue is YamlScalarNode nsScalarNode)
                    {
                        nsUsed = nsScalarNode.Value;
                    }
                }
            }

            if (isAttribute)
            {
                if (value is YamlScalarNode scalarNode)
                {
                    XmlConverter.CreateXAttribute(parentElement, validXmlName, scalarNode.Value, nsUsed);
                }
            }
            else
            {
                if (value is YamlSequenceNode sequenceNode)
                {
                    ProcessYamlSequenceForXElement(parentElement, sequenceNode, validXmlName, xmlMappingStrategy);
                }
                else
                {
                    XName xname;

                    // Fix: Properly handle namespaced elements
                    if (nsUsed != null && nsPostName != null)
                    {
                        // Use the proper namespace and local name
                        XNamespace ns = nsUsed;
                        xname = ns + nsPostName;
                    }
                    else
                    {
                        xname = XName.Get(validXmlName);
                    }

                    var childElement = new XElement(xname);
                    parentElement.Add(childElement);
                    ConvertYamlNodeToXElement(value, childElement, validXmlName, xmlMappingStrategy);
                }
            }
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a YamlScalarNode from various object types with type inference.
    /// </summary>
    /// <param name="value">The value to convert to a YamlScalarNode.</param>
    /// <returns>A YamlScalarNode representing the provided value.</returns>
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
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
/// Provides extension methods for converting JSON objects and documents to YAML streams.
/// </summary>
public static class JsonConverter
{
    #region ToJson

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to a <see cref="JsonDocument"/> asynchronously.
    /// </summary>
    /// <param name="jsonNode">The <see cref="JsonNode"/> to convert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="JsonDocument"/> representation of the <see cref="JsonObject"/>.</returns>
    public static async Task<JsonDocument> ToJsonDocument(this JsonNode jsonNode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonNode);

#if NETSTANDARD
        using var stream = new MemoryStream();
#else
        await using var stream = new MemoryStream();
#endif
        await using var utf8JsonWriter = new Utf8JsonWriter(stream);
        jsonNode.WriteTo(utf8JsonWriter);
        await utf8JsonWriter.FlushAsync(cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to a <see cref="JsonDocument"/> asynchronously.
    /// </summary>
    /// <param name="jsonObject">The <see cref="JsonObject"/> to convert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="JsonDocument"/> representation of the <see cref="JsonObject"/>.</returns>
    public static async Task<JsonDocument> ToJsonDocument(this JsonObject jsonObject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonObject);

        return await ToJsonDocument(jsonObject as JsonNode, cancellationToken);
    }

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to a <see cref="JsonDocument"/> asynchronously.
    /// </summary>
    /// <param name="jsonArray">The <see cref="JsonArray"/> to convert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="JsonDocument"/> representation of the <see cref="JsonObject"/>.</returns>
    public static async Task<JsonDocument> ToJsonDocument(this JsonArray jsonArray, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonArray);

        return await ToJsonDocument(jsonArray as JsonNode, cancellationToken);
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to a <see cref="JsonNode"/> asynchronously.
    /// </summary>
    /// <param name="jsonDocument">The <see cref="JsonDocument"/> to convert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="JsonNode"/> representation of the <see cref="JsonDocument"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if parsing the JSON document as a node fails.</exception>
    public static async Task<JsonNode> ToJsonNode(this JsonDocument jsonDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);

#if NETSTANDARD
        using var stream = new MemoryStream();
#else
        await using var stream = new MemoryStream();
#endif
        await using var utf8JsonWriter = new Utf8JsonWriter(stream);
        jsonDocument.WriteTo(utf8JsonWriter);
        await utf8JsonWriter.FlushAsync(cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);
#if NET8_0_OR_GREATER
        return await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Failed to parse JSON document as node.");
#else
        return JsonNode.Parse(stream) ?? throw new InvalidOperationException("Failed to parse JSON document as node.");
#endif
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to a <see cref="JsonObject"/> asynchronously.
    /// </summary>
    /// <param name="jsonDocument">The <see cref="JsonDocument"/> to convert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="JsonObject"/> representation of the <see cref="JsonDocument"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the root element of the JSON document is not an object or if parsing fails.</exception>
    public static async Task<JsonObject> ToJsonObject(this JsonDocument jsonDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);

        if (jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("The JSON document root element is not an object.");
        }
        var jsonNode = await ToJsonNode(jsonDocument, cancellationToken);
        return jsonNode.AsObject();
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to a <see cref="JsonArray"/> asynchronously.
    /// </summary>
    /// <param name="jsonDocument">The <see cref="JsonDocument"/> to convert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="JsonArray"/> representation of the <see cref="JsonDocument"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the root element of the JSON document is not an array or if parsing fails.</exception>
    public static async Task<JsonArray> ToJsonArray(this JsonDocument jsonDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);

        if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("The JSON document root element is not an array.");
        }
        var jsonNode = await ToJsonNode(jsonDocument, cancellationToken);
        return jsonNode.AsArray();
    }

#endregion

    #region ToYaml

    /// <summary>
    /// Converts a collection of <see cref="JsonNode"/> instances to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="jsonNodes">The collection of <see cref="JsonNode"/> to convert.</param>
    /// <returns>A <see cref="YamlStream"/> representing the converted JSON nodes.</returns>
    public static YamlStream ToYamlStream(this IEnumerable<JsonNode?> jsonNodes)
    {
        ArgumentNullException.ThrowIfNull(jsonNodes);

        static YamlNode ConvertJsonNodeToYamlNode(JsonNode? node)
        {
            if (node == null)
            {
                return new YamlScalarNode();
            }

#if NET8_0_OR_GREATER
            return node.GetValueKind() switch
#else
            return node.GetValue<JsonElement>().ValueKind switch
#endif
            {
                JsonValueKind.Object => CreateYamlMapping(node),
                JsonValueKind.Array => CreateYamlSequence(node),
                JsonValueKind.String => new YamlScalarNode(node.GetValue<string>()),
                JsonValueKind.Number => CreateYamlNumberNode(node),
                JsonValueKind.True => new YamlScalarNode("true"),
                JsonValueKind.False => new YamlScalarNode("false"),
                _ => new YamlScalarNode()
            };
        }

        static YamlMappingNode CreateYamlMapping(JsonNode node)
        {
            var mapping = new YamlMappingNode();
            foreach (var prop in node.AsObject())
            {
                mapping.Add(new YamlScalarNode(prop.Key), ConvertJsonNodeToYamlNode(prop.Value));
            }
            return mapping;
        }

        static YamlSequenceNode CreateYamlSequence(JsonNode node)
        {
            var sequence = new YamlSequenceNode();
            foreach (var item in node.AsArray())
            {
                sequence.Add(ConvertJsonNodeToYamlNode(item));
            }
            return sequence;
        }

        static YamlScalarNode CreateYamlNumberNode(JsonNode node)
        {
#if NET8_0_OR_GREATER
            // Preserve the original number format when possible
            if (node is JsonValue jsonValue)
            {
                if (jsonValue.TryGetValue(out int intValue))
                    return new YamlScalarNode(intValue.ToString());
                if (jsonValue.TryGetValue(out long longValue))
                    return new YamlScalarNode(longValue.ToString());
                if (jsonValue.TryGetValue(out decimal decimalValue))
                    return new YamlScalarNode(decimalValue.ToString());
            }
#endif
            // Fallback to double if specific type extraction isn't available
            return new YamlScalarNode(node.GetValue<double>().ToString());
        }

        var yamlStream = new YamlStream();

        foreach (var jsonNode in jsonNodes)
        {
            var yamlRoot = ConvertJsonNodeToYamlNode(jsonNode);
            var yamlDoc = new YamlDocument(yamlRoot);
            yamlStream.Add(yamlDoc);
        }

        return yamlStream;
    }

    /// <summary>
    /// Converts a collection of <see cref="JsonObject"/> instances to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="jsonObjects">The collection of <see cref="JsonObject"/> to convert.</param>
    /// <returns>A <see cref="YamlStream"/> representing the converted JSON objects.</returns>
    public static YamlStream ToYamlStream(this IEnumerable<JsonObject?> jsonObjects)
    {
        ArgumentNullException.ThrowIfNull(jsonObjects);

        return ToYamlStream(jsonObjects.AsEnumerable<JsonNode?>());
    }

    /// <summary>
    /// Converts a collection of <see cref="JsonArray"/> instances to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="jsonArrays">The collection of <see cref="JsonArray"/> to convert.</param>
    /// <returns>A <see cref="YamlStream"/> representing the converted JSON objects.</returns>
    public static YamlStream ToYamlStream(this IEnumerable<JsonArray?> jsonArrays)
    {
        ArgumentNullException.ThrowIfNull(jsonArrays);

        return ToYamlStream(jsonArrays.AsEnumerable<JsonNode?>());
    }

    /// <summary>
    /// Converts a collection of <see cref="JsonDocument"/> instances to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="jsonDocuments">The collection of <see cref="JsonDocument"/> to convert.</param>
    /// <returns>A <see cref="YamlStream"/> representing the converted JSON documents.</returns>
    public static YamlStream ToYamlStream(this IEnumerable<JsonDocument?> jsonDocuments)
    {
        ArgumentNullException.ThrowIfNull(jsonDocuments);

        static YamlNode ConvertJsonElementToYamlNode(JsonElement? element)
        {
            if (!element.HasValue)
            {
                return new YamlScalarNode();
            }

            return element.Value.ValueKind switch
            {
                JsonValueKind.Object => CreateYamlMapping(element.Value),
                JsonValueKind.Array => CreateYamlSequence(element.Value),
                JsonValueKind.String => new YamlScalarNode(element.Value.GetString()),
                JsonValueKind.Number => CreateYamlNumberNode(element.Value),
                JsonValueKind.True => new YamlScalarNode("true"),
                JsonValueKind.False => new YamlScalarNode("false"),
                _ => new YamlScalarNode()
            };
        }

        static YamlMappingNode CreateYamlMapping(JsonElement element)
        {
            var mapping = new YamlMappingNode();
            foreach (var prop in element.EnumerateObject())
            {
                mapping.Add(new YamlScalarNode(prop.Name), ConvertJsonElementToYamlNode(prop.Value));
            }
            return mapping;
        }

        static YamlSequenceNode CreateYamlSequence(JsonElement element)
        {
            var sequence = new YamlSequenceNode();
            foreach (var item in element.EnumerateArray())
            {
                sequence.Add(ConvertJsonElementToYamlNode(item));
            }
            return sequence;
        }

        static YamlScalarNode CreateYamlNumberNode(JsonElement element)
        {
#if NET8_0_OR_GREATER
            // Preserve the original number format when possible
            if (element.TryGetInt32(out int intValue))
                return new YamlScalarNode(intValue.ToString());
            if (element.TryGetInt64(out long longValue))
                return new YamlScalarNode(longValue.ToString());
            if (element.TryGetDecimal(out decimal decimalValue))
                return new YamlScalarNode(decimalValue.ToString());
#endif
            // Fallback to double if specific type extraction isn't available
            return new YamlScalarNode(element.GetDouble().ToString());
        }

        var yamlStream = new YamlStream();

        foreach (var jsonDocument in jsonDocuments)
        {
            var yamlRoot = ConvertJsonElementToYamlNode(jsonDocument?.RootElement);
            var yamlDoc = new YamlDocument(yamlRoot);
            yamlStream.Add(yamlDoc);
        }

        return yamlStream;
    }

    /// <summary>
    /// Converts a single <see cref="JsonNode"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="jsonNode">The <see cref="JsonNode"/> to convert.</param>
    /// <returns>A <see cref="YamlStream"/> representing the converted JSON object.</returns>
    public static YamlStream ToYamlStream(this JsonNode jsonNode)
    {
        ArgumentNullException.ThrowIfNull(jsonNode);

        return new[] { jsonNode }.ToYamlStream();
    }

    /// <summary>
    /// Converts a single <see cref="JsonArray"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="jsonArray">The <see cref="JsonArray"/> to convert.</param>
    /// <returns>A <see cref="YamlStream"/> representing the converted JSON object.</returns>
    public static YamlStream ToYamlStream(this JsonArray jsonArray)
    {
        ArgumentNullException.ThrowIfNull(jsonArray);

        return new[] { jsonArray }.ToYamlStream();
    }

    /// <summary>
    /// Converts a single <see cref="JsonDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="jsonDocument">The <see cref="JsonDocument"/> to convert.</param>
    /// <returns>A <see cref="YamlStream"/> representing the converted JSON document.</returns>
    public static YamlStream ToYamlStream(this JsonDocument jsonDocument)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);

        return new[] { jsonDocument }.ToYamlStream();
    }

    #endregion

    #region ToXml

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="jsonNode">The <see cref="JsonNode"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XmlDocument"/> representing the converted JSON node.</returns>
    public static XmlDocument ToXmlDocument(this JsonNode jsonNode, string rootElementName = "root", string arrayItemElementName = "item")
    {
        ArgumentNullException.ThrowIfNull(jsonNode);

        var document = new XmlDocument();
        document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
        var root = document.CreateElement(rootElementName);
        document.AppendChild(root);

        AddJsonNodeToXmlDocument(document, root, jsonNode, arrayItemElementName);

        return document;
    }

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="jsonObject">The <see cref="JsonObject"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XmlDocument"/> representing the converted JSON object.</returns>
    public static XmlDocument ToXmlDocument(this JsonObject jsonObject, string rootElementName = "root", string arrayItemElementName = "item")
    {
        ArgumentNullException.ThrowIfNull(jsonObject);

        return ToXmlDocument(jsonObject as JsonNode, rootElementName, arrayItemElementName);
    }

    /// <summary>
    /// Converts a <see cref="JsonArray"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="jsonArray">The <see cref="JsonArray"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XmlDocument"/> representing the converted JSON array.</returns>
    public static XmlDocument ToXmlDocument(this JsonArray jsonArray, string rootElementName = "root", string arrayItemElementName = "item")
    {
        ArgumentNullException.ThrowIfNull(jsonArray);

        var document = new XmlDocument();
        document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
        var root = document.CreateElement(rootElementName);
        document.AppendChild(root);

        foreach (var item in jsonArray)
        {
            var itemElement = document.CreateElement(arrayItemElementName);
            root.AppendChild(itemElement);
            AddJsonNodeToXmlDocument(document, itemElement, item, arrayItemElementName);
        }

        return document;
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="jsonDocument">The <see cref="JsonDocument"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XmlDocument"/> representing the converted JSON document.</returns>
    public static XmlDocument ToXmlDocument(this JsonDocument jsonDocument, string rootElementName = "root", string arrayItemElementName = "item")
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);

        var document = new XmlDocument();
        document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
        var root = document.CreateElement(rootElementName);
        document.AppendChild(root);

        AddJsonElementToXmlDocument(document, root, jsonDocument.RootElement, arrayItemElementName);

        return document;
    }

    // Helper methods for XmlDocument
    private static void AddJsonNodeToXmlDocument(XmlDocument document, XmlElement parent, JsonNode? node, string arrayItemElementName)
    {
        if (node == null)
            return;

#if NET8_0_OR_GREATER
        var valueKind = node.GetValueKind();
#else
        var valueKind = node.GetValue<JsonElement>().ValueKind;
#endif

        switch (valueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in node.AsObject())
                {
                    var validXmlName = MakeValidXmlName(property.Key);
                    var childElement = document.CreateElement(validXmlName);
                    parent.AppendChild(childElement);
                    AddJsonNodeToXmlDocument(document, childElement, property.Value, arrayItemElementName);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in node.AsArray())
                {
                    var childElement = document.CreateElement(arrayItemElementName);
                    parent.AppendChild(childElement);
                    AddJsonNodeToXmlDocument(document, childElement, item, arrayItemElementName);
                }
                break;

            case JsonValueKind.String:
                parent.InnerText = node.GetValue<string>() ?? string.Empty;
                break;

            case JsonValueKind.Number:
#if NET8_0_OR_GREATER
                if (node is JsonValue jsonValue)
                {
                    if (jsonValue.TryGetValue(out int intValue))
                        parent.InnerText = intValue.ToString();
                    else if (jsonValue.TryGetValue(out long longValue))
                        parent.InnerText = longValue.ToString();
                    else if (jsonValue.TryGetValue(out decimal decimalValue))
                        parent.InnerText = decimalValue.ToString();
                    else
                        parent.InnerText = node.GetValue<double>().ToString();
                }
                else
                {
                    parent.InnerText = node.GetValue<double>().ToString();
                }
#else
                parent.InnerText = node.GetValue<double>().ToString();
#endif
                break;

            case JsonValueKind.True:
                parent.InnerText = "true";
                break;

            case JsonValueKind.False:
                parent.InnerText = "false";
                break;

            case JsonValueKind.Null:
                // For null values, we leave the element empty
                break;
        }
    }

    private static void AddJsonElementToXmlDocument(XmlDocument document, XmlElement parent, JsonElement element, string arrayItemElementName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var validXmlName = MakeValidXmlName(property.Name);
                    var childElement = document.CreateElement(validXmlName);
                    parent.AppendChild(childElement);
                    AddJsonElementToXmlDocument(document, childElement, property.Value, arrayItemElementName);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var childElement = document.CreateElement(arrayItemElementName);
                    parent.AppendChild(childElement);
                    AddJsonElementToXmlDocument(document, childElement, item, arrayItemElementName);
                }
                break;

            case JsonValueKind.String:
                parent.InnerText = element.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Number:
#if NET8_0_OR_GREATER
                if (element.TryGetInt32(out int intValue))
                    parent.InnerText = intValue.ToString();
                else if (element.TryGetInt64(out long longValue))
                    parent.InnerText = longValue.ToString();
                else if (element.TryGetDecimal(out decimal decimalValue))
                    parent.InnerText = decimalValue.ToString();
                else
                    parent.InnerText = element.GetDouble().ToString();
#else
                parent.InnerText = element.GetDouble().ToString();
#endif
                break;

            case JsonValueKind.True:
                parent.InnerText = "true";
                break;

            case JsonValueKind.False:
                parent.InnerText = "false";
                break;

            case JsonValueKind.Null:
                // For null values, we leave the element empty
                break;
        }
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to an <see cref="XDocument"/>.
    /// </summary>
    /// <param name="jsonNode">The <see cref="JsonNode"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XDocument"/> representing the converted JSON node.</returns>
    public static XDocument ToXDocument(this JsonNode jsonNode, string rootElementName = "root", string arrayItemElementName = "item")
    {
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var root = new XElement(rootElementName);
        document.Add(root);

        AddJsonNodeToXml(root, jsonNode, arrayItemElementName);

        return document;
    }

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to an <see cref="XDocument"/>.
    /// </summary>
    /// <param name="jsonObject">The <see cref="JsonObject"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XDocument"/> representing the converted JSON object.</returns>
    public static XDocument ToXDocument(this JsonObject jsonObject, string rootElementName = "root", string arrayItemElementName = "item")
    {
        return ToXDocument(jsonObject as JsonNode, rootElementName, arrayItemElementName);
    }

    /// <summary>
    /// Converts a <see cref="JsonArray"/> to an <see cref="XDocument"/>.
    /// </summary>
    /// <param name="jsonArray">The <see cref="JsonArray"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XDocument"/> representing the converted JSON array.</returns>
    public static XDocument ToXDocument(this JsonArray jsonArray, string rootElementName = "root", string arrayItemElementName = "item")
    {
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var root = new XElement(rootElementName);
        document.Add(root);

        foreach (var item in jsonArray)
        {
            var itemElement = new XElement(arrayItemElementName);
            root.Add(itemElement);
            AddJsonNodeToXml(itemElement, item, arrayItemElementName);
        }

        return document;
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to an <see cref="XDocument"/>.
    /// </summary>
    /// <param name="jsonDocument">The <see cref="JsonDocument"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XDocument"/> representing the converted JSON document.</returns>
    public static XDocument ToXDocument(this JsonDocument jsonDocument, string rootElementName = "root", string arrayItemElementName = "item")
    {
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var root = new XElement(rootElementName);
        document.Add(root);

        AddJsonElementToXml(root, jsonDocument.RootElement, arrayItemElementName);

        return document;
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to an <see cref="XElement"/>.
    /// </summary>
    /// <param name="jsonNode">The <see cref="JsonNode"/> to convert.</param>
    /// <param name="elementName">The name of the XML element. Defaults to "element".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XElement"/> representing the converted JSON node.</returns>
    public static XElement ToXElement(this JsonNode jsonNode, string elementName = "element", string arrayItemElementName = "item")
    {
        var element = new XElement(elementName);
        AddJsonNodeToXml(element, jsonNode, arrayItemElementName);
        return element;
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to an <see cref="XElement"/>.
    /// </summary>
    /// <param name="jsonDocument">The <see cref="JsonDocument"/> to convert.</param>
    /// <param name="elementName">The name of the XML element. Defaults to "element".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <returns>An <see cref="XElement"/> representing the converted JSON document.</returns>
    public static XElement ToXElement(this JsonDocument jsonDocument, string elementName = "element", string arrayItemElementName = "item")
    {
        var element = new XElement(elementName);
        AddJsonElementToXml(element, jsonDocument.RootElement, arrayItemElementName);
        return element;
    }

    // Helper methods
    private static void AddJsonNodeToXml(XElement parent, JsonNode? node, string arrayItemElementName)
    {
        if (node == null)
            return;

#if NET8_0_OR_GREATER
        var valueKind = node.GetValueKind();
#else
        var valueKind = node.GetValue<JsonElement>().ValueKind;
#endif

        switch (valueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in node.AsObject())
                {
                    var validXmlName = MakeValidXmlName(property.Key);
                    var childElement = new XElement(validXmlName);
                    parent.Add(childElement);
                    AddJsonNodeToXml(childElement, property.Value, arrayItemElementName);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in node.AsArray())
                {
                    var childElement = new XElement(arrayItemElementName);
                    parent.Add(childElement);
                    AddJsonNodeToXml(childElement, item, arrayItemElementName);
                }
                break;

            case JsonValueKind.String:
                parent.Value = node.GetValue<string>() ?? string.Empty;
                break;

            case JsonValueKind.Number:
#if NET8_0_OR_GREATER
                if (node is JsonValue jsonValue)
                {
                    if (jsonValue.TryGetValue(out int intValue))
                        parent.Value = intValue.ToString();
                    else if (jsonValue.TryGetValue(out long longValue))
                        parent.Value = longValue.ToString();
                    else if (jsonValue.TryGetValue(out decimal decimalValue))
                        parent.Value = decimalValue.ToString();
                    else
                        parent.Value = node.GetValue<double>().ToString();
                }
                else
                {
                    parent.Value = node.GetValue<double>().ToString();
                }
#else
                parent.Value = node.GetValue<double>().ToString();
#endif
                break;

            case JsonValueKind.True:
                parent.Value = "true";
                break;

            case JsonValueKind.False:
                parent.Value = "false";
                break;

            case JsonValueKind.Null:
                // For null values, we leave the element empty
                break;
        }
    }

    private static void AddJsonElementToXml(XElement parent, JsonElement element, string arrayItemElementName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var validXmlName = MakeValidXmlName(property.Name);
                    var childElement = new XElement(validXmlName);
                    parent.Add(childElement);
                    AddJsonElementToXml(childElement, property.Value, arrayItemElementName);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var childElement = new XElement(arrayItemElementName);
                    parent.Add(childElement);
                    AddJsonElementToXml(childElement, item, arrayItemElementName);
                }
                break;

            case JsonValueKind.String:
                parent.Value = element.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Number:
#if NET8_0_OR_GREATER
                if (element.TryGetInt32(out int intValue))
                    parent.Value = intValue.ToString();
                else if (element.TryGetInt64(out long longValue))
                    parent.Value = longValue.ToString();
                else if (element.TryGetDecimal(out decimal decimalValue))
                    parent.Value = decimalValue.ToString();
                else
                    parent.Value = element.GetDouble().ToString();
#else
                parent.Value = element.GetDouble().ToString();
#endif
                break;

            case JsonValueKind.True:
                parent.Value = "true";
                break;

            case JsonValueKind.False:
                parent.Value = "false";
                break;

            case JsonValueKind.Null:
                // For null values, we leave the element empty
                break;
        }
    }

    private static string MakeValidXmlName(string name)
    {
        // XML element names cannot start with a number, contain spaces or special characters
        if (string.IsNullOrEmpty(name))
            return "element";

        // Replace invalid characters with underscore
        var validChars = new char[name.Length];
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            validChars[i] = XmlConvert.IsXmlChar(c) ? c : '_';
        }

        string result = new(validChars);

        // If the name starts with a number or invalid starting character, prefix it
        if (result.Length > 0 && !XmlConvert.IsStartNCNameChar(result[0]))
            result = "x_" + result;

        return result == string.Empty ? "element" : result;
    }

    internal static JsonValue? CreateJsonValue(string? value)
    {
        if (bool.TryParse(value, out var boolVal))
            return JsonValue.Create(boolVal);
        if (int.TryParse(value, out var intVal))
            return JsonValue.Create(intVal);
        if (long.TryParse(value, out var longVal))
            return JsonValue.Create(longVal);
        if (double.TryParse(value, out var doubleVal))
            return JsonValue.Create(doubleVal);
        if (decimal.TryParse(value, out var decimalVal))
            return JsonValue.Create(decimalVal);
        if (DateTime.TryParse(value, out var dateTimeVal))
            return JsonValue.Create(dateTimeVal);
        if (DateTimeOffset.TryParse(value, out var dateTimeOffsetVal))
            return JsonValue.Create(dateTimeOffsetVal);
        return JsonValue.Create(value);
    }

    #endregion
}
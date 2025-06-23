using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

#if NETSTANDARD
using ArgumentNullException = MultiFormatDataConverter.Polyfill.ArgumentNullException;
#else
using ArgumentNullException = System.ArgumentNullException;
#endif

namespace MultiFormatDataConverter;

/// <summary>
/// Provides extension methods for converting JSON objects and documents to YAML, XML, and LINQ to XML formats.
/// </summary>
public static class JsonConverter
{
    #region ToJson

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to a <see cref="JsonDocument"/> asynchronously.
    /// </summary>
    public static async Task<JsonDocument> ToJsonDocument(this JsonNode jsonNode, CancellationToken cancellationToken = default)
        => await WriteAndParseAsync(jsonNode, cancellationToken);

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to a <see cref="JsonDocument"/> asynchronously.
    /// </summary>
    public static Task<JsonDocument> ToJsonDocument(this JsonObject jsonObject, CancellationToken cancellationToken = default)
        => ToJsonDocument(jsonObject as JsonNode, cancellationToken);

    /// <summary>
    /// Converts a <see cref="JsonArray"/> to a <see cref="JsonDocument"/> asynchronously.
    /// </summary>
    public static Task<JsonDocument> ToJsonDocument(this JsonArray jsonArray, CancellationToken cancellationToken = default)
        => ToJsonDocument(jsonArray as JsonNode, cancellationToken);

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to a <see cref="JsonNode"/> asynchronously.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if parsing fails.</exception>
    public static async Task<JsonNode> ToJsonNode(this JsonDocument jsonDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);
        using var stream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(stream);
        jsonDocument.WriteTo(writer);
        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;
#if NET8_0_OR_GREATER
        return await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to parse JSON document as node.");
#else
        return JsonNode.Parse(stream)
            ?? throw new InvalidOperationException("Failed to parse JSON document as node.");
#endif
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to a <see cref="JsonObject"/> asynchronously.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the root is not an object.</exception>
    public static async Task<JsonObject> ToJsonObject(this JsonDocument jsonDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);
        if (jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("The JSON document root element is not an object.");
        var node = await ToJsonNode(jsonDocument, cancellationToken);
        return node.AsObject();
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to a <see cref="JsonArray"/> asynchronously.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the root is not an array.</exception>
    public static async Task<JsonArray> ToJsonArray(this JsonDocument jsonDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);
        if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("The JSON document root element is not an array.");
        var node = await ToJsonNode(jsonDocument, cancellationToken);
        return node.AsArray();
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> to a <see cref="JsonNode"/> asynchronously.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if parsing fails.</exception>
    public static async Task<JsonNode> ToJsonNode(this JsonElement element, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(stream);
        element.WriteTo(writer);
        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;
#if NET8_0_OR_GREATER
        return await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to parse JSON element as node.");
#else
        return JsonNode.Parse(stream)
            ?? throw new InvalidOperationException("Failed to parse JSON element as node.");
#endif
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> to a <see cref="JsonObject"/> asynchronously.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the element is not an object.</exception>
    public static async Task<JsonObject> ToJsonObject(this JsonElement element, CancellationToken cancellationToken = default)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("The JSON element is not an object.");
        var node = await ToJsonNode(element, cancellationToken);
        return node.AsObject();
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> to a <see cref="JsonArray"/> asynchronously.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the element is not an array.</exception>
    public static async Task<JsonArray> ToJsonArray(this JsonElement element, CancellationToken cancellationToken = default)
    {
        if (element.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("The JSON element is not an array.");
        var node = await ToJsonNode(element, cancellationToken);
        return node.AsArray();
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to a <see cref="JsonElement"/> asynchronously.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if the node is null.</exception>
    public static async Task<JsonElement> ToJsonElement(this JsonNode jsonNode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonNode);
        var document = await jsonNode.ToJsonDocument(cancellationToken);
        return document.RootElement;
    }

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to a <see cref="JsonElement"/> asynchronously.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if the object is null.</exception>
    public static Task<JsonElement> ToJsonElement(this JsonObject jsonObject, CancellationToken cancellationToken = default)
        => ToJsonElement(jsonObject as JsonNode, cancellationToken);

    /// <summary>
    /// Converts a <see cref="JsonArray"/> to a <see cref="JsonElement"/> asynchronously.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if the array is null.</exception>
    public static Task<JsonElement> ToJsonElement(this JsonArray jsonArray, CancellationToken cancellationToken = default)
        => ToJsonElement(jsonArray as JsonNode, cancellationToken);

    private static async Task<JsonDocument> WriteAndParseAsync(JsonNode jsonNode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jsonNode);
        using var stream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(stream);
        jsonNode.WriteTo(writer);
        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    #endregion

    #region ToYaml

    /// <summary>
    /// Converts a JSON string to a YAML string asynchronously.
    /// </summary>
    public static async Task<string> ToYaml(string jsonString, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonString);
        var jsonNode = await ParseJsonStringToNode(jsonString, cancellationToken);
        var yamlStream = ToYamlStream(jsonNode);
        return await YamlStreamToStringAsync(yamlStream, cancellationToken);
    }

    /// <summary>
    /// Converts a collection of <see cref="JsonNode"/> to a <see cref="YamlStream"/>.
    /// </summary>
    public static YamlStream ToYamlStream(this IEnumerable<JsonNode?> jsonNodes)
    {
        ArgumentNullException.ThrowIfNull(jsonNodes);
        var yamlStream = new YamlStream();
        foreach (var node in jsonNodes)
            yamlStream.Add(new YamlDocument(ConvertJsonNodeToYamlNode(node)));
        return yamlStream;
    }

    /// <summary>
    /// Converts a collection of <see cref="JsonObject"/> to a <see cref="YamlStream"/>.
    /// </summary>
    public static YamlStream ToYamlStream(this IEnumerable<JsonObject?> jsonObjects)
        => ToYamlStream(jsonObjects.Cast<JsonNode?>());

    /// <summary>
    /// Converts a collection of <see cref="JsonArray"/> to a <see cref="YamlStream"/>.
    /// </summary>
    public static YamlStream ToYamlStream(this IEnumerable<JsonArray?> jsonArrays)
        => ToYamlStream(jsonArrays.Cast<JsonNode?>());

    /// <summary>
    /// Converts a collection of <see cref="JsonDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    public static YamlStream ToYamlStream(this IEnumerable<JsonDocument?> jsonDocuments)
    {
        ArgumentNullException.ThrowIfNull(jsonDocuments);
        var yamlStream = new YamlStream();
        foreach (var doc in jsonDocuments)
            yamlStream.Add(new YamlDocument(ConvertJsonElementToYamlNode(doc?.RootElement)));
        return yamlStream;
    }

    /// <summary>
    /// Converts a single <see cref="JsonNode"/> to a <see cref="YamlStream"/>.
    /// </summary>
    public static YamlStream ToYamlStream(this JsonNode jsonNode)
    {
        ArgumentNullException.ThrowIfNull(jsonNode);
        return new[] { jsonNode }.ToYamlStream();
    }

    /// <summary>
    /// Converts a single <see cref="JsonArray"/> to a <see cref="YamlStream"/>.
    /// </summary>
    public static YamlStream ToYamlStream(this JsonArray jsonArray)
        => new[] { jsonArray }.ToYamlStream();

    /// <summary>
    /// Converts a single <see cref="JsonDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    public static YamlStream ToYamlStream(this JsonDocument jsonDocument)
        => new[] { jsonDocument }.ToYamlStream();

    private static async Task<JsonNode> ParseJsonStringToNode(string jsonString, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
#if NETSTANDARD
        await writer.WriteAsync(jsonString);
        await writer.FlushAsync();
#else
        await writer.WriteAsync(jsonString.AsMemory(), cancellationToken);
#if NET8_0_OR_GREATER
        await writer.FlushAsync(cancellationToken);
#else
        await writer.FlushAsync();
#endif
#endif
        stream.Position = 0;
#if NET8_0_OR_GREATER
        return await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to parse JSON string as node.");
#else
        return JsonNode.Parse(stream)
            ?? throw new InvalidOperationException("Failed to parse JSON string as node.");
#endif
    }

    private static async Task<string> YamlStreamToStringAsync(YamlStream yamlStream, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        yamlStream.Save(writer, false);
#if NET8_0_OR_GREATER
        await writer.FlushAsync(cancellationToken);
#else
        await writer.FlushAsync();
#endif
        stream.Position = 0;
        using var reader = new StreamReader(stream);
#if NET7_0_OR_GREATER
        return await reader.ReadToEndAsync(cancellationToken);
#else
        return await reader.ReadToEndAsync();
#endif
    }

    private static YamlNode ConvertJsonNodeToYamlNode(JsonNode? node)
    {
        if (node == null) return new YamlScalarNode();
#if NET8_0_OR_GREATER
        var valueKind = node.GetValueKind();
#else
        var valueKind = node.GetValue<JsonElement>().ValueKind;
#endif
        switch (valueKind)
        {
            case JsonValueKind.Object:
                var obj = node.AsObject();
                var mapping = new YamlMappingNode();
                foreach (var prop in obj)
                    mapping.Add(new YamlScalarNode(prop.Key), ConvertJsonNodeToYamlNode(prop.Value));
                return mapping;
            case JsonValueKind.Array:
                return new YamlSequenceNode(node.AsArray().Select(ConvertJsonNodeToYamlNode).ToList());
            default:
                return YamlConverter.CreateYamlScalarNode(node);
        }
    }

    private static YamlNode ConvertJsonElementToYamlNode(JsonElement? element)
    {
        if (!element.HasValue) return new YamlScalarNode();
        switch (element.Value.ValueKind)
        {
            case JsonValueKind.Object:
                var mapping = new YamlMappingNode();
                foreach (var prop in element.Value.EnumerateObject())
                    mapping.Add(new YamlScalarNode(prop.Name), ConvertJsonElementToYamlNode(prop.Value));
                return mapping;
            case JsonValueKind.Array:
                var sequence = new YamlSequenceNode();
                foreach (var item in element.Value.EnumerateArray())
                    sequence.Add(ConvertJsonElementToYamlNode(item));
                return sequence;
            default:
                return YamlConverter.CreateYamlScalarNode(element);
        }
    }

    #endregion

    #region ToXml

    /// <summary>
    /// Converts a JSON string to an XML string asynchronously.
    /// </summary>
    public static async Task<string> ToXml(string jsonString, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonString);
        var jsonNode = await ParseJsonStringToNode(jsonString, cancellationToken);
        var xmlDocument = jsonNode.ToXmlDocument(rootElementName, xmlMappingStrategy);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        xmlDocument.Save(writer);
#if NET8_0_OR_GREATER
        await writer.FlushAsync(cancellationToken);
#else
        await writer.FlushAsync();
#endif
        stream.Position = 0;
        using var reader = new StreamReader(stream);
#if NET7_0_OR_GREATER
        return await reader.ReadToEndAsync(cancellationToken);
#else
        return await reader.ReadToEndAsync();
#endif
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    public static XmlDocument ToXmlDocument(this JsonNode jsonNode, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(jsonNode);
        var doc = new XmlDocument();
        doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
        var root = doc.CreateElement(rootElementName);
        doc.AppendChild(root);
        AddJsonNodeToXmlDocument(doc, root, rootElementName, jsonNode, XmlConverter.GetMappingStrategy(xmlMappingStrategy));
        return doc;
    }

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    public static XmlDocument ToXmlDocument(this JsonObject jsonObject, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
        => ToXmlDocument(jsonObject as JsonNode, rootElementName, xmlMappingStrategy);

    /// <summary>
    /// Converts a <see cref="JsonArray"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    public static XmlDocument ToXmlDocument(this JsonArray jsonArray, string rootElementName = "root",
        string arrayItemElementName = "item",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(jsonArray);
        var doc = new XmlDocument();
        doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
        var root = doc.CreateElement(rootElementName);
        doc.AppendChild(root);
        var mapping = XmlConverter.GetMappingStrategy(xmlMappingStrategy);
        foreach (var item in jsonArray)
        {
            var itemElem = doc.CreateElement(arrayItemElementName);
            root.AppendChild(itemElem);
            AddJsonNodeToXmlDocument(doc, itemElem, arrayItemElementName, item, mapping);
        }
        return doc;
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    public static XmlDocument ToXmlDocument(this JsonDocument jsonDocument, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);
        var doc = new XmlDocument();
        doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
        var root = doc.CreateElement(rootElementName);
        doc.AppendChild(root);
        AddJsonElementToXmlDocument(doc, root, rootElementName, jsonDocument.RootElement, XmlConverter.GetMappingStrategy(xmlMappingStrategy));
        return doc;
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to an <see cref="XDocument"/>.
    /// </summary>
    public static XDocument ToXDocument(this JsonNode jsonNode, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(jsonNode);
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var root = new XElement(rootElementName);
        doc.Add(root);
        AddJsonNodeToXml(root, jsonNode, rootElementName, XmlConverter.GetMappingStrategy(xmlMappingStrategy));
        return doc;
    }

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to an <see cref="XDocument"/>.
    /// </summary>
    public static XDocument ToXDocument(this JsonObject jsonObject, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
        => ToXDocument(jsonObject as JsonNode, rootElementName, xmlMappingStrategy);

    /// <summary>
    /// Converts a <see cref="JsonArray"/> to an <see cref="XDocument"/>.
    /// </summary>
    public static XDocument ToXDocument(this JsonArray jsonArray, string rootElementName = "root",
        string arrayItemElementName = "item",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(jsonArray);
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var root = new XElement(rootElementName);
        doc.Add(root);
        var mapping = XmlConverter.GetMappingStrategy(xmlMappingStrategy);
        foreach (var item in jsonArray)
        {
            var itemElem = new XElement(arrayItemElementName);
            root.Add(itemElem);
            AddJsonNodeToXml(itemElem, item, arrayItemElementName, mapping);
        }
        return doc;
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to an <see cref="XDocument"/>.
    /// </summary>
    public static XDocument ToXDocument(this JsonDocument jsonDocument, string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var root = new XElement(rootElementName);
        doc.Add(root);
        AddJsonElementToXml(root, jsonDocument.RootElement, rootElementName, XmlConverter.GetMappingStrategy(xmlMappingStrategy));
        return doc;
    }

    // --- Internal helpers for JSON to XML/XDocument conversion ---

    private static void AddJsonNodeToXmlDocument(XmlDocument doc, XmlElement parent, string elementName,
        JsonNode? node, Func<string, (bool IsAttribute, string Name)> mapping)
    {
        if (node == null) return;
#if NET8_0_OR_GREATER
        var valueKind = node.GetValueKind();
#else
        var valueKind = node.GetValue<JsonElement>().ValueKind;
#endif
        switch (valueKind)
        {
            case JsonValueKind.Object:
                AddJsonObjectToXmlDocument(doc, parent, node, mapping);
                break;
            case JsonValueKind.Array:
                foreach (var item in node.AsArray())
                {
                    var child = doc.CreateElement(elementName);
                    parent.AppendChild(child);
                    AddJsonNodeToXmlDocument(doc, child, elementName, item, mapping);
                }
                break;
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                XmlConverter.CreateXmlInnerText(parent, node);
                break;
            case JsonValueKind.Null:
                break;
        }
    }

    private static void AddJsonObjectToXmlDocument(XmlDocument doc, XmlElement parent, JsonNode node,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        var (allNamedNs, allNs, props) = XmlPreloadJsonNode(node, mapping);
        foreach (var (name, value) in allNs)
            XmlConverter.CreateXmlAttribute(parent, name, value, null);
        foreach (var (isAttr, name, validXmlName, nsUsed, _, _, value) in props)
        {
            if (isAttr)
                XmlConverter.CreateXmlAttribute(parent, name, value, nsUsed);
            else if (value?.GetValueKind() == JsonValueKind.Array)
                AddJsonNodeToXmlDocument(doc, parent, validXmlName, value, mapping);
            else
            {
                var child = nsUsed != null ? doc.CreateElement(validXmlName, nsUsed) : doc.CreateElement(validXmlName);
                parent.AppendChild(child);
                AddJsonNodeToXmlDocument(doc, child, validXmlName, value, mapping);
            }
        }
    }

    private static void AddJsonElementToXmlDocument(XmlDocument doc, XmlElement parent, string elementName,
        JsonElement element, Func<string, (bool IsAttribute, string Name)> mapping)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                AddJsonObjectElementToXmlDocument(doc, parent, element, mapping);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var child = doc.CreateElement(elementName);
                    parent.AppendChild(child);
                    AddJsonElementToXmlDocument(doc, child, elementName, item, mapping);
                }
                break;
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                XmlConverter.CreateXmlInnerText(parent, element);
                break;
            case JsonValueKind.Null:
                break;
        }
    }

    private static void AddJsonObjectElementToXmlDocument(XmlDocument doc, XmlElement parent, JsonElement element,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        var (allNamedNs, allNs, props) = XmlPreloadJsonElement(element, mapping);
        foreach (var (name, value) in allNs)
            XmlConverter.CreateXmlAttribute(parent, name, value, null);
        foreach (var (isAttr, name, validXmlName, nsUsed, _, _, value) in props)
        {
            if (isAttr)
                XmlConverter.CreateXmlAttribute(parent, name, value, nsUsed);
            else if (value.ValueKind == JsonValueKind.Array)
                AddJsonElementToXmlDocument(doc, parent, validXmlName, value, mapping);
            else
            {
                var child = nsUsed != null ? doc.CreateElement(validXmlName, nsUsed) : doc.CreateElement(validXmlName);
                parent.AppendChild(child);
                AddJsonElementToXmlDocument(doc, child, validXmlName, value, mapping);
            }
        }
    }

    private static void AddJsonNodeToXml(XElement parent, JsonNode? node, string elementName,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        if (node == null) return;
#if NET8_0_OR_GREATER
        var valueKind = node.GetValueKind();
#else
        var valueKind = node.GetValue<JsonElement>().ValueKind;
#endif
        switch (valueKind)
        {
            case JsonValueKind.Object:
                AddJsonObjectToXml(parent, node, mapping);
                break;
            case JsonValueKind.Array:
                foreach (var item in node.AsArray())
                {
                    var child = new XElement(elementName);
                    parent.Add(child);
                    AddJsonNodeToXml(child, item, elementName, mapping);
                }
                break;
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                XmlConverter.CreateXInnerText(parent, node);
                break;
            case JsonValueKind.Null:
                break;
        }
    }

    private static void AddJsonObjectToXml(XElement parent, JsonNode node,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        var (allNamedNs, allNs, props) = XmlPreloadJsonNode(node, mapping);
        foreach (var (name, value) in allNs)
            XmlConverter.CreateXAttribute(parent, name, value, null);
        foreach (var (isAttr, name, validXmlName, nsUsed, nsPre, nsPost, value) in props)
        {
            if (isAttr)
                XmlConverter.CreateXAttribute(parent, name, value, nsUsed);
            else if (value?.GetValueKind() == JsonValueKind.Array)
                AddJsonNodeToXml(parent, value, validXmlName, mapping);
            else
            {
                XName xname = nsUsed == null ? XName.Get(name) : XName.Get(nsPost!, nsUsed);
                var child = new XElement(xname);
                parent.Add(child);
                AddJsonNodeToXml(child, value, validXmlName, mapping);
            }
        }
    }

    private static void AddJsonElementToXml(XElement parent, JsonElement element, string elementName,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                AddJsonObjectElementToXml(parent, element, mapping);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var child = new XElement(elementName);
                    parent.Add(child);
                    AddJsonElementToXml(child, item, elementName, mapping);
                }
                break;
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                XmlConverter.CreateXInnerText(parent, element);
                break;
            case JsonValueKind.Null:
                break;
        }
    }

    private static void AddJsonObjectElementToXml(XElement parent, JsonElement element,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        var (allNamedNs, allNs, props) = XmlPreloadJsonElement(element, mapping);
        foreach (var (name, value) in allNs)
            XmlConverter.CreateXAttribute(parent, name, value, null);
        foreach (var (isAttr, name, validXmlName, nsUsed, nsPre, nsPost, value) in props)
        {
            if (isAttr)
                XmlConverter.CreateXAttribute(parent, name, value, nsUsed);
            else if (value.ValueKind == JsonValueKind.Array)
                AddJsonElementToXml(parent, value, validXmlName, mapping);
            else
            {
                XName xname = nsUsed == null ? XName.Get(name) : XName.Get(nsPost!, nsUsed);
                var child = new XElement(xname);
                parent.Add(child);
                AddJsonElementToXml(child, value, validXmlName, mapping);
            }
        }
    }

    // Preload helpers for namespace/attribute handling
    private static (Dictionary<string, JsonNode?> AllNamedNs, List<(string name, JsonNode? value)> AllNs,
        List<(bool isAttribute, string name, string validXmlName, string? nsUsed, string? nsPreName, string? nsPostName, JsonNode? value)> AllProps)
        XmlPreloadJsonNode(JsonNode node, Func<string, (bool IsAttribute, string Name)> mapping)
    {
        var allNamedNs = new Dictionary<string, JsonNode?>();
        var allNs = new List<(string, JsonNode?)>();
        var preProps = new List<(bool, string, JsonNode?)>();
        var props = new List<(bool, string, string, string?, string?, string?, JsonNode?)>();
        foreach (var prop in node.AsObject())
        {
            var (isAttr, name) = mapping(prop.Key);
            if (isAttr && name.StartsWith("xmlns"))
                XmlConverter.ProcessNamespaceAttribute(name, prop.Value, allNamedNs, allNs);
            else
                preProps.Add((isAttr, name, prop.Value));
        }
        foreach (var (isAttr, name, value) in preProps)
            XmlConverter.ProcessPropertyWithNamespace(name, value, isAttr, allNamedNs, props);
        return (allNamedNs, allNs, props);
    }

    private static (Dictionary<string, JsonElement> AllNamedNs, List<(string name, JsonElement value)> AllNs,
        List<(bool isAttribute, string name, string validXmlName, string? nsUsed, string? nsPreName, string? nsPostName, JsonElement value)> AllProps)
        XmlPreloadJsonElement(JsonElement element, Func<string, (bool IsAttribute, string Name)> mapping)
    {
        var allNamedNs = new Dictionary<string, JsonElement>();
        var allNs = new List<(string, JsonElement)>();
        var preProps = new List<(bool, string, JsonElement)>();
        var props = new List<(bool, string, string, string?, string?, string?, JsonElement)>();
        foreach (var prop in element.EnumerateObject())
        {
            var (isAttr, name) = mapping(prop.Name);
            if (isAttr && name.StartsWith("xmlns"))
                XmlConverter.ProcessNamespaceAttribute(name, prop.Value, allNamedNs, allNs);
            else
                preProps.Add((isAttr, name, prop.Value));
        }
        foreach (var (isAttr, name, value) in preProps)
            XmlConverter.ProcessPropertyWithNamespace(name, value, isAttr, allNamedNs, props);
        return (allNamedNs, allNs, props);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a <see cref="JsonValue"/> from various object types with type inference.
    /// </summary>
    internal static JsonValue? CreateJsonValue(object? value)
    {
        if (value == null) return null;
        if (value is YamlScalarNode yamlScalarNode)
            return CreateJsonValueFromString(yamlScalarNode.Value);
        if (value is bool b) return JsonValue.Create(b);
        if (value is int i) return JsonValue.Create(i);
        if (value is long l) return JsonValue.Create(l);
        if (value is double d) return JsonValue.Create(d);
        if (value is decimal m) return JsonValue.Create(m);
        if (value is DateTime dt) return JsonValue.Create(dt);
        if (value is DateTimeOffset dto) return JsonValue.Create(dto);
        return CreateJsonValueFromString(value.ToString());
    }

    private static JsonValue? CreateJsonValueFromString(string? valueStr)
    {
        if (valueStr == null) return null;
        if (bool.TryParse(valueStr, out var b)) return JsonValue.Create(b);
        if (byte.TryParse(valueStr, out var bt)) return JsonValue.Create(bt);
        if (int.TryParse(valueStr, out var i)) return JsonValue.Create(i);
        if (long.TryParse(valueStr, out var l)) return JsonValue.Create(l);
        if (double.TryParse(valueStr, out var d)) return JsonValue.Create(d);
        if (decimal.TryParse(valueStr, out var m)) return JsonValue.Create(m);
        if (DateTime.TryParse(valueStr, out var dt)) return JsonValue.Create(dt);
        if (DateTimeOffset.TryParse(valueStr, out var dto)) return JsonValue.Create(dto);
        return JsonValue.Create(valueStr);
    }

    #endregion
}

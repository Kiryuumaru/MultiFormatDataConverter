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
    /// Converts a JSON string to a YAML string asynchronously.
    /// </summary>
    /// <param name="jsonString">The JSON string to convert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the YAML string representation of the input JSON string.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonString"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if parsing the JSON string as a node fails or if the conversion to YAML string fails.
    /// </exception>
    public static async Task<string> ToYaml(string jsonString, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonString);

        using var streamJson = new MemoryStream();
        using var writerJson = new StreamWriter(streamJson);

#if NETSTANDARD
        await writerJson.WriteAsync(jsonString);
        await writerJson.FlushAsync();
#else
        await writerJson.WriteAsync(jsonString.AsMemory(), cancellationToken);
#if NET8_0_OR_GREATER
        await writerJson.FlushAsync(cancellationToken);
#else
        await writerJson.FlushAsync();
#endif
#endif

        streamJson.Seek(0, SeekOrigin.Begin);

        var jsonNode = await JsonNode.ParseAsync(streamJson, cancellationToken: cancellationToken) ?? 
            throw new InvalidOperationException("Failed to parse JSON string as node.");
        var yamlStream = ToYamlStream(jsonNode);

        using var streamYaml = new MemoryStream();
        using var writerYaml = new StreamWriter(streamYaml);
        using var readerYaml = new StreamReader(streamYaml);
        yamlStream.Save(writerYaml, false);

#if NET8_0_OR_GREATER
        await writerYaml.FlushAsync(cancellationToken);
#else
        await writerYaml.FlushAsync();
#endif

#if NET7_0_OR_GREATER
        var yamlString = await readerYaml.ReadToEndAsync(cancellationToken);
#else
        var yamlString = await readerYaml.ReadToEndAsync();
#endif

        return yamlString;
    }

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
                _ => YamlConverter.CreateYamlScalarNode(node)
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
                _ => YamlConverter.CreateYamlScalarNode(element)
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
    /// Converts a JSON string to an XML string asynchronously.
    /// </summary>
    /// <param name="jsonString">The JSON string to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">
    /// Optional function to determine if a property should be an attribute and its XML name. Defaults to treating properties starting with '$' as attributes.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the XML string representation of the input JSON string.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonString"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if parsing the JSON string as a node fails or if the conversion to XML string fails.
    /// </exception>
    public static async Task<string> ToXml(string jsonString, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonString);

        using var streamJson = new MemoryStream();
        using var writerJson = new StreamWriter(streamJson);

#if NETSTANDARD
        await writerJson.WriteAsync(jsonString);
        await writerJson.FlushAsync();
#else
        await writerJson.WriteAsync(jsonString.AsMemory(), cancellationToken);
#if NET8_0_OR_GREATER
        await writerJson.FlushAsync(cancellationToken);
#else
        await writerJson.FlushAsync();
#endif
#endif

        streamJson.Seek(0, SeekOrigin.Begin);

        var jsonNode = await JsonNode.ParseAsync(streamJson, cancellationToken: cancellationToken) ??
            throw new InvalidOperationException("Failed to parse JSON string as node.");
        var xmlDocument = ToXmlDocument(jsonNode, rootElementName);

        using var streamXml = new MemoryStream();
        using var writerXml = new StreamWriter(streamXml);
        using var readerXml = new StreamReader(streamXml);
        xmlDocument.Save(writerXml);

#if NET8_0_OR_GREATER
        await writerXml.FlushAsync(cancellationToken);
#else
        await writerXml.FlushAsync();
#endif

#if NET7_0_OR_GREATER
        var xmlString = await readerXml.ReadToEndAsync(cancellationToken);
#else
        var xmlString = await readerXml.ReadToEndAsync();
#endif

        return xmlString;
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="jsonNode">The <see cref="JsonNode"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">
    /// Optional function to determine if a property should be an attribute and its XML name. Defaults to treating properties starting with '$' as attributes.
    /// </param>
    /// <returns>An <see cref="XmlDocument"/> representing the converted JSON node.</returns>
    public static XmlDocument ToXmlDocument(this JsonNode jsonNode, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(jsonNode);

        var document = new XmlDocument();
        document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
        var root = document.CreateElement(rootElementName);
        document.AppendChild(root);

        AddJsonNodeToXmlDocument(document, root, rootElementName, jsonNode, xmlMappingStrategy);

        return document;
    }

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="jsonObject">The <see cref="JsonObject"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">
    /// Optional function to determine if a property should be an attribute and its XML name. Defaults to treating properties starting with '$' as attributes.
    /// </param>
    /// <returns>An <see cref="XmlDocument"/> representing the converted JSON object.</returns>
    public static XmlDocument ToXmlDocument(this JsonObject jsonObject, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(jsonObject);

        return ToXmlDocument(jsonObject as JsonNode, rootElementName, xmlMappingStrategy);
    }

    /// <summary>
    /// Converts a <see cref="JsonArray"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="jsonArray">The <see cref="JsonArray"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <param name="xmlMappingStrategy">
    /// Optional function to determine if a property should be an attribute and its XML name. Defaults to treating properties starting with '$' as attributes.
    /// </param>
    /// <returns>An <see cref="XmlDocument"/> representing the converted JSON array.</returns>
    public static XmlDocument ToXmlDocument(this JsonArray jsonArray, string rootElementName = "root", string arrayItemElementName = "item", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
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
            AddJsonNodeToXmlDocument(document, itemElement, arrayItemElementName, item, xmlMappingStrategy);
        }

        return document;
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="jsonDocument">The <see cref="JsonDocument"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">
    /// Optional function to determine if a property should be an attribute and its XML name. Defaults to treating properties starting with '$' as attributes.
    /// </param>
    /// <returns>An <see cref="XmlDocument"/> representing the converted JSON document.</returns>
    public static XmlDocument ToXmlDocument(this JsonDocument jsonDocument, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);

        var document = new XmlDocument();
        document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
        var root = document.CreateElement(rootElementName);
        document.AppendChild(root);

        AddJsonElementToXmlDocument(document, root, rootElementName, jsonDocument.RootElement, xmlMappingStrategy);

        return document;
    }

    private static void AddJsonNodeToXmlDocument(XmlDocument document, XmlElement parent, string elementName, JsonNode? node, Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy)
    {
        if (node == null)
            return;
        
#if NETSTANDARD
        xmlMappingStrategy ??= propertyName => propertyName.StartsWith("$") ? (true, propertyName[1..]) : (false, propertyName);
#else
        xmlMappingStrategy ??= propertyName => propertyName.StartsWith('$') ? (true, propertyName[1..]) : (false, propertyName);
#endif

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
                    var (isAttribute, name) = xmlMappingStrategy(property.Key);

                    if (isAttribute)
                    {
                        XmlConverter.CreateXmlAttribute(parent, name, property.Value);
                    }
                    else
                    {
                        var validXmlName = XmlConverter.MakeValidXmlName(name);
                        if (property.Value?.GetValueKind() == JsonValueKind.Array)
                        {
                            AddJsonNodeToXmlDocument(document, parent, validXmlName, property.Value, xmlMappingStrategy);
                        }
                        else
                        {
                            var childElement = document.CreateElement(validXmlName);
                            parent.AppendChild(childElement);
                            AddJsonNodeToXmlDocument(document, childElement, validXmlName, property.Value, xmlMappingStrategy);
                        }
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in node.AsArray())
                {
                    var childElement = document.CreateElement(elementName);
                    parent.AppendChild(childElement);
                    AddJsonNodeToXmlDocument(document, childElement, elementName, item, xmlMappingStrategy);
                }
                break;

            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                XmlConverter.CreateXmlInnerText(parent, node);
                break;

            case JsonValueKind.Null:
                // For null values, we leave the element empty
                break;
        }
    }

    private static void AddJsonElementToXmlDocument(XmlDocument document, XmlElement parent, string elementName, JsonElement element, Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
#if NETSTANDARD
        xmlMappingStrategy ??= propertyName => propertyName.StartsWith("$") ? (true, propertyName[1..]) : (false, propertyName);
#else
        xmlMappingStrategy ??= propertyName => propertyName.StartsWith('$') ? (true, propertyName[1..]) : (false, propertyName);
#endif

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var (isAttribute, name) = xmlMappingStrategy(property.Name);

                    if (isAttribute)
                    {
                        XmlConverter.CreateXmlAttribute(parent, name, property.Value);
                    }
                    else
                    {
                        var validXmlName = XmlConverter.MakeValidXmlName(property.Name);
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            AddJsonElementToXmlDocument(document, parent, validXmlName, property.Value);
                        }
                        else
                        {
                            var childElement = document.CreateElement(validXmlName);
                            parent.AppendChild(childElement);
                            AddJsonElementToXmlDocument(document, childElement, validXmlName, property.Value);
                        }
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var childElement = document.CreateElement(elementName);
                    parent.AppendChild(childElement);
                    AddJsonElementToXmlDocument(document, childElement, elementName, item);
                }
                break;

            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                XmlConverter.CreateXmlInnerText(parent, element);
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
    /// <param name="xmlMappingStrategy">
    /// Optional function to determine if a property should be an attribute and its XML name. Defaults to treating properties starting with '$' as attributes.
    /// </param>
    /// <returns>An <see cref="XDocument"/> representing the converted JSON node.</returns>
    public static XDocument ToXDocument(this JsonNode jsonNode, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var root = new XElement(rootElementName);
        document.Add(root);

        AddJsonNodeToXml(root, jsonNode, rootElementName, xmlMappingStrategy);

        return document;
    }

    /// <summary>
    /// Converts a <see cref="JsonObject"/> to an <see cref="XDocument"/>.
    /// </summary>
    /// <param name="jsonObject">The <see cref="JsonObject"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">
    /// Optional function to determine if a property should be an attribute and its XML name. Defaults to treating properties starting with '$' as attributes.
    /// </param>
    /// <returns>An <see cref="XDocument"/> representing the converted JSON object.</returns>
    public static XDocument ToXDocument(this JsonObject jsonObject, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        return ToXDocument(jsonObject as JsonNode, rootElementName, xmlMappingStrategy);
    }

    /// <summary>
    /// Converts a <see cref="JsonArray"/> to an <see cref="XDocument"/>.
    /// </summary>
    /// <param name="jsonArray">The <see cref="JsonArray"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="arrayItemElementName">The name of individual array item elements. Defaults to "item".</param>
    /// <param name="xmlMappingStrategy">
    /// Optional function to determine if a property should be an attribute and its XML name. Defaults to treating properties starting with '$' as attributes.
    /// </param>
    /// <returns>An <see cref="XDocument"/> representing the converted JSON array.</returns>
    public static XDocument ToXDocument(this JsonArray jsonArray, string rootElementName = "root", string arrayItemElementName = "item", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var root = new XElement(rootElementName);
        document.Add(root);

        foreach (var item in jsonArray)
        {
            var itemElement = new XElement(arrayItemElementName);
            root.Add(itemElement);
            AddJsonNodeToXml(itemElement, item, arrayItemElementName, xmlMappingStrategy);
        }

        return document;
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to an <see cref="XDocument"/>.
    /// </summary>
    /// <param name="jsonDocument">The <see cref="JsonDocument"/> to convert.</param>
    /// <param name="rootElementName">The name of the root XML element. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">
    /// Optional function to determine if a property should be an attribute and its XML name. Defaults to treating properties starting with '$' as attributes.
    /// </param>
    /// <returns>An <see cref="XDocument"/> representing the converted JSON document.</returns>
    public static XDocument ToXDocument(this JsonDocument jsonDocument, string rootElementName = "root", Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var root = new XElement(rootElementName);
        document.Add(root);

        AddJsonElementToXml(root, jsonDocument.RootElement, rootElementName, xmlMappingStrategy);

        return document;
    }

    // Helper methods
    private static void AddJsonNodeToXml(XElement parent, JsonNode? node, string elementName, Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy)
    {
        if (node == null)
            return;

#if NETSTANDARD
        xmlMappingStrategy ??= propertyName => propertyName.StartsWith("$") ? (true, propertyName[1..]) : (false, propertyName);
#else
        xmlMappingStrategy ??= propertyName => propertyName.StartsWith('$') ? (true, propertyName[1..]) : (false, propertyName);
#endif

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
                    var (isAttribute, name) = xmlMappingStrategy(property.Key);

                    if (isAttribute)
                    {
                        XmlConverter.CreateXAttribute(parent, name, property.Value);
                    }
                    else
                    {
                        var validXmlName = XmlConverter.MakeValidXmlName(name);
                        if (property.Value?.GetValueKind() == JsonValueKind.Array)
                        {
                            AddJsonNodeToXml(parent, property.Value, validXmlName, xmlMappingStrategy);
                        }
                        else
                        {
                            var childElement = new XElement(validXmlName);
                            parent.Add(childElement);
                            AddJsonNodeToXml(childElement, property.Value, validXmlName, xmlMappingStrategy);
                        }
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in node.AsArray())
                {
                    var childElement = new XElement(elementName);
                    parent.Add(childElement);
                    AddJsonNodeToXml(childElement, item, elementName, xmlMappingStrategy);
                }
                break;

            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                XmlConverter.CreateXInnerText(parent, node);
                break;

            case JsonValueKind.Null:
                // For null values, we leave the element empty
                break;
        }
    }

    private static void AddJsonElementToXml(XElement parent, JsonElement element, string elementName, Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy)
    {
#if NETSTANDARD
        xmlMappingStrategy ??= propertyName => propertyName.StartsWith("$") ? (true, propertyName[1..]) : (false, propertyName);
#else
        xmlMappingStrategy ??= propertyName => propertyName.StartsWith('$') ? (true, propertyName[1..]) : (false, propertyName);
#endif

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var validXmlName = XmlConverter.MakeValidXmlName(property.Name);
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        AddJsonElementToXml(parent, property.Value, validXmlName, xmlMappingStrategy);
                    }
                    else
                    {
                        var childElement = new XElement(validXmlName);
                        parent.Add(childElement);
                        AddJsonElementToXml(childElement, property.Value, validXmlName, xmlMappingStrategy);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var childElement = new XElement(elementName);
                    parent.Add(childElement);
                    AddJsonElementToXml(childElement, item, elementName, xmlMappingStrategy);
                }
                break;

            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                XmlConverter.CreateXInnerText(parent, element);
                break;

            case JsonValueKind.Null:
                // For null values, we leave the element empty
                break;
        }
    }

#endregion

    internal static JsonValue? CreateJsonValue(object? value)
    {
        static JsonValue? CreateFromString(string? valueStr)
        {
            if (bool.TryParse(valueStr, out var boolVal))
                return JsonValue.Create(boolVal);
            if (byte.TryParse(valueStr, out var byteVal))
                return JsonValue.Create(byteVal);
            if (int.TryParse(valueStr, out var intVal))
                return JsonValue.Create(intVal);
            if (long.TryParse(valueStr, out var longVal))
                return JsonValue.Create(longVal);
            if (double.TryParse(valueStr, out var doubleVal))
                return JsonValue.Create(doubleVal);
            if (decimal.TryParse(valueStr, out var decimalVal))
                return JsonValue.Create(decimalVal);
            if (DateTime.TryParse(valueStr, out var dateTimeVal))
                return JsonValue.Create(dateTimeVal);
            if (DateTimeOffset.TryParse(valueStr, out var dateTimeOffsetVal))
                return JsonValue.Create(dateTimeOffsetVal);
            return JsonValue.Create(valueStr);
        }

        if (value is YamlScalarNode yamlScalarNode)
            return CreateFromString(yamlScalarNode.Value);

        if (value is bool boolVal)
            return JsonValue.Create(boolVal);
        else if (value is int intVal)
            return JsonValue.Create(intVal);
        else if (value is long longVal)
            return JsonValue.Create(longVal);
        else if (value is double doubleVal)
            return JsonValue.Create(doubleVal);
        else if (value is decimal decimalVal)
            return JsonValue.Create(decimalVal);
        else if (value is DateTime dateTimeVal)
            return JsonValue.Create(dateTimeVal);
        else if (value is DateTimeOffset dateTimeOffsetVal)
            return JsonValue.Create(dateTimeOffsetVal);

        return CreateFromString(value?.ToString());
    }
}
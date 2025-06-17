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
/// Provides extension methods for converting <see cref="YamlStream"/> to JSON, XML, and LINQ to XML formats.
/// </summary>
public static class YamlConverter
{
    #region ToJson

    /// <summary>
    /// Converts a <see cref="YamlStream"/> to an array of <see cref="JsonNode"/> (one per YAML document).
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>An array of <see cref="JsonNode"/> representing each YAML document.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    public static JsonNode?[] ToJsonNodeArray(this YamlStream yamlStream)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);

        static JsonNode? ConvertYamlNode(YamlNode? node)
        {
            if (node == null) return null;
            return node switch
            {
                YamlScalarNode scalar => JsonConverter.CreateJsonValue(scalar),
                YamlSequenceNode seq => new JsonArray([.. seq.Children.Select(ConvertYamlNode)]),
                YamlMappingNode map => new JsonObject(map.Children
                    .Where(e => e.Key is YamlScalarNode)
                    .ToDictionary(
                        e => ((YamlScalarNode)e.Key).Value ?? string.Empty,
                        e => ConvertYamlNode(e.Value))),
                _ => null
            };
        }

        return [.. yamlStream.Documents.Select(doc => ConvertYamlNode(doc.RootNode))];
    }

    /// <summary>
    /// Converts a <see cref="YamlStream"/> to an array of <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>An array of <see cref="JsonObject"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    public static JsonObject?[] ToJsonObjectArray(this YamlStream yamlStream)
        => [.. yamlStream.ToJsonNodeArray().Select(n => n as JsonObject)];

    /// <summary>
    /// Converts a <see cref="YamlStream"/> to an array of <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>An array of <see cref="JsonDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    public static JsonDocument?[] ToJsonDocumentArray(this YamlStream yamlStream)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);
        var jsonNodes = yamlStream.ToJsonNodeArray();
        var result = new JsonDocument?[jsonNodes.Length];
        for (int i = 0; i < jsonNodes.Length; i++)
        {
            if (jsonNodes[i] is JsonNode node)
            {
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                node.WriteTo(writer, new JsonSerializerOptions { WriteIndented = true });
                writer.Flush();
                stream.Position = 0;
                result[i] = JsonDocument.Parse(stream);
            }
        }
        return result;
    }

    /// <summary>
    /// Converts a single-document <see cref="YamlStream"/> to a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>The <see cref="JsonObject"/> for the first document, or an empty object if none.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the stream contains multiple documents.</exception>
    public static JsonObject? ToJsonObject(this YamlStream yamlStream)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);
        if (yamlStream.Documents.Count > 1)
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToJsonObjectArray)}.");
        var arr = yamlStream.ToJsonObjectArray();
        return arr.Length > 0 ? arr[0] : [];
    }

    /// <summary>
    /// Converts a single-document <see cref="YamlStream"/> to a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <returns>The <see cref="JsonDocument"/> for the first document, or an empty document if none.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the stream contains multiple documents.</exception>
    public static JsonDocument? ToJsonDocument(this YamlStream yamlStream)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);
        if (yamlStream.Documents.Count > 1)
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToJsonDocumentArray)}.");
        var arr = yamlStream.ToJsonDocumentArray();
        return arr.Length > 0 ? arr[0] : JsonDocument.Parse("{}");
    }

    #endregion

    #region ToXml

    /// <summary>
    /// Converts a <see cref="YamlStream"/> to an XML string asynchronously.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">Root XML element name. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional mapping for attributes and element names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>XML string representation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    public static async Task<string> ToXmlString(
        this YamlStream yamlStream,
        string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);
        var xmlDoc = yamlStream.ToXmlDocument(rootElementName, xmlMappingStrategy);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        xmlDoc.Save(writer);
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
    /// Converts a <see cref="YamlStream"/> to an array of <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">Root XML element name. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional mapping for attributes and element names.</param>
    /// <returns>Array of <see cref="XmlDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    public static XmlDocument[] ToXmlDocumentArray(
        this YamlStream yamlStream,
        string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);
        var mapping = XmlConverter.GetMappingStrategy(xmlMappingStrategy);
        return [.. yamlStream.Documents.Select(doc =>
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            var root = xmlDoc.CreateElement(rootElementName);
            xmlDoc.AppendChild(root);
            ConvertYamlNodeToXml(xmlDoc, root, doc.RootNode, rootElementName, mapping);
            return xmlDoc;
        })];
    }

    /// <summary>
    /// Converts a single-document <see cref="YamlStream"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">Root XML element name. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional mapping for attributes and element names.</param>
    /// <returns>An <see cref="XmlDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the stream contains multiple documents.</exception>
    public static XmlDocument ToXmlDocument(
        this YamlStream yamlStream,
        string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);
        if (yamlStream.Documents.Count > 1)
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToXmlDocumentArray)}.");
        if (yamlStream.Documents.Count == 0)
        {
            var emptyDoc = new XmlDocument();
            emptyDoc.AppendChild(emptyDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            emptyDoc.AppendChild(emptyDoc.CreateElement(rootElementName));
            return emptyDoc;
        }
        return yamlStream.ToXmlDocumentArray(rootElementName, xmlMappingStrategy)[0];
    }

    /// <summary>
    /// Converts a <see cref="YamlStream"/> to an array of <see cref="XDocument"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">Root XML element name. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional mapping for attributes and element names.</param>
    /// <returns>Array of <see cref="XDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    public static XDocument[] ToXDocumentArray(
        this YamlStream yamlStream,
        string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);
        var mapping = XmlConverter.GetMappingStrategy(xmlMappingStrategy);
        return [.. yamlStream.Documents.Select(doc =>
        {
            var xDoc = new XDocument(new XDeclaration("1.0", "utf-8", null));
            var root = new XElement(rootElementName);
            xDoc.Add(root);
            ConvertYamlNodeToXElement(doc.RootNode, root, rootElementName, mapping);
            return xDoc;
        })];
    }

    /// <summary>
    /// Converts a single-document <see cref="YamlStream"/> to an <see cref="XDocument"/>.
    /// </summary>
    /// <param name="yamlStream">The YAML stream to convert.</param>
    /// <param name="rootElementName">Root XML element name. Defaults to "root".</param>
    /// <param name="xmlMappingStrategy">Optional mapping for attributes and element names.</param>
    /// <returns>An <see cref="XDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlStream"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the stream contains multiple documents.</exception>
    public static XDocument ToXDocument(
        this YamlStream yamlStream,
        string rootElementName = "root",
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(yamlStream);
        if (yamlStream.Documents.Count > 1)
            throw new InvalidOperationException($"YamlStream contains multiple documents. Use {nameof(ToXDocumentArray)}.");
        if (yamlStream.Documents.Count == 0)
            return new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(rootElementName));
        return yamlStream.ToXDocumentArray(rootElementName, xmlMappingStrategy)[0];
    }

    // --- Internal helpers for YAML to XML/XDocument conversion ---

    private static void ConvertYamlNodeToXml(
        XmlDocument xmlDoc,
        XmlElement parent,
        YamlNode? node,
        string elementName,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        if (node == null) return;
        switch (node)
        {
            case YamlScalarNode scalar:
                XmlConverter.CreateXmlInnerText(parent, scalar.Value);
                break;
            case YamlSequenceNode seq:
                foreach (var item in seq.Children)
                {
                    var itemElem = xmlDoc.CreateElement(elementName);
                    parent.AppendChild(itemElem);
                    ConvertYamlNodeToXml(xmlDoc, itemElem, item, elementName, mapping);
                }
                break;
            case YamlMappingNode map:
                ProcessYamlMappingForXml(xmlDoc, parent, map, mapping);
                break;
        }
    }

    private static void ProcessYamlMappingForXml(
        XmlDocument xmlDoc,
        XmlElement parent,
        YamlMappingNode map,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        var allNamedNs = new Dictionary<string, YamlNode>();
        var allNs = new List<(string name, YamlNode value)>();
        var preProps = new List<(bool isAttribute, string name, YamlNode value)>();

        foreach (var entry in map.Children)
        {
            if (entry.Key is YamlScalarNode key && !string.IsNullOrEmpty(key.Value))
            {
                var (isAttr, name) = mapping(key.Value!);
                if (isAttr && name.StartsWith("xmlns") && entry.Value is YamlScalarNode val)
                    XmlConverter.ProcessNamespaceAttribute(name, val, allNamedNs, allNs);
                else
                    preProps.Add((isAttr, name, entry.Value));
            }
        }

        foreach (var (name, value) in allNs)
            if (value is YamlScalarNode val)
                XmlConverter.CreateXmlAttribute(parent, name, val.Value, null);

        foreach (var (isAttr, name, value) in preProps)
        {
            var validXmlName = XmlConverter.MakeValidXmlName(name);
            string? nsUsed = null, nsPre = null, nsPost = null;
            if (validXmlName.Contains(':'))
            {
                var split = validXmlName.Split(':');
                if (split.Length == 2)
                {
                    nsPre = split[0];
                    nsPost = split[1];
                    if (allNamedNs.TryGetValue(nsPre, out var nsVal) && nsVal is YamlScalarNode nsScalar)
                        nsUsed = nsScalar.Value;
                }
            }
            if (isAttr)
            {
                if (value is YamlScalarNode scalar)
                    XmlConverter.CreateXmlAttribute(parent, validXmlName, scalar.Value, nsUsed);
            }
            else if (value is YamlSequenceNode seq)
            {
                foreach (var item in seq.Children)
                {
                    var itemElem = xmlDoc.CreateElement(validXmlName);
                    parent.AppendChild(itemElem);
                    ConvertYamlNodeToXml(xmlDoc, itemElem, item, validXmlName, mapping);
                }
            }
            else
            {
                XmlElement child;
                if (nsUsed != null && nsPost != null)
                    child = xmlDoc.CreateElement(nsPre, nsPost, nsUsed);
                else
                    child = xmlDoc.CreateElement(validXmlName);
                parent.AppendChild(child);
                ConvertYamlNodeToXml(xmlDoc, child, value, validXmlName, mapping);
            }
        }
    }

    private static void ConvertYamlNodeToXElement(
        YamlNode? node,
        XElement parent,
        string elementName,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        if (node == null) return;
        switch (node)
        {
            case YamlScalarNode scalar:
                XmlConverter.CreateXInnerText(parent, scalar.Value);
                break;
            case YamlSequenceNode seq:
                foreach (var item in seq.Children)
                {
                    var itemElem = new XElement(elementName);
                    parent.Add(itemElem);
                    ConvertYamlNodeToXElement(item, itemElem, elementName, mapping);
                }
                break;
            case YamlMappingNode map:
                ProcessYamlMappingForXElement(parent, map, mapping);
                break;
        }
    }

    private static void ProcessYamlMappingForXElement(
        XElement parent,
        YamlMappingNode map,
        Func<string, (bool IsAttribute, string Name)> mapping)
    {
        var allNamedNs = new Dictionary<string, YamlNode>();
        var allNs = new List<(string name, YamlNode value)>();
        var preProps = new List<(bool isAttribute, string name, YamlNode value)>();

        foreach (var entry in map.Children)
        {
            if (entry.Key is YamlScalarNode key && !string.IsNullOrEmpty(key.Value))
            {
                var (isAttr, name) = mapping(key.Value!);
                if (isAttr && name.StartsWith("xmlns") && entry.Value is YamlScalarNode val)
                    XmlConverter.ProcessNamespaceAttribute(name, val, allNamedNs, allNs);
                else
                    preProps.Add((isAttr, name, entry.Value));
            }
        }

        foreach (var (name, value) in allNs)
            if (value is YamlScalarNode val)
                XmlConverter.CreateXAttribute(parent, name, val.Value, null);

        foreach (var (isAttr, name, value) in preProps)
        {
            var validXmlName = XmlConverter.MakeValidXmlName(name);
            string? nsUsed = null, nsPre = null, nsPost = null;
            if (validXmlName.Contains(':'))
            {
                var split = validXmlName.Split(':');
                if (split.Length == 2)
                {
                    nsPre = split[0];
                    nsPost = split[1];
                    if (allNamedNs.TryGetValue(nsPre, out var nsVal) && nsVal is YamlScalarNode nsScalar)
                        nsUsed = nsScalar.Value;
                }
            }
            if (isAttr)
            {
                if (value is YamlScalarNode scalar)
                    XmlConverter.CreateXAttribute(parent, validXmlName, scalar.Value, nsUsed);
            }
            else if (value is YamlSequenceNode seq)
            {
                foreach (var item in seq.Children)
                {
                    var itemElem = new XElement(validXmlName);
                    parent.Add(itemElem);
                    ConvertYamlNodeToXElement(item, itemElem, validXmlName, mapping);
                }
            }
            else
            {
                XName xname = (nsUsed != null && nsPost != null)
                    ? XNamespace.Get(nsUsed) + nsPost
                    : XName.Get(validXmlName);
                var child = new XElement(xname);
                parent.Add(child);
                ConvertYamlNodeToXElement(value, child, validXmlName, mapping);
            }
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a <see cref="YamlScalarNode"/> from a .NET object, inferring the type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A <see cref="YamlScalarNode"/>.</returns>
    internal static YamlScalarNode CreateYamlScalarNode(object? value)
    {
        if (value is JsonValue jsonValue)
        {
#if NET8_0_OR_GREATER
            if (jsonValue.TryGetValue(out bool bJv)) return new YamlScalarNode(bJv.ToString());
            if (jsonValue.TryGetValue(out byte btJv)) return new YamlScalarNode(btJv.ToString());
            if (jsonValue.TryGetValue(out int iJv)) return new YamlScalarNode(iJv.ToString());
            if (jsonValue.TryGetValue(out long lJv)) return new YamlScalarNode(lJv.ToString());
            if (jsonValue.TryGetValue(out double dJv)) return new YamlScalarNode(dJv.ToString());
            if (jsonValue.TryGetValue(out decimal mJv)) return new YamlScalarNode(mJv.ToString());
#endif
            return new YamlScalarNode(jsonValue.ToString());
        }
        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.True => new YamlScalarNode("true"),
                JsonValueKind.False => new YamlScalarNode("false"),
                JsonValueKind.Number => je.TryGetInt64(out var lJe) ? new YamlScalarNode(lJe.ToString())
                    : je.TryGetDouble(out var dJe) ? new YamlScalarNode(dJe.ToString())
                    : je.TryGetDecimal(out var mJe) ? new YamlScalarNode(mJe.ToString())
                    : new YamlScalarNode(je.ToString()),
                JsonValueKind.String => new YamlScalarNode(je.GetString()),
                JsonValueKind.Null => new YamlScalarNode(),
                _ => new YamlScalarNode(je.ToString())
            };
        }
        if (value is string s) return new YamlScalarNode(s);
        if (value is bool b) return new YamlScalarNode(b.ToString());
        if (value is int i) return new YamlScalarNode(i.ToString());
        if (value is long l) return new YamlScalarNode(l.ToString());
        if (value is double d) return new YamlScalarNode(d.ToString());
        if (value is decimal m) return new YamlScalarNode(m.ToString());
        if (value is DateTime dt) return new YamlScalarNode(dt.ToString("o"));
        if (value is DateTimeOffset dto) return new YamlScalarNode(dto.ToString("o"));
        return new YamlScalarNode();
    }

    #endregion
}

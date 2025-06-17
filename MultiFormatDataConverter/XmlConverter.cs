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
/// Extension methods for converting between XML (<see cref="XmlDocument"/>, <see cref="XDocument"/>) and other data formats (JSON, YAML).
/// </summary>
public static class XmlConverter
{
    #region XML <-> XDocument

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to an <see cref="XDocument"/>.
    /// </summary>
    /// <param name="xmlDocument">Source XML document.</param>
    /// <returns>Equivalent <see cref="XDocument"/>.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static XDocument ToXDocument(this XmlDocument xmlDocument)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);
        using var nodeReader = new XmlNodeReader(xmlDocument);
        return XDocument.Load(nodeReader);
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to an <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="xDocument">Source XDocument.</param>
    /// <returns>Equivalent <see cref="XmlDocument"/>.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static XmlDocument ToXmlDocument(this XDocument xDocument)
    {
        ArgumentNullException.ThrowIfNull(xDocument);
        var xmlDocument = new XmlDocument();
        using var xmlReader = xDocument.CreateReader();
        xmlDocument.Load(xmlReader);
        return xmlDocument;
    }

    #endregion

    #region XML <-> JSON

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="xmlDocument">Source XML document.</param>
    /// <param name="attributeNameFactory">Attribute name mapping (default: prefix with '$').</param>
    /// <returns>JSON node representation.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static JsonNode? ToJsonNode(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);
        return xmlDocument.DocumentElement == null
            ? new JsonObject()
            : ConvertElementToJsonNode(xmlDocument.DocumentElement, attributeNameFactory ?? (n => "$" + n));
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="xDocument">Source XDocument.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>JSON node representation.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static JsonNode? ToJsonNode(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
        => xDocument.ToXmlDocument().ToJsonNode(attributeNameFactory);

    /// <summary>
    /// Converts the root's child elements of an <see cref="XmlDocument"/> to a <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="xmlDocument">Source XML document.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>JSON array of child elements.</returns>
    public static JsonArray ToJsonArray(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);
        var children = xmlDocument.DocumentElement?.ChildNodes.OfType<XmlElement>() ?? [];
        var factory = attributeNameFactory ?? (n => "$" + n);
        return new JsonArray([.. children.Select(el => ConvertElementToJsonNode(el, factory))]);
    }

    /// <summary>
    /// Converts the root's child elements of an <see cref="XDocument"/> to a <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="xDocument">Source XDocument.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>JSON array of child elements.</returns>
    public static JsonArray ToJsonArray(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
        => xDocument.ToXmlDocument().ToJsonArray(attributeNameFactory);

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="xmlDocument">Source XML document.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>JSON object representation.</returns>
    public static JsonObject ToJsonObject(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);
        var node = xmlDocument.ToJsonNode(attributeNameFactory);
        return node as JsonObject ?? (node is null ? [] : new JsonObject { [xmlDocument.DocumentElement?.Name ?? "root"] = node });
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="xDocument">Source XDocument.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>JSON object representation.</returns>
    public static JsonObject ToJsonObject(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
        => xDocument.ToXmlDocument().ToJsonObject(attributeNameFactory);

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="xmlDocument">Source XML document.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>JSON document representation.</returns>
    public static JsonDocument ToJsonDocument(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);
        var node = xmlDocument.ToJsonNode(attributeNameFactory);
        return node is null ? JsonDocument.Parse("{}") : JsonDocument.Parse(node.ToJsonString());
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="xDocument">Source XDocument.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>JSON document representation.</returns>
    public static JsonDocument ToJsonDocument(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
        => xDocument.ToXmlDocument().ToJsonDocument(attributeNameFactory);

    private static JsonNode? ConvertElementToJsonNode(XmlElement element, Func<string, string> attributeNameFactory)
    {
        var childGroups = element.ChildNodes.OfType<XmlElement>().GroupBy(e => e.Name)
            .ToDictionary(g => g.Key, g => g.ToList());
        var jsonObject = new JsonObject();

        // Text content
        var textContent = string.Concat(
            element.ChildNodes.OfType<XmlText>().Select(t => t.Value)
            .Concat(element.ChildNodes.OfType<XmlCDataSection>().Select(c => c.Value))
        );
        if (!string.IsNullOrWhiteSpace(textContent))
            jsonObject.Add("#text", JsonConverter.CreateJsonValue(textContent));

        // Attributes
        foreach (XmlAttribute attr in element.Attributes)
            jsonObject.Add(attributeNameFactory(attr.Name), JsonConverter.CreateJsonValue(attr.Value));

        // Child elements
        foreach (var kvp in childGroups)
        {
            var name = kvp.Key;
            var items = kvp.Value;
            jsonObject.Add(name, items.Count == 1
                ? ConvertElementToJsonNode(items[0], attributeNameFactory)
                : new JsonArray([.. items.Select(e => ConvertElementToJsonNode(e, attributeNameFactory))]));
        }

        // If only text, return as value
        return jsonObject.Count == 1 && jsonObject.ContainsKey("#text")
            ? JsonConverter.CreateJsonValue(textContent)
            : jsonObject;
    }

    #endregion

    #region XML <-> YAML

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xmlDocument">Source XML document.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>YAML stream representation.</returns>
    public static YamlStream ToYamlStream(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
        => new[] { xmlDocument }.ToYamlStream(attributeNameFactory);

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xDocument">Source XDocument.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>YAML stream representation.</returns>
    public static YamlStream ToYamlStream(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
        => new[] { xDocument }.ToYamlStream(attributeNameFactory);

    /// <summary>
    /// Converts a collection of <see cref="XmlDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xmlDocuments">Source XML documents.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>YAML stream representation.</returns>
    public static YamlStream ToYamlStream(this IEnumerable<XmlDocument> xmlDocuments, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocuments);
        var yamlStream = new YamlStream();
        foreach (var xmlDocument in xmlDocuments)
        {
            if (xmlDocument == null) continue;
            var jsonNode = xmlDocument.ToJsonNode(attributeNameFactory);
            yamlStream.Documents.Add(new YamlDocument(ConvertJsonNodeToYamlNode(jsonNode)));
        }
        return yamlStream;
    }

    /// <summary>
    /// Converts a collection of <see cref="XDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xDocuments">Source XDocuments.</param>
    /// <param name="attributeNameFactory">Attribute name mapping.</param>
    /// <returns>YAML stream representation.</returns>
    public static YamlStream ToYamlStream(this IEnumerable<XDocument> xDocuments, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xDocuments);
        var yamlStream = new YamlStream();
        foreach (var xDocument in xDocuments)
        {
            if (xDocument == null) continue;
            var jsonNode = xDocument.ToJsonNode(attributeNameFactory);
            yamlStream.Documents.Add(new YamlDocument(ConvertJsonNodeToYamlNode(jsonNode)));
        }
        return yamlStream;
    }

    private static YamlNode ConvertJsonNodeToYamlNode(JsonNode? node)
    {
        if (node is null) return new YamlScalarNode(string.Empty);
        return node switch
        {
            JsonValue value => new YamlScalarNode(value.GetValue<object?>()?.ToString() ?? string.Empty),
            JsonArray array => new YamlSequenceNode(array.Select(ConvertJsonNodeToYamlNode).ToList()),
            JsonObject obj => new YamlMappingNode(obj.Select(kvp =>
                new KeyValuePair<YamlNode, YamlNode>(
                    new YamlScalarNode(kvp.Key), ConvertJsonNodeToYamlNode(kvp.Value)
                )).ToList()),
            _ => new YamlScalarNode(node.ToJsonString())
        };
    }

    #endregion

    #region XML String Output

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a formatted XML string.
    /// </summary>
    /// <param name="xmlDocument">Source XML document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Formatted XML string.</returns>
    public static async Task<string> ToXmlString(this XmlDocument xmlDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);
        using var stream = new MemoryStream();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Indent = true,
            NewLineHandling = NewLineHandling.Replace,
            NewLineChars = "\n",
            Async = true
        });
        xmlDocument.Save(writer);
        await writer.FlushAsync();
        stream.Position = 0;
        using var reader = new StreamReader(stream);
#if NET7_0_OR_GREATER
        return await reader.ReadToEndAsync(cancellationToken);
#else
        return await reader.ReadToEndAsync();
#endif
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a formatted XML string.
    /// </summary>
    /// <param name="xDocument">Source XDocument.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Formatted XML string.</returns>
    public static async Task<string> ToXmlString(this XDocument xDocument, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(xDocument);
        using var stream = new MemoryStream();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Indent = true,
            NewLineHandling = NewLineHandling.Replace,
            NewLineChars = "\n",
            Async = true
        });
#if NETSTANDARD
        xDocument.Save(writer);
#else
        await xDocument.SaveAsync(writer, cancellationToken);
#endif
        await writer.FlushAsync();
        stream.Position = 0;
        using var reader = new StreamReader(stream);
#if NET7_0_OR_GREATER
        return await reader.ReadToEndAsync(cancellationToken);
#else
        return await reader.ReadToEndAsync();
#endif
    }

    #endregion

    #region Helpers

    // Helper methods for XML/JSON/YAML conversion (unchanged, but can be further refactored if needed)
    static void CreateTextString(Action<string> onCreate, string? valueStr)
    {
        if (bool.TryParse(valueStr, out var boolVal))
        {
            onCreate(boolVal.ToString().ToLowerInvariant());
            return;
        }
        if (byte.TryParse(valueStr, out var byteVal))
        {
            onCreate(byteVal.ToString());
            return;
        }
        if (int.TryParse(valueStr, out var intVal))
        {
            onCreate(intVal.ToString());
            return;
        }
        if (long.TryParse(valueStr, out var longVal))
        {
            onCreate(longVal.ToString());
            return;
        }
        if (double.TryParse(valueStr, out var doubleVal))
        {
            onCreate(doubleVal.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        if (decimal.TryParse(valueStr, out var decimalVal))
        {
            onCreate(decimalVal.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        if (DateTime.TryParse(valueStr, out var dateTimeVal))
        {
            onCreate(dateTimeVal.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        if (DateTimeOffset.TryParse(valueStr, out var dateTimeOffsetVal))
        {
            onCreate(dateTimeOffsetVal.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        if (valueStr != null)
        {
            onCreate(valueStr);
        }
    }

    internal static void CreateXmlInnerText(XmlElement parent, object? value)
    {
        if (value is YamlScalarNode yamlScalarNode)
            CreateTextString(s => parent.InnerText = s, yamlScalarNode.Value);
        else if (value is JsonNode jsonNode)
            CreateTextString(s => parent.InnerText = s, jsonNode.ToString());
        else if (value is JsonElement jsonElement)
            CreateTextString(s => parent.InnerText = s, jsonElement.ToString());
        else
            CreateTextString(s => parent.InnerText = s, value?.ToString());
    }

    internal static void CreateXmlAttribute(XmlElement parent, string name, object? value, string? namespaceUri)
    {
        void Create(string valueStr)
        {
            if (namespaceUri != null)
            {
                var xmlAttribute = parent.OwnerDocument.CreateAttribute(name, namespaceUri);
                xmlAttribute.Value = valueStr;
                parent.SetAttributeNode(xmlAttribute);
            }
            else
            {
                parent.SetAttribute(name, valueStr);
            }
        }

        if (value is YamlScalarNode yamlScalarNode)
            CreateTextString(Create, yamlScalarNode.Value);
        else if (value is JsonNode jsonNode)
            CreateTextString(Create, jsonNode.ToString());
        else if (value is JsonElement jsonElement)
            CreateTextString(Create, jsonElement.ToString());
        else
            CreateTextString(Create, value?.ToString());
    }

    internal static void CreateXInnerText(XElement parent, object? value)
    {
        if (value is YamlScalarNode yamlScalarNode)
            CreateTextString(s => parent.Value = s, yamlScalarNode.Value);
        else if (value is JsonNode jsonNode)
            CreateTextString(s => parent.Value = s, jsonNode.ToString());
        else if (value is JsonElement jsonElement)
            CreateTextString(s => parent.Value = s, jsonElement.ToString());
        else
            CreateTextString(s => parent.Value = s, value?.ToString());
    }

    internal static void CreateXAttribute(XElement parent, string name, object? value, string? namespaceUri)
    {
        void Create(string valueStr)
        {
            if (namespaceUri != null)
            {
                XNamespace ns = namespaceUri;
                var nameSplit = name.Split(':');
                parent.Add(new XAttribute(ns + nameSplit[1], valueStr));
            }
            else
            {
                if (name.StartsWith("xmlns"))
                {
                    var nameSplit = name.Split(':');
                    parent.Add(new XAttribute(XNamespace.Xmlns + nameSplit[1], valueStr));
                }
                else
                {
                    parent.Add(new XAttribute(name, valueStr));
                }
            }
        }

        if (value is YamlScalarNode yamlScalarNode)
            CreateTextString(Create, yamlScalarNode.Value);
        else if (value is JsonNode jsonNode)
            CreateTextString(Create, jsonNode.ToString());
        else if (value is JsonElement jsonElement)
            CreateTextString(Create, jsonElement.ToString());
        else
            CreateTextString(Create, value?.ToString());
    }

    internal static string MakeValidXmlName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "unnamed";

        // Remove invalid characters and ensure the name starts with a valid character
        // XML names must start with a letter or underscore, and can contain letters, digits, hyphens, underscores, periods, and colons
        // See: https://www.w3.org/TR/xml/#NT-Name

        // Replace invalid characters with '_'
        var validName = new System.Text.StringBuilder();
        int i = 0;
        foreach (char c in name)
        {
            if ((i == 0 && (char.IsLetter(c) || c == '_')) ||
                (i > 0 && (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' || c == ':')))
            {
                validName.Append(c);
            }
            else
            {
                validName.Append('_');
            }
            i++;
        }

        // If the first character is not valid, prefix with '_'
        if (validName.Length == 0 || !(char.IsLetter(validName[0]) || validName[0] == '_'))
        {
            validName.Insert(0, '_');
        }

        // XML names cannot start with "xml" (case-insensitive)
        if (validName.ToString().StartsWith("xml", StringComparison.OrdinalIgnoreCase))
        {
            validName.Insert(0, '_');
        }

        return validName.ToString();
    }

    internal static void ProcessNamespaceAttribute<T>(string name, T value,
        Dictionary<string, T> allNamedNs, List<(string name, T value)> allNs)
    {
        if (name.StartsWith("xmlns:") && name.Length > 6)
        {
            var nameSplit = name.Split(':');
            if (nameSplit.Length != 2)
            {
                throw new InvalidOperationException($"Invalid namespace declaration: {name}.");
            }

            if (allNamedNs.ContainsKey(nameSplit[1]))
            {
                throw new InvalidOperationException($"Duplicate namespace declaration: {name}");
            }

            allNamedNs.Add(nameSplit[1], value);
        }

        allNs.Add((name, value));
    }

    internal static void ProcessPropertyWithNamespace<T>(string name, T value, bool isAttribute,
        Dictionary<string, T> allNamedNs,
        List<(bool isAttribute, string name, string validXmlName, string? nsUsed, string? nsPreName, string? nsPostName, T value)> props)
    {
        string? nsUsed = null;
        string? nsPreName = null;
        string? nsPostName = null;
        var validXmlName = MakeValidXmlName(name);

        if (validXmlName.Contains(':'))
        {
            var nameSplit = validXmlName.Split(':');
            if (nameSplit.Length != 2)
            {
                throw new InvalidOperationException($"Invalid namespace declaration: {validXmlName}.");
            }

            nsPreName = nameSplit[0];
            nsPostName = nameSplit[1];

            if (allNamedNs.TryGetValue(nsPreName, out var nsValue))
            {
                nsUsed = nsValue?.ToString();
            }
            else
            {
                throw new InvalidOperationException($"Namespace prefix '{nsPreName}' not declared for property '{name}'.");
            }
        }

        props.Add((isAttribute, name, validXmlName, nsUsed, nsPreName, nsPostName, value));
    }

    internal static Func<string, (bool IsAttribute, string Name)> GetMappingStrategy(
        Func<string, (bool IsAttribute, string Name)>? xmlMappingStrategy)
    {
#if NETSTANDARD
        return xmlMappingStrategy ?? (propertyName => propertyName.StartsWith("$") 
            ? (true, propertyName[1..]) 
            : (false, propertyName));
#else
        return xmlMappingStrategy ?? (propertyName => propertyName.StartsWith('$')
            ? (true, propertyName[1..])
            : (false, propertyName));
#endif
    }

    #endregion
}

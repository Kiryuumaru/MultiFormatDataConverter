using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
/// Provides conversion functionality between XML formats and other data formats.
/// </summary>
public static class XmlConverter
{
    #region ToXml

    /// <summary>
    /// Converts an XmlDocument to an XDocument.
    /// </summary>
    /// <param name="xmlDocument">The XmlDocument to convert. Cannot be null.</param>
    /// <returns>An XDocument representation of the input XmlDocument.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the xmlDocument parameter is null.</exception>
    public static XDocument ToXDocument(this XmlDocument xmlDocument)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        using var nodeReader = new XmlNodeReader(xmlDocument);
        return XDocument.Load(nodeReader);
    }

    /// <summary>
    /// Converts an XDocument to an XmlDocument.
    /// </summary>
    /// <param name="xDocument">The XDocument to convert. Cannot be null.</param>
    /// <returns>An XmlDocument representation of the input XDocument.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the xDocument parameter is null.</exception>
    public static XmlDocument ToXmlDocument(this XDocument xDocument)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        var xmlDocument = new XmlDocument();
        using var xmlReader = xDocument.CreateReader();
        xmlDocument.Load(xmlReader);
        return xmlDocument;
    }

    #endregion

    #region ToJson

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="xmlDocument">The <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to JSON property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>A <see cref="JsonNode"/> representation of the input <see cref="XmlDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the xmlDocument parameter is null.</exception>
    public static JsonNode? ToJsonNode(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        if (xmlDocument.DocumentElement == null)
        {
            return new JsonObject();
        }

        return ConvertElementToJsonNode(xmlDocument.DocumentElement, attributeNameFactory);
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="xDocument">The <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to JSON property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>A <see cref="JsonNode"/> representation of the input <see cref="XDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the xDocument parameter is null.</exception>
    public static JsonNode? ToJsonNode(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        return xDocument.ToXmlDocument().ToJsonNode(attributeNameFactory);
    }
    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonNode"/> representing a JSON array.
    /// </summary>
    /// <param name="xDocument">The <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to JSON property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="JsonNode"/> (specifically a <see cref="JsonArray"/>) representing the child elements of the root element.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xDocument"/> parameter is null.</exception>
    public static JsonNode? ToJsonArray(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        return xDocument.ToXmlDocument().ToJsonArray(attributeNameFactory);
    }

    /// <summary>
    /// Converts the child elements of the root element of an <see cref="XmlDocument"/> to a <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="xmlDocument">The <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to JSON property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="JsonArray"/> representing the child elements of the root element. 
    /// If the root element is null or has no child elements, returns an empty <see cref="JsonArray"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xmlDocument"/> parameter is null.</exception>
    public static JsonNode? ToJsonArray(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        if (xmlDocument.DocumentElement == null)
        {
            return new JsonArray();
        }

        // Check if the root element has a collection of identically named child elements
        var childElements = xmlDocument.DocumentElement.ChildNodes
            .OfType<XmlElement>()
            .ToList();

        if (childElements.Count == 0)
        {
            return new JsonArray();
        }

        var jsonArray = new JsonArray();
        foreach (var element in childElements)
        {
            jsonArray.Add(ConvertElementToJsonNode(element, attributeNameFactory));
        }

        return jsonArray;
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="xDocument">The <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to JSON property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="JsonObject"/> representation of the input <see cref="XDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xDocument"/> parameter is null.</exception>
    public static JsonObject? ToJsonObject(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        return xDocument.ToXmlDocument().ToJsonObject(attributeNameFactory);
    }

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="xmlDocument">The <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to JSON property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="JsonObject"/> representation of the input <see cref="XmlDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xmlDocument"/> parameter is null.</exception>
    public static JsonObject? ToJsonObject(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        var jsonNode = xmlDocument.ToJsonNode(attributeNameFactory);
        if (jsonNode is JsonObject jsonObject)
        {
            return jsonObject;
        }
        else if (jsonNode is null)
        {
            return [];
        }
        else
        {
            // If it's not already a JsonObject, wrap it in one
            var result = new JsonObject();
            if (xmlDocument.DocumentElement != null)
            {
                result.Add(xmlDocument.DocumentElement.Name, jsonNode);
            }
            return result;
        }
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="xDocument">The <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to JSON property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="JsonDocument"/> representation of the input <see cref="XDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xDocument"/> parameter is null.</exception>
    public static JsonDocument? ToJsonDocument(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        return xDocument.ToXmlDocument().ToJsonDocument(attributeNameFactory);
    }

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="xmlDocument">The <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to JSON property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="JsonDocument"/> representation of the input <see cref="XmlDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xmlDocument"/> parameter is null.</exception>
    public static JsonDocument? ToJsonDocument(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        var jsonNode = xmlDocument.ToJsonNode(attributeNameFactory);
        if (jsonNode is null)
        {
            return JsonDocument.Parse("{}");
        }

        return JsonDocument.Parse(jsonNode.ToJsonString());
    }

    private static JsonNode? ConvertElementToJsonNode(XmlElement element, Func<string, string>? attributeNameFactory)
    {
        attributeNameFactory ??= name => "$" + name;

        // Process child nodes
        var childElements = element.ChildNodes.OfType<XmlElement>()
            .GroupBy(e => e.Name)
            .ToDictionary(g => g.Key, g => g.ToList());

        bool hasText = false;
        string textContent = string.Empty;

        foreach (XmlNode node in element.ChildNodes)
        {
            if (node is XmlText text)
            {
                hasText = true;
                textContent += text.Value;
            }
            else if (node is XmlCDataSection cdata)
            {
                hasText = true;
                textContent += cdata.Value;
            }
        }

        var jsonObject = new JsonObject();

        // Add text content if present, retaining specific types
        if (hasText && !string.IsNullOrWhiteSpace(textContent))
        {
            jsonObject.Add("#text", JsonConverter.CreateJsonValue(textContent));
        }

        // Add attributes
        foreach (XmlAttribute attr in element.Attributes)
        {
            var attributeName = attributeNameFactory(attr.Name);
            jsonObject.Add(attributeName, JsonConverter.CreateJsonValue(attr.Value));
        }

        // Add child elements
        foreach (var group in childElements)
        {
            string name = group.Key;
            var items = group.Value;

            if (items.Count == 1)
            {
                jsonObject.Add(name, ConvertElementToJsonNode(items[0], attributeNameFactory));
            }
            else
            {
                var array = new JsonArray();
                foreach (var item in items)
                {
                    array.Add(ConvertElementToJsonNode(item, attributeNameFactory));
                }
                jsonObject.Add(name, array);
            }
        }

        // If the object only has text content and no attributes or elements, return just the text (with type retained)
        if (hasText && !string.IsNullOrWhiteSpace(textContent) &&
            jsonObject.Count == 1 && jsonObject.ContainsKey("#text"))
        {
            return JsonConverter.CreateJsonValue(textContent);
        }

        return jsonObject;
    }

    #endregion

    #region ToYaml

    /// <summary>
    /// Converts an array of <see cref="XmlDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xmlDocuments">The array of <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to YAML property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="YamlStream"/> representation of the input <see cref="XmlDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xmlDocuments"/> parameter is null.</exception>
    public static YamlStream ToYamlStream(this IEnumerable<XmlDocument> xmlDocuments, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocuments);

        var yamlStream = new YamlStream();
        foreach (var xmlDocument in xmlDocuments)
        {
            if (xmlDocument == null)
                continue;

            var jsonNode = xmlDocument.ToJsonNode(attributeNameFactory);
            var root = ConvertJsonNodeToYamlNode(jsonNode);
            var doc = new YamlDocument(root);
            yamlStream.Documents.Add(doc);
        }
        return yamlStream;
    }

    /// <summary>
    /// Converts an array of <see cref="XDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xDocuments">The array of <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to YAML property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="YamlStream"/> representation of the input <see cref="XDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xDocuments"/> parameter is null.</exception>
    public static YamlStream ToYamlStream(this IEnumerable<XDocument> xDocuments, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xDocuments);

        var yamlStream = new YamlStream();
        foreach (var xDocument in xDocuments)
        {
            if (xDocument == null)
                continue;

            var jsonNode = xDocument.ToJsonNode(attributeNameFactory);
            var root = ConvertJsonNodeToYamlNode(jsonNode);
            var doc = new YamlDocument(root);
            yamlStream.Documents.Add(doc);
        }
        return yamlStream;
    }

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xmlDocument">The <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to YAML property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="YamlStream"/> representation of the input <see cref="XmlDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xmlDocument"/> parameter is null.</exception>
    public static YamlStream ToYamlStream(this XmlDocument xmlDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        return new XmlDocument[] { xmlDocument }.ToYamlStream(attributeNameFactory);
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xDocument">The <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <param name="attributeNameFactory">
    /// Optional. A function to transform attribute names when converting to YAML property names. 
    /// If not provided, attribute names will be prefixed with '$'.
    /// </param>
    /// <returns>
    /// A <see cref="YamlStream"/> representation of the input <see cref="XDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xDocument"/> parameter is null.</exception>
    public static YamlStream ToYamlStream(this XDocument xDocument, Func<string, string>? attributeNameFactory = null)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        return new XDocument[] { xDocument }.ToYamlStream(attributeNameFactory);
    }

    private static YamlNode ConvertJsonNodeToYamlNode(JsonNode? node)
    {
        if (node is null)
            return new YamlScalarNode(string.Empty);

        if (node is JsonValue value)
        {
            var val = value.GetValue<object?>();
            return new YamlScalarNode(val?.ToString() ?? string.Empty);
        }

        if (node is JsonArray array)
        {
            var seq = new YamlSequenceNode();
            foreach (var item in array)
            {
                seq.Add(ConvertJsonNodeToYamlNode(item));
            }
            return seq;
        }

        if (node is JsonObject obj)
        {
            var mapping = new YamlMappingNode();
            foreach (var kvp in obj)
            {
                mapping.Add(new YamlScalarNode(kvp.Key), ConvertJsonNodeToYamlNode(kvp.Value));
            }
            return mapping;
        }

        return new YamlScalarNode(node.ToJsonString());
    }

    #endregion

    static void CreateInnerTextString(Action<string> onCreate, string? valueStr)
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
            CreateInnerTextString(s => parent.InnerText = s, yamlScalarNode.Value);
        else if (value is JsonNode jsonNode)
            CreateInnerTextString(s => parent.InnerText = s, jsonNode.ToString());
        else if (value is JsonElement jsonElement)
            CreateInnerTextString(s => parent.InnerText = s, jsonElement.ToString());
        else
            CreateInnerTextString(s => parent.InnerText = s, value?.ToString());
    }

    internal static void CreateXmlAttribute(XmlElement parent, string name, object? value)
    {
        void Create(string valueStr)
        {
            var attribute = parent.OwnerDocument.CreateAttribute(name);
            attribute.Value = valueStr;
            parent.SetAttributeNode(attribute);
        }

        if (value is YamlScalarNode yamlScalarNode)
            CreateInnerTextString(Create, yamlScalarNode.Value);
        else if (value is JsonNode jsonNode)
            CreateInnerTextString(Create, jsonNode.ToString());
        else if (value is JsonElement jsonElement)
            CreateInnerTextString(Create, jsonElement.ToString());
        else
            CreateInnerTextString(Create, value?.ToString());
    }

    internal static void CreateXInnerText(XElement parent, object? value)
    {
        if (value is YamlScalarNode yamlScalarNode)
            CreateInnerTextString(s => parent.Value = s, yamlScalarNode.Value);
        else if (value is JsonNode jsonNode)
            CreateInnerTextString(s => parent.Value = s, jsonNode.ToString());
        else if (value is JsonElement jsonElement)
            CreateInnerTextString(s => parent.Value = s, jsonElement.ToString());
        else
            CreateInnerTextString(s => parent.Value = s, value?.ToString());
    }

    internal static void CreateXAttribute(XElement parent, string name, object? value)
    {
        void Create(string valueStr)
        {
            XName xname;
            if (name.Contains(':'))
            {
                var nameSplit = name.Split([':'], 2);
                xname = XName.Get(nameSplit[0], nameSplit[1]);
            }
            else
            {
                xname = XName.Get(name);
            }
            parent.SetAttributeValue(xname, valueStr);
        }

        if (value is YamlScalarNode yamlScalarNode)
            CreateInnerTextString(Create, yamlScalarNode.Value);
        else if (value is JsonNode jsonNode)
            CreateInnerTextString(Create, jsonNode.ToString());
        else if (value is JsonElement jsonElement)
            CreateInnerTextString(Create, jsonElement.ToString());
        else
            CreateInnerTextString(Create, value?.ToString());
    }

    internal static string MakeValidXmlName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "unnamed";

        // Remove invalid characters and ensure the name starts with a valid character
        // XML names must start with a letter or underscore, and can contain letters, digits, hyphens, underscores, and periods
        // See: https://www.w3.org/TR/xml/#NT-Name

        // Replace invalid characters with '_'
        var validName = new System.Text.StringBuilder();
        int i = 0;
        foreach (char c in name)
        {
            if ((i == 0 && (char.IsLetter(c) || c == '_')) ||
                (i > 0 && (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')))
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
}

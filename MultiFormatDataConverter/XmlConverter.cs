using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

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
    /// <returns>A <see cref="JsonNode"/> representation of the input <see cref="XmlDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the xmlDocument parameter is null.</exception>
    public static JsonNode? ToJsonNode(this XmlDocument xmlDocument)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        if (xmlDocument.DocumentElement == null)
        {
            return new JsonObject();
        }

        return ConvertElementToJsonNode(xmlDocument.DocumentElement);
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="xDocument">The <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <returns>A <see cref="JsonNode"/> representation of the input <see cref="XDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the xDocument parameter is null.</exception>
    public static JsonNode? ToJsonNode(this XDocument xDocument)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        return xDocument.ToXmlDocument().ToJsonNode();
    }
    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonNode"/> representing a JSON array.
    /// </summary>
    /// <param name="xDocument">The <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <returns>
    /// A <see cref="JsonNode"/> (specifically a <see cref="JsonArray"/>) representing the child elements of the root element.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xDocument"/> parameter is null.</exception>
    public static JsonNode? ToJsonArray(this XDocument xDocument)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        return xDocument.ToXmlDocument().ToJsonArray();
    }

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="JsonNode"/> representing a JSON array.
    /// </summary>
    /// <param name="xmlDocument">The <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <returns>
    /// A <see cref="JsonNode"/> (specifically a <see cref="JsonArray"/>) representing the child elements of the root element.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xmlDocument"/> parameter is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the XML document does not contain a collection of elements suitable for conversion to a JSON array.
    /// </exception>
    public static JsonNode? ToJsonArray(this XmlDocument xmlDocument)
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
            throw new InvalidOperationException("The XML document cannot be converted to a JSON array because it does not contain a collection of elements.");
        }

        var jsonArray = new JsonArray();
        foreach (var element in childElements)
        {
            jsonArray.Add(ConvertElementToJsonNode(element));
        }

        return jsonArray;
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="xDocument">The <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <returns>
    /// A <see cref="JsonObject"/> representation of the input <see cref="XDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xDocument"/> parameter is null.</exception>
    public static JsonObject? ToJsonObject(this XDocument xDocument)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        return xDocument.ToXmlDocument().ToJsonObject();
    }

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="xmlDocument">The <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <returns>
    /// A <see cref="JsonObject"/> representation of the input <see cref="XmlDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xmlDocument"/> parameter is null.</exception>
    public static JsonObject? ToJsonObject(this XmlDocument xmlDocument)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        var jsonNode = xmlDocument.ToJsonNode();
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
    /// <returns>
    /// A <see cref="JsonDocument"/> representation of the input <see cref="XDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xDocument"/> parameter is null.</exception>
    public static JsonDocument? ToJsonDocument(this XDocument xDocument)
    {
        ArgumentNullException.ThrowIfNull(xDocument);

        return xDocument.ToXmlDocument().ToJsonDocument();
    }

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="xmlDocument">The <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <returns>
    /// A <see cref="JsonDocument"/> representation of the input <see cref="XmlDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xmlDocument"/> parameter is null.</exception>
    public static JsonDocument? ToJsonDocument(this XmlDocument xmlDocument)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        var jsonNode = xmlDocument.ToJsonNode();
        if (jsonNode is null)
        {
            return JsonDocument.Parse("{}");
        }

        return JsonDocument.Parse(jsonNode.ToJsonString());
    }

    private static JsonNode? ConvertElementToJsonNode(XmlElement element)
    {
        var jsonObject = new JsonObject();

        // Add attributes
        foreach (XmlAttribute attr in element.Attributes)
        {
            jsonObject.Add($"@{attr.Name}", JsonValue.Create(attr.Value));
        }

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

        // Add text content if present
        if (hasText && !string.IsNullOrWhiteSpace(textContent))
        {
            jsonObject.Add("#text", JsonValue.Create(textContent));
        }

        // Add child elements
        foreach (var group in childElements)
        {
            string name = group.Key;
            var items = group.Value;

            if (items.Count == 1)
            {
                jsonObject.Add(name, ConvertElementToJsonNode(items[0]));
            }
            else
            {
                var array = new JsonArray();
                foreach (var item in items)
                {
                    array.Add(ConvertElementToJsonNode(item));
                }
                jsonObject.Add(name, array);
            }
        }

        // If the object only has text content and no attributes or elements, return just the text
        if (hasText && !string.IsNullOrWhiteSpace(textContent) &&
            jsonObject.Count == 1 && jsonObject.ContainsKey("#text"))
        {
            return JsonValue.Create(textContent);
        }

        return jsonObject;
    }

    #endregion

    #region ToYaml

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xmlDocument">The <see cref="XmlDocument"/> to convert. Cannot be null.</param>
    /// <returns>
    /// A <see cref="YamlStream"/> representation of the input <see cref="XmlDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xmlDocument"/> parameter is null.</exception>
    public static YamlStream ToYamlStream(this XmlDocument xmlDocument)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        var jsonNode = xmlDocument.ToJsonNode();
        var yamlStream = new YamlStream();
        var root = ConvertJsonNodeToYamlNode(jsonNode);
        var doc = new YamlDocument(root);
        yamlStream.Documents.Add(doc);
        return yamlStream;
    }

    /// <summary>
    /// Converts an <see cref="XDocument"/> to a <see cref="YamlStream"/>.
    /// </summary>
    /// <param name="xDocument">The <see cref="XDocument"/> to convert. Cannot be null.</param>
    /// <returns>
    /// A <see cref="YamlStream"/> representation of the input <see cref="XDocument"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="xDocument"/> parameter is null.</exception>
    public static YamlStream ToYamlStream(this XDocument xDocument)
    {
        ArgumentNullException.ThrowIfNull(xDocument);
        return xDocument.ToXmlDocument().ToYamlStream();
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
}

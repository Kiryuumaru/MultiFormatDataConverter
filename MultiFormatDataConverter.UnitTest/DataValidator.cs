using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;
using YamlDotNet.RepresentationModel;

namespace MultiFormatDataConverter.UnitTest;

public static class JsonValidator
{
    public class JsonComparisonException(string message) : Exception(message)
    {
    }

    public static void AreEqual(JsonNode? node1, JsonNode? node2, bool nullIsEmpty = false)
    {
        AreEqualInternal(node1, node2, "$", nullIsEmpty);
    }

    private static void AreEqualInternal(JsonNode? node1, JsonNode? node2, string path, bool nullIsEmpty)
    {
        // Both null case
        if (node1 == null && node2 == null)
        {
            return;
        }

        // One null, one not null case
        if (node1 == null || node2 == null)
        {
            if (nullIsEmpty)
            {
                var rawValue1 = FormatValue(node1);
                var rawValue2 = FormatValue(node2);
                if (!(rawValue1 is null || rawValue1.Equals("null") || rawValue1.Equals("{}") || rawValue1.Equals("")) ||
                    !(rawValue2 is null || rawValue2.Equals("null") || rawValue2.Equals("{}") || rawValue2.Equals("")))
                {
                    throw new JsonComparisonException($"Property {path}: Expected {FormatValue(node1)}, Actual {FormatValue(node2)}");
                }
            }
            else
            {
                throw new JsonComparisonException($"Property {path}: Expected {FormatValue(node1)}, Actual {FormatValue(node2)}");
            }
        }

        // JsonValue comparison
        if (node1 is JsonValue value1 && node2 is JsonValue value2)
        {
            if (value1.GetValueKind() == value2.GetValueKind())
            {
                // Compare the underlying values
                var rawValue1 = value1.ToJsonString();
                var rawValue2 = value2.ToJsonString();

                // Handle number type conversions
                if (IsNumber(rawValue1) && IsNumber(rawValue2))
                {
                    if (!Convert.ToDecimal(rawValue1).Equals(Convert.ToDecimal(rawValue2)))
                    {
                        throw new JsonComparisonException($"Property {path}: Expected {rawValue1}, Actual {rawValue2}");
                    }

                    return;
                }

                if (nullIsEmpty)
                {
                    if ((rawValue1 is null || rawValue1.Equals("null") || rawValue1.Equals("{}") || rawValue1.Equals("")) &&
                        (rawValue2 is null || rawValue2.Equals("null") || rawValue2.Equals("{}") || rawValue2.Equals("")))
                    {
                        return;
                    }
                }

                if (!Equals(rawValue1, rawValue2))
                {
                    throw new JsonComparisonException($"Property {path}: Expected {FormatValue(rawValue1)}, Actual {FormatValue(rawValue2)}");
                }
            }
            else
            {
                throw new JsonComparisonException($"Property {path}: Value kinds do not match. Expected {value1.GetValueKind()}, Actual {value2.GetValueKind()}");
            }

            return;
        }

        // JsonObject comparison
        if (node1 is JsonObject obj1 && node2 is JsonObject obj2)
        {
            // Check if the objects have the same properties
            if (obj1.Count != obj2.Count)
            {
                var missingInObj1 = string.Join(", ", obj2.Select(kv => kv.Key).Except(obj1.Select(kv => kv.Key)));
                var missingInObj2 = string.Join(", ", obj1.Select(kv => kv.Key).Except(obj2.Select(kv => kv.Key)));

                StringBuilder sb = new();
                sb.AppendLine($"Property {path}: Object property count mismatch. Expected {obj1.Count}, Actual {obj2.Count}");

                if (!string.IsNullOrEmpty(missingInObj2))
                    sb.AppendLine($"Properties missing in actual: {missingInObj2}");

                if (!string.IsNullOrEmpty(missingInObj1))
                    sb.AppendLine($"Extra properties in actual: {missingInObj1}");

                throw new JsonComparisonException(sb.ToString());
            }

            // Recursively compare each property
            foreach (var property in obj1)
            {
                if (!obj2.ContainsKey(property.Key))
                {
                    throw new JsonComparisonException($"Property {path}.{property.Key}: Missing in actual object");
                }

                var childPath = $"{path}.{property.Key}";

                AreEqualInternal(property.Value, obj2[property.Key], childPath, nullIsEmpty);
            }

            return;
        }

        // JsonArray comparison
        if (node1 is JsonArray array1 && node2 is JsonArray array2)
        {
            if (array1.Count != array2.Count)
            {
                throw new JsonComparisonException($"Property {path}: Array length mismatch. Expected {array1.Count}, Actual {array2.Count}");
            }

            // Recursively compare each element
            for (int i = 0; i < array1.Count; i++)
            {
                var childPath = $"{path}[{i}]";
                AreEqualInternal(array1[i], array2[i], childPath, nullIsEmpty);
            }

            return;
        }

        // Different types
        if (node1?.GetType() != node2?.GetType())
        {
            if (nullIsEmpty)
            {
                var rawValue1 = FormatValue(node1);
                var rawValue2 = FormatValue(node2);
                if ((rawValue1 is null || rawValue1.Equals("null") || rawValue1.Equals("{}") || rawValue1.Equals("")) &&
                    (rawValue2 is null || rawValue2.Equals("null") || rawValue2.Equals("{}") || rawValue2.Equals("")))
                {
                    return;
                }
            }

            throw new JsonComparisonException($"Property {path}: Type mismatch. Expected {node1?.GetType().Name}, Actual {node2?.GetType().Name}");
        }

        // If we get here, we don't know how to compare these nodes
        throw new JsonComparisonException($"Property {path}: Unknown node types for comparison");
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is string strValue)
            return $"\"{strValue}\"";

        return value.ToString() ?? "null";
    }

    private static bool IsNumber(object value)
    {
        return value is sbyte || value is byte || value is short || value is ushort ||
               value is int || value is uint || value is long || value is ulong ||
               value is float || value is double || value is decimal;
    }
}

public static class YamlValidator
{
    public class YamlComparisonException(string message) : Exception(message)
    {
    }

    public static void AreEqual(YamlStream? yaml1, YamlStream? yaml2, bool nullIsEmpty = false)
    {
        AreEqualInternal(yaml1, yaml2, "$", nullIsEmpty);
    }

    private static void AreEqualInternal(YamlStream? yaml1, YamlStream? yaml2, string path, bool nullIsEmpty)
    {
        // Both null
        if (yaml1 == null && yaml2 == null)
            return;

        // One null
        if (yaml1 == null || yaml2 == null)
        {
            if (nullIsEmpty)
            {
                if ((yaml1 == null || yaml1.Documents.Count == 0) &&
                    (yaml2 == null || yaml2.Documents.Count == 0))
                    return;
            }
            throw new YamlComparisonException($"YAML {path}: One stream is null/empty, the other is not.");
        }

        if (yaml1.Documents.Count != yaml2.Documents.Count)
        {
            throw new YamlComparisonException($"YAML {path}: Document count mismatch. Expected {yaml1.Documents.Count}, Actual {yaml2.Documents.Count}");
        }

        for (int i = 0; i < yaml1.Documents.Count; i++)
        {
            var docPath = $"{path}#doc[{i}]";
            AreEqualNodes(yaml1.Documents[i].RootNode, yaml2.Documents[i].RootNode, docPath, nullIsEmpty);
        }
    }

    private static void AreEqualNodes(YamlNode? node1, YamlNode? node2, string path, bool nullIsEmpty)
    {
        // Both null
        if (node1 == null && node2 == null)
            return;

        if (nullIsEmpty)
        {
            if (IsNullOrEmptyYamlNode(node1) && IsNullOrEmptyYamlNode(node2))
                return;
        }

        // One null
        if (node1 == null || node2 == null)
        {
            throw new YamlComparisonException($"YAML {path}: One node is null/empty, the other is not.");
        }

        if (node1.GetType() != node2.GetType())
        {
            throw new YamlComparisonException($"YAML {path}: Node type mismatch. Expected {node1.GetType().Name}, Actual {node2.GetType().Name}");
        }

        switch (node1)
        {
            case YamlScalarNode scalar1 when node2 is YamlScalarNode scalar2:
                var val1 = FormatValue(scalar1.Value);
                var val2 = FormatValue(scalar2.Value);
                if (nullIsEmpty)
                {
                    if (string.IsNullOrEmpty(val1) && string.IsNullOrEmpty(val2))
                        return;
                }
                if (!string.Equals(val1, val2, StringComparison.Ordinal))
                {
                    throw new YamlComparisonException($"YAML {path}: Scalar value mismatch. Expected '{val1}', Actual '{val2}'");
                }
                break;

            case YamlSequenceNode seq1 when node2 is YamlSequenceNode seq2:
                if (seq1.Children.Count != seq2.Children.Count)
                {
                    throw new YamlComparisonException($"YAML {path}: Sequence length mismatch. Expected {seq1.Children.Count}, Actual {seq2.Children.Count}");
                }
                for (int i = 0; i < seq1.Children.Count; i++)
                {
                    AreEqualNodes(seq1.Children[i], seq2.Children[i], $"{path}[{i}]", nullIsEmpty);
                }
                break;

            case YamlMappingNode map1 when node2 is YamlMappingNode map2:
                if (map1.Children.Count != map2.Children.Count)
                {
                    var keys1 = map1.Children.Keys.Select(k => k.ToString()).ToHashSet();
                    var keys2 = map2.Children.Keys.Select(k => k.ToString()).ToHashSet();
                    var missingIn2 = string.Join(", ", keys1.Except(keys2));
                    var extraIn2 = string.Join(", ", keys2.Except(keys1));
                    var sb = new StringBuilder();
                    sb.AppendLine($"YAML {path}: Mapping key count mismatch. Expected {map1.Children.Count}, Actual {map2.Children.Count}");
                    if (!string.IsNullOrEmpty(missingIn2))
                        sb.AppendLine($"Keys missing in actual: {missingIn2}");
                    if (!string.IsNullOrEmpty(extraIn2))
                        sb.AppendLine($"Extra keys in actual: {extraIn2}");
                    throw new YamlComparisonException(sb.ToString());
                }
                foreach (var kvp in map1.Children)
                {
                    if (!map2.Children.TryGetValue(kvp.Key, out var value2))
                    {
                        throw new YamlComparisonException($"YAML {path}.{kvp.Key}: Key missing in actual mapping.");
                    }
                    AreEqualNodes(kvp.Value, value2, $"{path}.{kvp.Key}", nullIsEmpty);
                }
                break;

            default:
                throw new YamlComparisonException($"YAML {path}: Unknown or unsupported YamlNode type: {node1.GetType().Name}");
        }
    }

    private static bool IsNullOrEmptyYamlNode(YamlNode? node)
    {
        if (node == null)
            return true;
        if (node is YamlScalarNode scalar)
            return string.IsNullOrEmpty(scalar.Value);
        if (node is YamlSequenceNode seq)
            return seq.Children.Count == 0;
        if (node is YamlMappingNode map)
            return map.Children.Count == 0;
        return false;
    }

    private static string FormatValue(object? value)
    {
        var strValue = value?.ToString() ?? "null";

        if (strValue.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
            strValue.Equals("yes", StringComparison.InvariantCultureIgnoreCase))
        {
            return "true";
        }
        if (strValue.Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
            strValue.Equals("no", StringComparison.InvariantCultureIgnoreCase))
        {
            return "false";
        }

        if (bool.TryParse(strValue, out var boolValue))
            return boolValue.ToString();

        if (int.TryParse(strValue, out var intValue))
            return intValue.ToString();

        if (long.TryParse(strValue, out var longValue))
            return longValue.ToString();

        if (double.TryParse(strValue, out var doubleValue))
            return doubleValue.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

        if (decimal.TryParse(strValue, out var decimalValue))
            return decimalValue.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

        return strValue;
    }
}

public static class XmlValidator
{
    public class XmlComparisonException(string message) : Exception(message)
    {
    }

    public static void AreEqual(XDocument? doc1, XDocument? doc2, bool nullIsEmpty = false)
    {
        AreEqualInternal(doc1?.Root, doc2?.Root, "$", nullIsEmpty);
    }

    public static void AreEqual(System.Xml.XmlDocument? doc1, System.Xml.XmlDocument? doc2, bool nullIsEmpty = false)
    {
        AreEqualInternal(doc1?.DocumentElement, doc2?.DocumentElement, "$", nullIsEmpty);
    }

    private static void AreEqualInternal(XElement? elem1, XElement? elem2, string path, bool nullIsEmpty)
    {
        // Both null
        if (elem1 == null && elem2 == null)
            return;

        // One null
        if (elem1 == null || elem2 == null)
        {
            if (nullIsEmpty && (IsNullOrEmpty(elem1) && IsNullOrEmpty(elem2)))
                return;
            throw new XmlComparisonException($"XML {path}: One element is null/empty, the other is not.");
        }

        // Name
        if (elem1.Name != elem2.Name)
            throw new XmlComparisonException($"XML {path}: Element name mismatch. Expected '{elem1.Name}', Actual '{elem2.Name}'");

        // Attributes
        var attrs1 = elem1.Attributes().OrderBy(a => a.Name.ToString()).ToList();
        var attrs2 = elem2.Attributes().OrderBy(a => a.Name.ToString()).ToList();
        if (attrs1.Count != attrs2.Count)
            throw new XmlComparisonException($"XML {path}: Attribute count mismatch. Expected {attrs1.Count}, Actual {attrs2.Count}");

        for (int i = 0; i < attrs1.Count; i++)
        {
            if (attrs1[i].Name != attrs2[i].Name || attrs1[i].Value != attrs2[i].Value)
                throw new XmlComparisonException($"XML {path}: Attribute mismatch at '{attrs1[i].Name}'. Expected '{attrs1[i].Value}', Actual '{attrs2[i].Value}'");
        }

        // Value (for leaf nodes)
        var val1 = elem1.HasElements ? null : elem1.Value?.Trim();
        var val2 = elem2.HasElements ? null : elem2.Value?.Trim();
        if (!elem1.HasElements && !elem2.HasElements)
        {
            if (nullIsEmpty && (string.IsNullOrEmpty(val1) && string.IsNullOrEmpty(val2)))
                return;
            if (FormatValue(val1) != FormatValue(val2))
                throw new XmlComparisonException($"XML {path}: Value mismatch. Expected '{val1}', Actual '{val2}'");
        }

        // Children
        var children1 = elem1.Elements().ToList();
        var children2 = elem2.Elements().ToList();
        if (children1.Count != children2.Count)
            throw new XmlComparisonException($"XML {path}: Child element count mismatch. Expected {children1.Count}, Actual {children2.Count}");

        for (int i = 0; i < children1.Count; i++)
        {
            var childPath = $"{path}/{children1[i].Name.LocalName}[{i}]";
            AreEqualInternal(children1[i], children2[i], childPath, nullIsEmpty);
        }
    }

    private static void AreEqualInternal(System.Xml.XmlElement? elem1, System.Xml.XmlElement? elem2, string path, bool nullIsEmpty)
    {
        // Both null
        if (elem1 == null && elem2 == null)
            return;

        // One null
        if (elem1 == null || elem2 == null)
        {
            if (nullIsEmpty && (IsNullOrEmpty(elem1) && IsNullOrEmpty(elem2)))
                return;
            throw new XmlComparisonException($"XML {path}: One element is null/empty, the other is not.");
        }

        // Name
        if (elem1.Name != elem2.Name)
            throw new XmlComparisonException($"XML {path}: Element name mismatch. Expected '{elem1.Name}', Actual '{elem2.Name}'");

        // Attributes
        var attrs1 = elem1.Attributes.Cast<System.Xml.XmlAttribute>().OrderBy(a => a.Name).ToList();
        var attrs2 = elem2.Attributes.Cast<System.Xml.XmlAttribute>().OrderBy(a => a.Name).ToList();

        // Compare attributes regardless of order
        var attrs1Dict = attrs1.ToDictionary(a => a.Name.ToString(), a => a.Value);
        var attrs2Dict = attrs2.ToDictionary(a => a.Name.ToString(), a => a.Value);

        if (attrs1Dict.Count != attrs2Dict.Count)
            throw new XmlComparisonException($"XML {path}: Attribute count mismatch. Expected {attrs1Dict.Count}, Actual {attrs2Dict.Count}");

        foreach (var kvp in attrs1Dict)
        {
            if (!attrs2Dict.TryGetValue(kvp.Key, out var value2) || kvp.Value != value2)
                throw new XmlComparisonException($"XML {path}: Attribute mismatch at '{kvp.Key}'. Expected '{kvp.Value}', Actual '{value2 ?? "missing"}'");
        }

        // Value (for leaf nodes)
        var val1 = elem1.HasChildNodes && elem1.ChildNodes.OfType<System.Xml.XmlElement>().Any() ? null : elem1.InnerText?.Trim();
        var val2 = elem2.HasChildNodes && elem2.ChildNodes.OfType<System.Xml.XmlElement>().Any() ? null : elem2.InnerText?.Trim();
        if (!(elem1.HasChildNodes && elem1.ChildNodes.OfType<System.Xml.XmlElement>().Any()) &&
            !(elem2.HasChildNodes && elem2.ChildNodes.OfType<System.Xml.XmlElement>().Any()))
        {
            if (nullIsEmpty && (string.IsNullOrEmpty(val1) && string.IsNullOrEmpty(val2)))
                return;
            if (FormatValue(val1) != FormatValue(val2))
                throw new XmlComparisonException($"XML {path}: Value mismatch. Expected '{val1}', Actual '{val2}'");
        }

        // Children
        var children1 = elem1.ChildNodes.OfType<System.Xml.XmlElement>().ToList();
        var children2 = elem2.ChildNodes.OfType<System.Xml.XmlElement>().ToList();
        if (children1.Count != children2.Count)
            throw new XmlComparisonException($"XML {path}: Child element count mismatch. Expected {children1.Count}, Actual {children2.Count}");

        for (int i = 0; i < children1.Count; i++)
        {
            var childPath = $"{path}/{children1[i].Name}[{i}]";
            AreEqualInternal(children1[i], children2[i], childPath, nullIsEmpty);
        }
    }

    private static bool IsNullOrEmpty(XElement? elem)
    {
        if (elem == null)
            return true;
        if (!elem.HasElements && string.IsNullOrWhiteSpace(elem.Value) && !elem.HasAttributes)
            return true;
        if (elem.HasElements && !elem.Elements().Any() && !elem.HasAttributes)
            return true;
        return false;
    }

    private static bool IsNullOrEmpty(System.Xml.XmlElement? elem)
    {
        if (elem == null)
            return true;
        if (!elem.HasChildNodes && string.IsNullOrWhiteSpace(elem.InnerText) && elem.Attributes.Count == 0)
            return true;
        if (elem.HasChildNodes && !elem.ChildNodes.OfType<System.Xml.XmlElement>().Any() && elem.Attributes.Count == 0)
            return true;
        return false;
    }

    private static string FormatValue(object? value)
    {
        var strValue = value?.ToString() ?? "null";

        if (strValue.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
            strValue.Equals("yes", StringComparison.InvariantCultureIgnoreCase))
        {
            return "true";
        }
        if (strValue.Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
            strValue.Equals("no", StringComparison.InvariantCultureIgnoreCase))
        {
            return "false";
        }

        if (bool.TryParse(strValue, out var boolValue))
            return boolValue.ToString();

        if (int.TryParse(strValue, out var intValue))
            return intValue.ToString();

        if (long.TryParse(strValue, out var longValue))
            return longValue.ToString();

        if (double.TryParse(strValue, out var doubleValue))
            return doubleValue.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

        if (decimal.TryParse(strValue, out var decimalValue))
            return decimalValue.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

        return strValue;
    }
}

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MultiFormatDataConverter.UnitTest;

public static class JsonValidator
{
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

public class JsonComparisonException(string message) : Exception(message)
{
}
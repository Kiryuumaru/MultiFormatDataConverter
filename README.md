# MultiFormatDataConverter

A versatile .NET library for converting data between JSON, YAML, and XML formats. This library provides a comprehensive set of extension methods to seamlessly convert between different data representation formats.

## Installation

Install the package via NuGet:

```
dotnet add package MultiFormatDataConverter
```

## Features

- Convert between JSON, YAML, and XML with simple, intuitive extension methods
- Support for various object types in each format:
  - **JSON**: JsonNode, JsonObject, JsonArray, JsonDocument
  - **XML**: XmlDocument, XDocument
  - **YAML**: YamlStream
- Cross-platform compatibility
- AOT-compatible
- Comprehensive XML documentation
- .NET 6.0+ support

## Usage Examples

### Converting between XML and JSON

```csharp
// Convert from XML to JSON
XmlDocument xmlDoc = new XmlDocument();
xmlDoc.LoadXml("<root><item>value</item></root>");
JsonNode jsonNode = xmlDoc.ToJsonNode();

// Convert from JSON to XML
JsonObject jsonObject = new JsonObject { ["property"] = "value" };
XmlDocument convertedXml = jsonObject.ToXmlDocument();
```

### Converting between JSON and YAML

```csharp
// Convert JSON to YAML
JsonObject jsonObject = new JsonObject { ["name"] = "John", ["age"] = 30 };
YamlStream yamlStream = jsonObject.ToYamlStream();

// Convert JSON document to YAML
JsonDocument jsonDocument = JsonDocument.Parse("{\"items\": [1, 2, 3]}");
YamlStream yamlFromDoc = jsonDocument.ToYamlStream();
```

### Converting between YAML and XML

```csharp
// First convert YAML to JSON, then to XML
YamlStream yamlStream = new YamlStream();
using (var reader = new StringReader("key: value"))
{
    yamlStream.Load(reader);
}
JsonObject? jsonObject = yamlStream.ToJsonObject();
XmlDocument xmlDoc = jsonObject.ToXmlDocument();
```

## API Overview

The library provides three main static classes:

- **XmlConverter**: Convert to/from XML formats (XmlDocument, XDocument)
- **JsonConverter**: Convert to/from JSON formats (JsonNode, JsonObject, JsonArray, JsonDocument)
- **YamlConverter**: Convert to/from YAML formats (YamlStream)

## Framework Support

- .NET STANDARD 2.0
- .NET 6.0
- .NET 7.0
- .NET 8.0
- .NET 9.0

## License

This project is licensed under the terms specified in the LICENSE.txt file.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

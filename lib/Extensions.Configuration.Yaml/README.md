# DarkPatterns.Extensions.Configuration.Yaml

An extension for .NET's `System.Extensions.Configuration` that adds support for
combining YAML documents with your configuration.

## Usage

```csharp
var builder = new ConfigurationBuilder();
builder.AddYamlStream(stream);
builder.AddYamlFile(file);
```

Additional configuration options include:

- Specify an `IFileProvider` to access the file from an interface file system
- Specify whether a file should be optional or required
- Specify whether to monitor the file for changes

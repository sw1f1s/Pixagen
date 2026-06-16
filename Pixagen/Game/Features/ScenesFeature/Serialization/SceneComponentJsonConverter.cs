using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pixagen.Game.Features.ScenesFeature.Serialization;

public sealed class SceneComponentJsonConverter : JsonConverter<IComponent>
{
    private const string TypePropertyName = "type";

    public override IComponent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (!Vector3JsonConverter.TryGetProperty(root, TypePropertyName, out JsonElement typeElement) ||
            typeElement.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Scene component must contain a string 'type' property.");
        }

        string? typeName = typeElement.GetString();
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new JsonException("Scene component 'type' property cannot be empty.");
        }

        Type componentType = SceneComponentRegistry.ResolveType(typeName);
        object? component = JsonSerializer.Deserialize(root.GetRawText(), componentType, options);
        return component is IComponent typedComponent
            ? typedComponent
            : throw new JsonException($"Scene component '{typeName}' could not be deserialized.");
    }

    public override void Write(Utf8JsonWriter writer, IComponent value, JsonSerializerOptions options)
    {
        Type componentType = value.GetType();
        string typeName = SceneComponentRegistry.ResolveName(componentType);
        JsonElement componentElement = JsonSerializer.SerializeToElement(value, componentType, options);

        writer.WriteStartObject();
        writer.WriteString(TypePropertyName, typeName);
        foreach (JsonProperty property in componentElement.EnumerateObject())
        {
            if (string.Equals(property.Name, TypePropertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}

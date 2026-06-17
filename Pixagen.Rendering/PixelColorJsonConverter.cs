using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pixagen.Rendering;

public sealed class PixelColorJsonConverter : JsonConverter<PixelColor>
{
    public override PixelColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (PixelColor.TryParse(value, out PixelColor color))
            {
                return color;
            }

            throw new JsonException($"Invalid pixel color '{value}'. Use '#RRGGBB', '#RGB', or an object with r/g/b values.");
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement root = document.RootElement;
            return new PixelColor(
                ReadByte(root, "r"),
                ReadByte(root, "g"),
                ReadByte(root, "b"));
        }

        throw new JsonException("Pixel color must be '#RRGGBB', '#RGB', or an object with r/g/b values.");
    }

    public override void Write(Utf8JsonWriter writer, PixelColor value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToJsonString());
    }

    private static byte ReadByte(JsonElement element, string propertyName)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.Number &&
                property.Value.TryGetInt32(out int value))
            {
                return (byte)Math.Clamp(value, 0, 255);
            }
        }

        return 0;
    }
}

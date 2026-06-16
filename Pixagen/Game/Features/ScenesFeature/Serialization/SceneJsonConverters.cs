using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Rendering;

namespace Pixagen.Game.Features.ScenesFeature.Serialization;

public sealed class FixJsonConverter : JsonConverter<Fix>
{
    public override Fix Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return Fix.FromDouble(reader.GetDouble());
        }

        if (reader.TokenType == JsonTokenType.String &&
            double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            return Fix.FromDouble(value);
        }

        return Fix.Zero;
    }

    public override void Write(Utf8JsonWriter writer, Fix value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((double)value);
    }
}

public sealed class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        return new Vector3(
            Fix.FromDouble(GetDouble(root, "x")),
            Fix.FromDouble(GetDouble(root, "y")),
            Fix.FromDouble(GetDouble(root, "z")));
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", (double)value.X);
        writer.WriteNumber("y", (double)value.Y);
        writer.WriteNumber("z", (double)value.Z);
        writer.WriteEndObject();
    }

    internal static double GetDouble(JsonElement element, string propertyName, double fallback = 0)
    {
        if (!TryGetProperty(element, propertyName, out JsonElement property))
        {
            return fallback;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.GetDouble(),
            JsonValueKind.String when double.TryParse(
                property.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value) => value,
            _ => fallback
        };
    }

    internal static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        foreach (JsonProperty candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }
}

public sealed class QuaternionJsonConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (Vector3JsonConverter.TryGetProperty(root, "direction", out JsonElement directionElement))
        {
            Vector3 direction = directionElement.Deserialize<Vector3>(options);
            return direction.IsZero ? Quaternion.Identity : Quaternion.FromDirection(direction.Normalized);
        }

        if (Vector3JsonConverter.TryGetProperty(root, "axisAngle", out JsonElement axisAngleElement))
        {
            Vector3 axis = Vector3.Up;
            if (Vector3JsonConverter.TryGetProperty(axisAngleElement, "axis", out JsonElement axisElement))
            {
                axis = axisElement.Deserialize<Vector3>(options);
            }

            if (axis.IsZero)
            {
                axis = Vector3.Up;
            }

            double angle = Vector3JsonConverter.GetDouble(axisAngleElement, "angle");
            return Quaternion.FromAxisAngle(axis.Normalized, Fix.FromDouble(angle));
        }

        if (TryReadComponents(root, out Quaternion quaternion))
        {
            return quaternion.Normalized;
        }

        return Quaternion.Identity;
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        Quaternion normalized = value.Normalized;
        writer.WriteStartObject();
        writer.WriteNumber("x", (double)normalized.X);
        writer.WriteNumber("y", (double)normalized.Y);
        writer.WriteNumber("z", (double)normalized.Z);
        writer.WriteNumber("w", (double)normalized.W);
        writer.WriteEndObject();
    }

    private static bool TryReadComponents(JsonElement root, out Quaternion quaternion)
    {
        if (!Vector3JsonConverter.TryGetProperty(root, "x", out _) ||
            !Vector3JsonConverter.TryGetProperty(root, "y", out _) ||
            !Vector3JsonConverter.TryGetProperty(root, "z", out _) ||
            !Vector3JsonConverter.TryGetProperty(root, "w", out _))
        {
            quaternion = Quaternion.Identity;
            return false;
        }

        quaternion = new Quaternion(
            Fix.FromDouble(Vector3JsonConverter.GetDouble(root, "x")),
            Fix.FromDouble(Vector3JsonConverter.GetDouble(root, "y")),
            Fix.FromDouble(Vector3JsonConverter.GetDouble(root, "z")),
            Fix.FromDouble(Vector3JsonConverter.GetDouble(root, "w")));
        return true;
    }
}

public sealed class TransformJsonConverter : JsonConverter<Transform>
{
    public override Transform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        Vector3 position = Vector3.Zero;
        Quaternion rotation = Quaternion.Identity;
        Vector3 scale = Vector3.One;

        if (Vector3JsonConverter.TryGetProperty(root, "position", out JsonElement positionElement))
        {
            position = positionElement.Deserialize<Vector3>(options);
        }

        if (Vector3JsonConverter.TryGetProperty(root, "rotation", out JsonElement rotationElement))
        {
            rotation = rotationElement.Deserialize<Quaternion>(options);
        }

        if (Vector3JsonConverter.TryGetProperty(root, "scale", out JsonElement scaleElement))
        {
            scale = scaleElement.Deserialize<Vector3>(options);
        }

        return new Transform(position, rotation, scale);
    }

    public override void Write(Utf8JsonWriter writer, Transform value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("position");
        JsonSerializer.Serialize(writer, value.Position, options);
        writer.WritePropertyName("rotation");
        JsonSerializer.Serialize(writer, value.Rotation, options);
        writer.WritePropertyName("scale");
        JsonSerializer.Serialize(writer, value.Scale, options);
        writer.WriteEndObject();
    }
}

public sealed class MaterialJsonConverter : JsonConverter<Material>
{
    public override Material Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        PixelColor color = PixelColor.FromRgb(255, 255, 255);
        MaterialTexture? texture = null;
        MaterialTransparency? transparency = null;
        MaterialShaderKind shader = MaterialShaderKind.Lit;

        if (Vector3JsonConverter.TryGetProperty(root, "color", out JsonElement colorElement))
        {
            color = colorElement.Deserialize<PixelColor>(options);
        }

        if (Vector3JsonConverter.TryGetProperty(root, "texture", out JsonElement textureElement) &&
            textureElement.ValueKind == JsonValueKind.Object)
        {
            texture = ReadTexture(textureElement);
        }

        if (Vector3JsonConverter.TryGetProperty(root, "transparent", out JsonElement transparentElement) &&
            transparentElement.ValueKind == JsonValueKind.Object)
        {
            transparency = ReadTransparency(transparentElement);
        }

        if (Vector3JsonConverter.TryGetProperty(root, "shader", out JsonElement shaderElement))
        {
            shader = ReadShader(shaderElement);
        }

        return new Material(color, texture, transparency, shader);
    }

    public override void Write(Utf8JsonWriter writer, Material value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("color");
        JsonSerializer.Serialize(writer, value.Color, options);

        if (value.Texture is { } texture && !string.IsNullOrWhiteSpace(texture.Asset))
        {
            writer.WritePropertyName("texture");
            WriteTexture(writer, texture);
        }

        if (value.Transparency is { } transparency)
        {
            writer.WritePropertyName("transparent");
            WriteTransparency(writer, transparency);
        }

        writer.WritePropertyName("shader");
        JsonSerializer.Serialize(writer, value.Shader, options);
        writer.WriteEndObject();
    }

    private static MaterialTexture ReadTexture(JsonElement element)
    {
        string asset = string.Empty;
        if (Vector3JsonConverter.TryGetProperty(element, "asset", out JsonElement assetElement) &&
            assetElement.ValueKind == JsonValueKind.String)
        {
            asset = assetElement.GetString() ?? string.Empty;
        }

        return new MaterialTexture(
            asset,
            Fix.FromDouble(Vector3JsonConverter.GetDouble(element, "tilingX", 1)),
            Fix.FromDouble(Vector3JsonConverter.GetDouble(element, "tilingY", 1)),
            Fix.FromDouble(Vector3JsonConverter.GetDouble(element, "offsetX")),
            Fix.FromDouble(Vector3JsonConverter.GetDouble(element, "offsetY")));
    }

    private static void WriteTexture(Utf8JsonWriter writer, MaterialTexture value)
    {
        writer.WriteStartObject();
        writer.WriteString("asset", value.Asset);
        writer.WriteNumber("tilingX", (double)value.TilingX);
        writer.WriteNumber("tilingY", (double)value.TilingY);
        writer.WriteNumber("offsetX", (double)value.OffsetX);
        writer.WriteNumber("offsetY", (double)value.OffsetY);
        writer.WriteEndObject();
    }

    private static MaterialTransparency ReadTransparency(JsonElement element)
    {
        return new MaterialTransparency(
            Fix.FromDouble(Vector3JsonConverter.GetDouble(element, "opacity", 1)),
            Fix.FromDouble(Vector3JsonConverter.GetDouble(element, "alphaCutoff")));
    }

    private static MaterialShaderKind ReadShader(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            Vector3JsonConverter.TryGetProperty(element, "kind", out JsonElement kindElement))
        {
            element = kindElement;
        }

        if (element.ValueKind == JsonValueKind.String &&
            Enum.TryParse(element.GetString(), ignoreCase: true, out MaterialShaderKind shader))
        {
            return shader;
        }

        if (element.ValueKind == JsonValueKind.Number &&
            element.TryGetInt32(out int rawValue) &&
            Enum.IsDefined(typeof(MaterialShaderKind), rawValue))
        {
            return (MaterialShaderKind)rawValue;
        }

        return MaterialShaderKind.Lit;
    }

    private static void WriteTransparency(Utf8JsonWriter writer, MaterialTransparency value)
    {
        writer.WriteStartObject();
        writer.WriteNumber("opacity", (double)value.Opacity);
        writer.WriteNumber("alphaCutoff", (double)value.AlphaCutoff);
        writer.WriteEndObject();
    }
}

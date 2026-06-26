using System.Reflection;
using System.Text.Json.Serialization;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.ScenesFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;

namespace Pixagen.Editor.Workspace;

public sealed record EditorComponentDescriptor(Type Type, string DisplayName, string NamespaceName);

public static class EditorComponentCatalog
{
    private static readonly Lazy<IReadOnlyList<EditorComponentDescriptor>> Components = new(BuildComponents);

    public static IReadOnlyList<EditorComponentDescriptor> All => Components.Value;

    public static IEnumerable<EditorComponentDescriptor> GetAddableComponents(EditorSceneNode node, string search)
    {
        string query = Normalize(search);
        foreach (EditorComponentDescriptor descriptor in All)
        {
            if (node.Object.Components.Any(component => component.GetType() == descriptor.Type))
            {
                continue;
            }

            if (query.Length > 0 &&
                !Normalize(descriptor.DisplayName).Contains(query, StringComparison.Ordinal) &&
                !Normalize(descriptor.NamespaceName).Contains(query, StringComparison.Ordinal))
            {
                continue;
            }

            yield return descriptor;
        }
    }

    public static bool IsProtectedComponent(Type type)
    {
        return type == typeof(Info) ||
            type == typeof(Transform) ||
            type == typeof(LocalTransform);
    }

    public static IComponent CreateDefault(Type type, string objectName)
    {
        if (!IsAuthorableComponent(type) && type != typeof(Info))
        {
            throw new InvalidOperationException($"Type '{type.FullName}' is not an editor component.");
        }

        object instance = Activator.CreateInstance(type)!;
        ApplyAuthoringDefaults(type, ref instance, objectName);
        if (instance is IComponent component)
        {
            return component;
        }

        throw new InvalidOperationException($"Type '{type.FullName}' is not an editor component.");
    }

    private static IReadOnlyList<EditorComponentDescriptor> BuildComponents()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(IsAuthorableComponent)
            .Select(type => new EditorComponentDescriptor(
                type,
                type.Name,
                type.Namespace ?? string.Empty))
            .OrderBy(component => component.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    private static bool IsAuthorableComponent(Type type)
    {
        if (!type.IsValueType ||
            type.IsAbstract ||
            type.IsGenericTypeDefinition ||
            type == typeof(SceneObject) ||
            type == typeof(PhysicsBodyReference) ||
            type == typeof(Parent) ||
            !typeof(IComponent).IsAssignableFrom(type))
        {
            return false;
        }

        if (typeof(IOneTickComponent).IsAssignableFrom(type) ||
            ImplementsOpenGeneric(type, typeof(IAutoPoolComponent<>)) ||
            ImplementsOpenGeneric(type, typeof(IAutoCopyComponent<>)) ||
            ImplementsOpenGeneric(type, typeof(IAutoDestroyComponent<>)) ||
            ImplementsOpenGeneric(type, typeof(IAutoResetComponent<>)))
        {
            return false;
        }

        string name = type.Name;
        return !name.Contains("Dirty", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith("OneTick", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith("NextTick", StringComparison.OrdinalIgnoreCase) &&
            name != nameof(DisabledInHierarchy) &&
            name != nameof(HasChildren);
    }

    private static bool ImplementsOpenGeneric(Type type, Type openGeneric)
    {
        return type
            .GetInterfaces()
            .Any(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == openGeneric);
    }

    private static void ApplyAuthoringDefaults(Type ownerType, ref object instance, string objectName)
    {
        foreach (FieldInfo field in GetAuthoringFields(ownerType))
        {
            if (field.IsInitOnly)
            {
                continue;
            }

            object? value = CreateFieldDefault(ownerType, field, field.GetValue(instance), objectName);
            field.SetValue(instance, value);
        }
    }

    private static IEnumerable<FieldInfo> GetAuthoringFields(Type type)
    {
        return type
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .OrderBy(field => field.MetadataToken);
    }

    private static object? CreateFieldDefault(
        Type ownerType,
        FieldInfo field,
        object? current,
        string objectName)
    {
        Type fieldType = field.FieldType;
        Type? nullableType = Nullable.GetUnderlyingType(fieldType);
        if (nullableType is not null)
        {
            return null;
        }

        if (fieldType == typeof(string))
        {
            return CreateStringDefault(field.Name, objectName);
        }

        if (fieldType == typeof(bool))
        {
            return field.Name.Equals("Value", StringComparison.OrdinalIgnoreCase);
        }

        if (fieldType == typeof(int))
        {
            return CreateIntDefault(field.Name);
        }

        if (fieldType == typeof(Fix))
        {
            return CreateFixDefault(field.Name);
        }

        if (fieldType == typeof(Vector3))
        {
            return CreateVectorDefault(field.Name);
        }

        if (fieldType == typeof(Quaternion))
        {
            return Quaternion.Identity;
        }

        if (fieldType == typeof(PixelColor))
        {
            return ownerType.Name.Contains("Skybox", StringComparison.OrdinalIgnoreCase)
                ? PixelColor.FromRgb(14, 18, 26)
                : PixelColor.FromRgb(188, 196, 202);
        }

        if (fieldType.IsEnum)
        {
            Array values = Enum.GetValues(fieldType);
            return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(fieldType);
        }

        if (fieldType.IsValueType && GetAuthoringFields(fieldType).Any())
        {
            object nested = current ?? Activator.CreateInstance(fieldType)!;
            ApplyAuthoringDefaults(fieldType, ref nested, objectName);
            return nested;
        }

        return current ?? Activator.CreateInstance(fieldType);
    }

    private static string CreateStringDefault(string fieldName, string objectName)
    {
        if (fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase))
        {
            return Guid.NewGuid().ToString("N");
        }

        if (fieldName.Equals("Name", StringComparison.OrdinalIgnoreCase))
        {
            return objectName;
        }

        if (fieldName.Equals("Value", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Text", StringComparison.OrdinalIgnoreCase))
        {
            return "Text";
        }

        return string.Empty;
    }

    private static int CreateIntDefault(string fieldName)
    {
        if (fieldName.Contains("FontSize", StringComparison.OrdinalIgnoreCase))
        {
            return 12;
        }

        if (fieldName is "X" or "Y")
        {
            return 12;
        }

        return 0;
    }

    private static Fix CreateFixDefault(string fieldName)
    {
        if (fieldName.Contains("MinPitch", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.Zero - Fix.Pi * new Fix(85) / new Fix(180);
        }

        if (fieldName.Contains("MaxPitch", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.Pi * new Fix(85) / new Fix(180);
        }

        if (fieldName.Contains("MoveSpeed", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(6);
        }

        if (fieldName.Contains("RotationSpeed", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(2.2);
        }

        if (fieldName.Contains("JumpSpeed", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(5);
        }

        if (fieldName.Contains("GroundProbeDistance", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(0.7);
        }

        if (fieldName.Contains("GroundNormalY", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(0.6);
        }

        if (fieldName.Equals("Height", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(1.75);
        }

        if (fieldName.Contains("StepHeight", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(0.35);
        }

        if (fieldName.Contains("CameraHeightFactor", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(0.95);
        }

        if (fieldName.Contains("HalfHeight", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(9d / 16d);
        }

        if (fieldName.Contains("RecoveryVelocity", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.Two;
        }

        if (fieldName.Contains("Distance", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Max", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(64);
        }

        if (fieldName.Contains("Bias", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(0.02);
        }

        if (fieldName.Contains("Ambient", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(0.2);
        }

        if (fieldName.Contains("Shadow", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(0.8);
        }

        if (fieldName.Contains("Radius", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.FromDouble(0.5);
        }

        if (fieldName.Contains("Length", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Mass", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Friction", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Duration", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Interval", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Opacity", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Tiling", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("ProjectionPlane", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("HalfWidth", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Intensity", StringComparison.OrdinalIgnoreCase))
        {
            return Fix.One;
        }

        return Fix.Zero;
    }

    private static Vector3 CreateVectorDefault(string fieldName)
    {
        if (fieldName.Contains("Scale", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("Size", StringComparison.OrdinalIgnoreCase))
        {
            return Vector3.One;
        }

        if (fieldName.Contains("Axis", StringComparison.OrdinalIgnoreCase))
        {
            return Vector3.Up;
        }

        return Vector3.Zero;
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}

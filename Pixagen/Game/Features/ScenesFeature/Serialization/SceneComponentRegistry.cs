using System.Reflection;
using Pixagen.Game.Features.ScenesFeature.Components;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.ScenesFeature.Serialization;

internal static class SceneComponentRegistry
{
    private static readonly Lazy<Dictionary<string, Type>> TypeIndex = new(BuildTypeIndex);
    private static readonly MethodInfo CreateWriterMethod = typeof(SceneComponentRegistry)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
        .Single(method =>
            method.Name == nameof(CreateWriterGeneric) &&
            method.IsGenericMethodDefinition &&
            method.GetParameters().Length == 1);

    public static Type ResolveType(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Scene component type name is empty.");
        }

        Type? runtimeType = Type.GetType(name, throwOnError: false);
        if (IsSerializableComponent(runtimeType))
        {
            return runtimeType!;
        }

        if (TypeIndex.Value.TryGetValue(name, out Type? type) ||
            TypeIndex.Value.TryGetValue(Normalize(name), out type))
        {
            return type;
        }

        throw new InvalidOperationException($"Unknown scene component type '{name}'.");
    }

    public static string ResolveName(Type type)
    {
        if (!IsSerializableComponent(type))
        {
            throw new InvalidOperationException($"Scene component type '{type.FullName}' is not serializable.");
        }

        return type.FullName ?? type.Name;
    }

    public static Action<Entity, IComponent> CreateWriter(IWorld world, Type componentType)
    {
        if (!IsSerializableComponent(componentType))
        {
            throw new InvalidOperationException($"Scene component type '{componentType.FullName}' is not serializable.");
        }

        return (Action<Entity, IComponent>)CreateWriterMethod
            .MakeGenericMethod(componentType)
            .Invoke(null, [world])!;
    }

    private static Action<Entity, IComponent> CreateWriterGeneric<T>(IWorld world)
        where T : struct, IComponent
    {
        var components = new ComponentInject<T>(world);
        return (entity, component) => components.Replace(entity, (T)component);
    }

    private static Dictionary<string, Type> BuildTypeIndex()
    {
        var index = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (!IsSerializableComponent(type))
                {
                    continue;
                }

                AddTypeName(index, type.FullName, type);
                AddTypeName(index, type.AssemblyQualifiedName, type);
                AddTypeName(index, type.Name, type);
            }
        }

        return index;
    }

    private static void AddTypeName(Dictionary<string, Type> index, string? name, Type type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        index[name] = type;
        index[Normalize(name)] = type;
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

    private static bool IsSerializableComponent(Type? type)
    {
        return type is not null &&
            type.IsValueType &&
            !type.IsAbstract &&
            !type.IsGenericTypeDefinition &&
            type != typeof(SceneObject) &&
            typeof(IComponent).IsAssignableFrom(type);
    }

    private static string Normalize(string value)
    {
        return value
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}

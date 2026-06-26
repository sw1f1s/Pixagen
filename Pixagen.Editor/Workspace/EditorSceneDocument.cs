using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.ScenesFeature.Serialization;
using Pixagen.Game.Features.SharedFeature.Components;

namespace Pixagen.Editor.Workspace;

public sealed class EditorSceneDocument
{
    private readonly List<EditorSceneNode> _flatNodes = new();

    public EditorSceneDocument(string path, SceneDefinition scene)
    {
        Path = path;
        Scene = scene;
        RebuildIndex();
    }

    public string Path { get; }
    public SceneDefinition Scene { get; }
    public IReadOnlyList<EditorSceneNode> FlatNodes => _flatNodes;

    public EditorSceneNode CreateEmptyObject(EditorSceneNode? parent = null)
    {
        string name = $"GameObject {_flatNodes.Count + 1}";
        Transform transform = parent is not null && parent.TryGetComponent(out Transform parentTransform)
            ? parentTransform
            : new Transform(Vector3.Zero);
        var sceneObject = new SceneObjectDefinition
        {
            Components =
            [
                Info.Create(name),
                transform
            ]
        };

        if (parent is null)
        {
            Scene.Objects.Add(sceneObject);
        }
        else
        {
            sceneObject.Components.Add(new LocalTransform(Vector3.Zero));
            parent.Object.Children.Add(sceneObject);
        }

        RebuildIndex();
        return _flatNodes.First(node => ReferenceEquals(node.Object, sceneObject));
    }

    public bool EnsureTransform(EditorSceneNode node)
    {
        if (node.HasComponent<Transform>())
        {
            return false;
        }

        node.Object.Components.Add(new Transform(Vector3.Zero));
        return true;
    }

    public bool EnsureMesh(EditorSceneNode node, string asset = "cube.obj")
    {
        if (node.HasComponent<Mesh>())
        {
            return false;
        }

        node.Object.Components.Add(new Mesh(asset));
        return true;
    }

    public bool EnsureMaterial(EditorSceneNode node)
    {
        if (node.HasComponent<Material>())
        {
            return false;
        }

        node.Object.Components.Add(new Material(PixelColor.FromRgb(188, 196, 202)));
        return true;
    }

    public bool HasComponent(EditorSceneNode node, Type type)
    {
        return node.Object.Components.Any(component => component.GetType() == type);
    }

    public bool AddComponent(EditorSceneNode node, IComponent component)
    {
        if (HasComponent(node, component.GetType()))
        {
            return false;
        }

        node.Object.Components.Add(component);
        return true;
    }

    public bool RemoveComponent(EditorSceneNode node, int componentIndex)
    {
        if ((uint)componentIndex >= (uint)node.Object.Components.Count)
        {
            return false;
        }

        node.Object.Components.RemoveAt(componentIndex);
        return true;
    }

    public bool ReplaceComponent(EditorSceneNode node, int componentIndex, IComponent component)
    {
        if ((uint)componentIndex >= (uint)node.Object.Components.Count)
        {
            return false;
        }

        node.Object.Components[componentIndex] = component;
        return true;
    }

    public void RebuildIndex()
    {
        _flatNodes.Clear();
        for (int i = 0; i < Scene.Objects.Count; i++)
        {
            AddNode(Scene.Objects[i], null, i, 0, i.ToString());
        }
    }

    private EditorSceneNode AddNode(
        SceneObjectDefinition sceneObject,
        EditorSceneNode? parent,
        int siblingIndex,
        int depth,
        string path)
    {
        var node = new EditorSceneNode(sceneObject, parent, siblingIndex, depth, path);
        _flatNodes.Add(node);

        for (int i = 0; i < sceneObject.Children.Count; i++)
        {
            node.Children.Add(AddNode(sceneObject.Children[i], node, i, depth + 1, $"{path}/{i}"));
        }

        return node;
    }
}

public sealed class EditorSceneNode
{
    public EditorSceneNode(
        SceneObjectDefinition sceneObject,
        EditorSceneNode? parent,
        int siblingIndex,
        int depth,
        string path)
    {
        Object = sceneObject;
        Parent = parent;
        SiblingIndex = siblingIndex;
        Depth = depth;
        Path = path;
    }

    public SceneObjectDefinition Object { get; }
    public EditorSceneNode? Parent { get; }
    public int SiblingIndex { get; }
    public int Depth { get; }
    public string Path { get; }
    public List<EditorSceneNode> Children { get; } = new();
    public string StableId => TryGetComponent(out Info info) && !string.IsNullOrWhiteSpace(info.Id) ? info.Id : Path;

    public string DisplayName
    {
        get
        {
            if (TryGetComponent(out Info info) && !string.IsNullOrWhiteSpace(info.Name))
            {
                return info.Name;
            }

            if (TryGetComponent(out Mesh mesh) && !string.IsNullOrWhiteSpace(mesh.Asset))
            {
                return System.IO.Path.GetFileNameWithoutExtension(mesh.Asset);
            }

            return $"Scene Object {Path}";
        }
    }

    public bool HasComponent<T>()
        where T : struct, IComponent
    {
        return Object.Components.Any(component => component is T);
    }

    public bool TryGetComponent<T>(out T value)
        where T : struct, IComponent
    {
        foreach (IComponent component in Object.Components)
        {
            if (component is T typed)
            {
                value = typed;
                return true;
            }
        }

        value = default;
        return false;
    }
}

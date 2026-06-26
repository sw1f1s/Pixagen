using Pixagen.Game.Features.ScenesFeature.Serialization;
using Pixagen.Editor.Preview;
using Pixagen.Game.Features.FreeCameraFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;

namespace Pixagen.Editor.Workspace;

public sealed class EditorWorkspace
{
    private readonly SceneAssetStore _sceneStore = new();

    private EditorWorkspace(
        string contentRoot,
        string scenePath,
        EditorSceneDocument scene,
        AssetDatabase assets)
    {
        ContentRoot = contentRoot;
        ScenePath = scenePath;
        Scene = scene;
        Assets = assets;
        Status = $"Loaded {System.IO.Path.GetFileName(scenePath)}";
    }

    public string ContentRoot { get; }
    public string ScenePath { get; }
    public EditorSceneDocument Scene { get; }
    public AssetDatabase Assets { get; }
    public EditorSelection Selection { get; } = new();
    public string Status { get; private set; }

    public static EditorWorkspace Load(string[] args)
    {
        string contentRoot = EditorContentPaths.ResolveContentRoot();
        string scenePath = ResolveScenePath(contentRoot, args);
        var store = new SceneAssetStore();
        SceneDefinition scene = File.Exists(scenePath)
            ? store.Load(scenePath)
            : DefaultSceneFactory.Create();

        var document = new EditorSceneDocument(scenePath, scene);
        var assets = new AssetDatabase(contentRoot);
        var workspace = new EditorWorkspace(contentRoot, scenePath, document, assets);
        if (document.FlatNodes.Count > 0)
        {
            workspace.Selection.SelectSceneObject(document.FlatNodes[0]);
        }

        return workspace;
    }

    public void SelectSceneObject(EditorSceneNode node)
    {
        Selection.SelectSceneObject(node);
        Status = $"Selected {node.DisplayName}";
    }

    public void SelectAsset(EditorAssetEntry asset)
    {
        Selection.SelectAsset(asset);
        Status = $"Selected {asset.RelativePath}";
    }

    public EditorSceneNode CreateEmptyObject(EditorSceneNode? parent = null)
    {
        EditorSceneNode node = Scene.CreateEmptyObject(parent);
        SelectSceneObject(node);
        Status = $"Created {node.DisplayName}";
        return node;
    }

    public void RefreshAssets()
    {
        EditorAssetEntry? selectedAsset = Selection.Asset;
        Assets.Refresh();
        if (selectedAsset is not null)
        {
            EditorAssetEntry? remapped = Assets.Assets.FirstOrDefault(asset =>
                string.Equals(asset.FullPath, selectedAsset.FullPath, StringComparison.OrdinalIgnoreCase));
            if (remapped is not null)
            {
                Selection.SelectAsset(remapped);
            }
            else if (Selection.Kind == EditorSelectionKind.Asset)
            {
                Selection.Clear();
            }
        }

        Status = $"Assets refreshed: {Assets.Assets.Count}";
    }

    public void SaveScene()
    {
        _sceneStore.Save(ScenePath, Scene.Scene);
        Status = $"Saved {System.IO.Path.GetFileName(ScenePath)}";
    }

    public string SavePlaySceneSnapshot()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"pixagen-play-{Guid.NewGuid():N}.scene.json");
        _sceneStore.Save(path, Scene.Scene);
        Status = $"Prepared Game scene snapshot";
        return path;
    }

    public string SavePreviewSceneSnapshot(bool updateStatus = true)
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"pixagen-scene-preview-{Guid.NewGuid():N}.scene.json");
        _sceneStore.Save(path, CreatePreviewScene());
        if (updateStatus)
        {
            Status = $"Prepared Scene preview";
        }

        return path;
    }

    public PreviewOverlayState CreateOverlayState()
    {
        return Selection.Kind switch
        {
            EditorSelectionKind.SceneObject when Selection.SceneNode is { } node => new PreviewOverlayState
            {
                SelectionKind = "SceneObject",
                SelectedName = node.DisplayName,
                Transform = FormatNodeTransform(node),
                ContentRoot = ContentRoot
            },
            EditorSelectionKind.Asset when Selection.Asset is { } asset => new PreviewOverlayState
            {
                SelectionKind = "Asset",
                SelectedName = asset.Name,
                Transform = asset.RelativePath,
                ContentRoot = ContentRoot
            },
            _ => new PreviewOverlayState
            {
                SelectedName = "None",
                SelectionKind = "None",
                ContentRoot = ContentRoot
            }
        };
    }

    public void SetStatus(string status)
    {
        Status = status;
    }

    private static string ResolveScenePath(string contentRoot, IReadOnlyList<string> args)
    {
        string? sceneArg = ParseSceneArgument(args);
        if (!string.IsNullOrWhiteSpace(sceneArg))
        {
            string path = sceneArg;
            return System.IO.Path.IsPathRooted(path)
                ? System.IO.Path.GetFullPath(path)
                : System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.CurrentDirectory, path));
        }

        return System.IO.Path.Combine(contentRoot, "Scenes", "default.scene.json");
    }

    private static string? ParseSceneArgument(IReadOnlyList<string> args)
    {
        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            if (arg.StartsWith("--scene=", StringComparison.OrdinalIgnoreCase))
            {
                return arg["--scene=".Length..];
            }

            if (string.Equals(arg, "--scene", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
            {
                return args[i + 1];
            }
        }

        return args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
    }

    private SceneDefinition CreatePreviewScene()
    {
        var objects = new List<SceneObjectDefinition>(Scene.Scene.Objects.Count + 1)
        {
            CreatePreviewCamera()
        };
        objects.AddRange(Scene.Scene.Objects);

        return new SceneDefinition
        {
            Version = Scene.Scene.Version,
            Id = Scene.Scene.Id + "-editor-preview",
            Name = Scene.Scene.Name + " (Editor Preview)",
            Objects = objects
        };
    }

    private static SceneObjectDefinition CreatePreviewCamera()
    {
        return new SceneObjectDefinition
        {
            Components =
            [
                new Info("editor-preview-camera", "Editor Preview Camera"),
                new Transform(
                    new Vector3(Fix.Zero, Fix.FromDouble(4), Fix.FromDouble(-18)),
                    Quaternion.FromDirection(new Vector3(Fix.Zero, Fix.FromDouble(-0.1), Fix.One).Normalized),
                    Vector3.One),
                new Velocity(),
                new Camera(Fix.One, Fix.One, Fix.FromDouble(9) / Fix.FromDouble(16), Fix.FromDouble(128)),
                new FreeCamera(Fix.FromDouble(8), Fix.FromDouble(2.2))
            ]
        };
    }

    private static string FormatNodeTransform(EditorSceneNode node)
    {
        if (!node.TryGetComponent(out Transform transform))
        {
            return "Transform: <none>";
        }

        return $"POS {(double)transform.Position.X:0.##}, {(double)transform.Position.Y:0.##}, {(double)transform.Position.Z:0.##} | " +
            $"SCALE {(double)transform.Scale.X:0.##}, {(double)transform.Scale.Y:0.##}, {(double)transform.Scale.Z:0.##}";
    }
}

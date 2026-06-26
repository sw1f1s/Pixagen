namespace Pixagen.Editor.Workspace;

public enum EditorSelectionKind
{
    None,
    SceneObject,
    Asset
}

public sealed class EditorSelection
{
    public EditorSelectionKind Kind { get; private set; }
    public EditorSceneNode? SceneNode { get; private set; }
    public EditorAssetEntry? Asset { get; private set; }

    public void SelectSceneObject(EditorSceneNode node)
    {
        Kind = EditorSelectionKind.SceneObject;
        SceneNode = node;
        Asset = null;
    }

    public void SelectAsset(EditorAssetEntry asset)
    {
        Kind = EditorSelectionKind.Asset;
        Asset = asset;
        SceneNode = null;
    }

    public void Clear()
    {
        Kind = EditorSelectionKind.None;
        SceneNode = null;
        Asset = null;
    }
}

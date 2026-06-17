namespace Pixagen.Rendering;

public interface IRenderInputSink
{
    void BeginFrame();
    void SetKey(RenderInputKey key, bool isDown);
    void AddMouseDelta(float deltaX, float deltaY);
    void RequestExit();
}

public enum RenderInputKey
{
    W,
    A,
    S,
    D,
    C,
    Space,
    Escape,
    Up,
    Down,
    Left,
    Right
}

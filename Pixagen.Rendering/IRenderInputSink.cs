namespace Pixagen.Rendering;

public interface IRenderInputSink
{
    void BeginFrame();
    void SetKey(RenderInputKey key, bool isDown);
    void SetMousePosition(float x, float y);
    void SetMouseButton(RenderMouseButton button, bool isDown);
    void AddMouseDelta(float deltaX, float deltaY);
    void AddMouseWheelDelta(float delta);
    void RequestExit();
}

public enum RenderInputKey
{
    W,
    A,
    S,
    D,
    Q,
    E,
    C,
    Space,
    LeftShift,
    RightShift,
    Escape,
    Up,
    Down,
    Left,
    Right
}

public enum RenderMouseButton
{
    Left,
    Right,
    Middle
}

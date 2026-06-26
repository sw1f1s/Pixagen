namespace Pixagen.Core.Input;

public sealed class InputState : IRenderInputSink
{
    private readonly HashSet<InputKey> _downKeys = new();
    private readonly HashSet<InputKey> _pressedThisFrame = new();
    private readonly HashSet<InputMouseButton> _downMouseButtons = new();
    private readonly HashSet<InputMouseButton> _pressedMouseButtonsThisFrame = new();

    public bool ExitRequested { get; private set; }
    public int MouseDeltaX { get; private set; }
    public int MouseDeltaY { get; private set; }
    public int MouseWheelDelta { get; private set; }
    public int MouseX { get; private set; }
    public int MouseY { get; private set; }

    public bool WasPressed(InputKey key) => _pressedThisFrame.Contains(key);
    public bool IsDown(InputKey key) => _downKeys.Contains(key);
    public bool WasPressed(InputMouseButton button) => _pressedMouseButtonsThisFrame.Contains(button);
    public bool IsDown(InputMouseButton button) => _downMouseButtons.Contains(button);

    public void BeginFrame()
    {
        _pressedThisFrame.Clear();
        _pressedMouseButtonsThisFrame.Clear();
        MouseDeltaX = 0;
        MouseDeltaY = 0;
        MouseWheelDelta = 0;
    }

    public void SetKey(InputKey key, bool isDown)
    {
        if (isDown)
        {
            if (_downKeys.Add(key))
            {
                _pressedThisFrame.Add(key);
            }

            if (key == InputKey.Escape)
            {
                ExitRequested = true;
            }

            return;
        }

        _downKeys.Remove(key);
    }

    public void SetKey(RenderInputKey key, bool isDown)
    {
        SetKey((InputKey)key, isDown);
    }

    public void SetMousePosition(float x, float y)
    {
        MouseX = Math.Max(0, (int)MathF.Round(x));
        MouseY = Math.Max(0, (int)MathF.Round(y));
    }

    public void SetMouseButton(InputMouseButton button, bool isDown)
    {
        if (isDown)
        {
            if (_downMouseButtons.Add(button))
            {
                _pressedMouseButtonsThisFrame.Add(button);
            }

            return;
        }

        _downMouseButtons.Remove(button);
    }

    public void SetMouseButton(RenderMouseButton button, bool isDown)
    {
        SetMouseButton((InputMouseButton)button, isDown);
    }

    public void AddMouseDelta(float deltaX, float deltaY)
    {
        MouseDeltaX += (int)MathF.Round(deltaX);
        MouseDeltaY += (int)MathF.Round(deltaY);
    }

    public void AddMouseWheelDelta(float delta)
    {
        MouseWheelDelta += (int)MathF.Round(delta);
    }

    public void RequestExit()
    {
        ExitRequested = true;
    }

    public void Poll()
    {
        // Window backends feed input before ECS update. Kept as a no-op while systems still call Poll().
    }
}

namespace Pixagen.Core.Input;

public sealed class InputState
{
    private readonly HashSet<InputKey> _downKeys = new();
    private readonly HashSet<InputKey> _pressedThisFrame = new();

    public bool ExitRequested { get; private set; }
    public int MouseDeltaX { get; private set; }
    public int MouseDeltaY { get; private set; }

    public bool WasPressed(InputKey key) => _pressedThisFrame.Contains(key);
    public bool IsDown(InputKey key) => _downKeys.Contains(key);

    public void BeginFrame()
    {
        _pressedThisFrame.Clear();
        MouseDeltaX = 0;
        MouseDeltaY = 0;
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

    public void AddMouseDelta(float deltaX, float deltaY)
    {
        MouseDeltaX += (int)MathF.Round(deltaX);
        MouseDeltaY += (int)MathF.Round(deltaY);
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

using Veldrid;

namespace Pixagen.Rendering.Vulkan;

internal static class VulkanInputMapper
{
    public static bool TryMapKey(Key key, out InputKey inputKey)
    {
        inputKey = key switch
        {
            Key.W => InputKey.W,
            Key.A => InputKey.A,
            Key.S => InputKey.S,
            Key.D => InputKey.D,
            Key.C => InputKey.C,
            Key.Space => InputKey.Space,
            Key.Escape => InputKey.Escape,
            Key.Up => InputKey.Up,
            Key.Down => InputKey.Down,
            Key.Left => InputKey.Left,
            Key.Right => InputKey.Right,
            _ => default
        };

        return key is Key.W or Key.A or Key.S or Key.D or Key.C or Key.Space or Key.Escape or
            Key.Up or Key.Down or Key.Left or Key.Right;
    }
}

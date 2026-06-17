using Veldrid;

namespace Pixagen.Rendering.Vulkan;

internal static class VulkanInputMapper
{
    public static bool TryMapKey(Key key, out RenderInputKey inputKey)
    {
        inputKey = key switch
        {
            Key.W => RenderInputKey.W,
            Key.A => RenderInputKey.A,
            Key.S => RenderInputKey.S,
            Key.D => RenderInputKey.D,
            Key.C => RenderInputKey.C,
            Key.Space => RenderInputKey.Space,
            Key.Escape => RenderInputKey.Escape,
            Key.Up => RenderInputKey.Up,
            Key.Down => RenderInputKey.Down,
            Key.Left => RenderInputKey.Left,
            Key.Right => RenderInputKey.Right,
            _ => default
        };

        return key is Key.W or Key.A or Key.S or Key.D or Key.C or Key.Space or Key.Escape or
            Key.Up or Key.Down or Key.Left or Key.Right;
    }
}

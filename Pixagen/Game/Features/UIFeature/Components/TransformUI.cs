
namespace Pixagen.Game.Features.UIFeature.Components;

public struct TransformUI : IComponent
{
    public int X;
    public int Y;
    public int Order;

    public TransformUI(int x, int y, int order = 0)
    {
        X = x;
        Y = y;
        Order = order;
    }
}

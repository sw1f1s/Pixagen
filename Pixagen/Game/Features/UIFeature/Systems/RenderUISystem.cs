using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Game.Features.UIFeature.Components;
using Pixagen.Rendering;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.UIFeature.Systems;

public sealed class RenderUISystem : IUpdateSystem
{
    private readonly List<Entity> _textEntities = new();

    private readonly CustomInject<RenderBackendOptions> _backendOptions = default;
    private readonly CustomInject<UiOverlayBuffer> _uiOverlay = default;
    private readonly FilterInject<Include<TransformUI, TextUI>> _texts = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<TransformUI> _transforms = default;
    private readonly ComponentInject<TextUI> _textComponents = default;

    public void Update()
    {
        UiOverlayBuffer overlay = _uiOverlay.Value;
        overlay.Clear();
        _textEntities.Clear();

        foreach (Entity entity in _texts.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            _textEntities.Add(entity);
        }

        _textEntities.Sort(CompareTextEntities);

        foreach (Entity entity in _textEntities)
        {
            ref TransformUI transform = ref _transforms.Get(entity);
            ref TextUI text = ref _textComponents.Get(entity);
            AddText(overlay, transform, text, Math.Max(1, _backendOptions.Value.CellPixelSize));
        }
    }

    private int CompareTextEntities(Entity left, Entity right)
    {
        ref TransformUI leftTransform = ref _transforms.Get(left);
        ref TransformUI rightTransform = ref _transforms.Get(right);

        int order = leftTransform.Order.CompareTo(rightTransform.Order);
        if (order != 0)
        {
            return order;
        }

        int y = leftTransform.Y.CompareTo(rightTransform.Y);
        return y != 0 ? y : leftTransform.X.CompareTo(rightTransform.X);
    }

    private static void AddText(UiOverlayBuffer overlay, TransformUI transform, TextUI text, int baseFontSize)
    {
        if (string.IsNullOrEmpty(text.Value))
        {
            return;
        }

        int fontSize = ResolveFontSize(text, baseFontSize);
        overlay.AddText(new UiTextDrawCommand(
            transform.X * baseFontSize,
            transform.Y * baseFontSize,
            transform.Order,
            text.Value,
            text.Color,
            fontSize));
    }

    private static int ResolveFontSize(TextUI text, int baseFontSize)
    {
        return text.FontSize > 0 ? text.FontSize : baseFontSize;
    }
}

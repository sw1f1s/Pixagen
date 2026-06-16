using Pixagen.Ecs.Runtime;

namespace Pixagen.Game.Features.SharedFeature.Components;

public struct Parent : IComponent
{
    public Entity Entity;

    public Parent(Entity entity)
    {
        Entity = entity;
    }
}

using System;

namespace Pixagen.Game.Features.SharedFeature.Components;

public struct Info : IComponent
{
    public string Id;
    public string Name;

    public Info(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public static Info Create(string name = "")
    {
        return new Info(Guid.NewGuid().ToString("N"), name);
    }
}

namespace Pixagen.Game.Features.ResourceFeature.Runtime;

internal readonly record struct ResourceLoadResult<T>(T Resource, bool Inserted);

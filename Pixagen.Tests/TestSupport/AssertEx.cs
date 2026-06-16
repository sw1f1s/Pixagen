using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.TestSupport;

public static class AssertEx
{
    public static void Equal(Vector3 expected, Vector3 actual)
    {
        Assert.Equal(expected.X, actual.X);
        Assert.Equal(expected.Y, actual.Y);
        Assert.Equal(expected.Z, actual.Z);
    }

    public static void Alive(Entity entity)
    {
        Assert.NotEqual(Entity.Empty, entity);
        Assert.True(Access(entity).IsAlive());
    }

    public static void Dead(Entity entity)
    {
        Assert.False(Access(entity).IsAlive());
    }
}

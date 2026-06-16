namespace Pixagen.Benchmark;

public static class BenchmarkMath
{
    public static Vector3 GridPosition(int index, int entityCount, Fix spacing, Fix y, Fix zOffset)
    {
        int width = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(entityCount)));
        int x = index % width;
        int z = index / width;
        Fix centeredX = new Fix(x - width / 2) * spacing;
        Fix centeredZ = new Fix(z) * spacing + zOffset;
        return new Vector3(centeredX, y, centeredZ);
    }

    public static byte Channel(int index, int salt)
    {
        return (byte)(64 + Math.Abs((index * 31 + salt * 97) % 160));
    }
}

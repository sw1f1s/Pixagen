namespace Pixagen.Rendering.Textures;

public sealed class TextureAsset
{
    public TextureAsset(string id, int width, int height, TexturePixel[] pixels)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (pixels.Length != width * height)
        {
            throw new ArgumentException("Texture pixel count must match width * height.", nameof(pixels));
        }

        Id = id;
        Width = width;
        Height = height;
        Pixels = pixels;
        MipLevels = BuildMipLevels(width, height, pixels);
        MipPixelCount = MipLevels.Sum(level => level.Pixels.Length);
    }

    public string Id { get; }
    public int Width { get; }
    public int Height { get; }
    public TexturePixel[] Pixels { get; }
    public TextureMipLevel[] MipLevels { get; }
    public int MipCount => MipLevels.Length;
    public int MipPixelCount { get; }

    public TexturePixel Sample(float u, float v, float lod = 0f)
    {
        int level = Math.Clamp((int)MathF.Round(lod), 0, MipLevels.Length - 1);
        TextureMipLevel mip = MipLevels[level];

        u -= MathF.Floor(u);
        v -= MathF.Floor(v);

        int x = Math.Clamp((int)MathF.Floor(u * mip.Width), 0, mip.Width - 1);
        int y = Math.Clamp((int)MathF.Floor((1f - v) * mip.Height), 0, mip.Height - 1);
        return mip.Pixels[y * mip.Width + x];
    }

    private static TextureMipLevel[] BuildMipLevels(int width, int height, TexturePixel[] pixels)
    {
        var levels = new List<TextureMipLevel> { new(width, height, pixels) };
        int currentWidth = width;
        int currentHeight = height;
        TexturePixel[] currentPixels = pixels;

        while (currentWidth > 1 || currentHeight > 1)
        {
            int nextWidth = Math.Max(1, currentWidth / 2);
            int nextHeight = Math.Max(1, currentHeight / 2);
            var nextPixels = new TexturePixel[nextWidth * nextHeight];

            for (int y = 0; y < nextHeight; y++)
            {
                for (int x = 0; x < nextWidth; x++)
                {
                    nextPixels[y * nextWidth + x] = Average2x2(currentPixels, currentWidth, currentHeight, x * 2, y * 2);
                }
            }

            levels.Add(new TextureMipLevel(nextWidth, nextHeight, nextPixels));
            currentWidth = nextWidth;
            currentHeight = nextHeight;
            currentPixels = nextPixels;
        }

        return levels.ToArray();
    }

    private static TexturePixel Average2x2(TexturePixel[] pixels, int width, int height, int x, int y)
    {
        int count = 0;
        int r = 0;
        int g = 0;
        int b = 0;
        int a = 0;

        for (int offsetY = 0; offsetY < 2; offsetY++)
        {
            int sourceY = y + offsetY;
            if (sourceY >= height)
            {
                continue;
            }

            for (int offsetX = 0; offsetX < 2; offsetX++)
            {
                int sourceX = x + offsetX;
                if (sourceX >= width)
                {
                    continue;
                }

                TexturePixel pixel = pixels[sourceY * width + sourceX];
                r += pixel.R;
                g += pixel.G;
                b += pixel.B;
                a += pixel.A;
                count++;
            }
        }

        count = Math.Max(1, count);
        return new TexturePixel(
            (byte)(r / count),
            (byte)(g / count),
            (byte)(b / count),
            (byte)(a / count));
    }
}

public readonly record struct TextureMipLevel(int Width, int Height, TexturePixel[] Pixels);

public readonly record struct TexturePixel(byte R, byte G, byte B, byte A)
{
    public PixelColor Color => PixelColor.FromRgb(R, G, B);
    public float Alpha => A / 255f;
}

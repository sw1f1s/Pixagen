namespace Pixagen.Rendering;

public sealed class FrameBuffer
{
    private FrameCell[] _cells;

    public FrameBuffer(int width, int height)
    {
        ValidateSize(width, height);

        Width = width;
        Height = height;
        _cells = new FrameCell[width * height];
        Clear();
    }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public ReadOnlySpan<FrameCell> Cells => _cells;
    public Span<FrameCell> WritableCells => _cells;
    public FrameCell[] WritableCellsArray => _cells;

    public void Resize(int width, int height)
    {
        ValidateSize(width, height);

        if (width == Width && height == Height)
        {
            return;
        }

        Width = width;
        Height = height;
        _cells = new FrameCell[width * height];
        Clear();
    }

    public void Clear()
    {
        Clear(FrameCell.Empty);
    }

    public void Clear(FrameCell cell)
    {
        Array.Fill(_cells, cell);
    }

    public void Set(int x, int y, FrameCell cell)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return;
        }

        _cells[y * Width + x] = cell;
    }

    public FrameCell Get(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return FrameCell.Empty;
        }

        return _cells[y * Width + x];
    }

    private static void ValidateSize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
    }
}

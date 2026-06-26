namespace Pixagen.Editor.Preview;

public sealed class PreviewOverlayFile
{
    private readonly string _path;
    private DateTime _lastWriteTimeUtc;
    private PreviewOverlayState _state = PreviewOverlayState.Empty;

    public PreviewOverlayFile(string path)
    {
        _path = path;
    }

    public PreviewOverlayState Read()
    {
        if (!File.Exists(_path))
        {
            _state = PreviewOverlayState.Empty;
            _lastWriteTimeUtc = default;
            return _state;
        }

        DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(_path);
        if (lastWriteTimeUtc == _lastWriteTimeUtc)
        {
            return _state;
        }

        _state = PreviewOverlayState.Load(_path);
        _lastWriteTimeUtc = lastWriteTimeUtc;
        return _state;
    }
}

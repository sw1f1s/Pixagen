using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Pixagen.Editor.Preview;
using Pixagen.Editor.Workspace;

namespace Pixagen.Editor.Avalonia;

public sealed class SceneViewportControl : NativeControlHost, IDisposable
{
    private readonly EditorWorkspace _workspace;
    private readonly EmbeddedScenePreviewHost _preview = new();
    private readonly DispatcherTimer _resizeTimer;
    private IPlatformHandle? _nativeControl;
    private Control? _inputLayer;
    private Point _lastPointerPosition;
    private bool _hasPointerPosition;
    private bool _isLookActive;
    private bool _restartPending;
    private bool _disposed;

    public SceneViewportControl(EditorWorkspace workspace)
    {
        _workspace = workspace;
        Focusable = true;
        ClipToBounds = true;
        MinHeight = 240;

        _preview.StatusChanged += SetStatus;

        _resizeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(180)
        };
        _resizeTimer.Tick += (_, _) =>
        {
            _resizeTimer.Stop();
            ResizePreview();
        };

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        PointerExited += OnPointerExited;
    }

    public event Action<string>? StatusChanged;

    public void AttachInputLayer(Control inputLayer)
    {
        if (_inputLayer is not null)
        {
            _inputLayer.KeyDown -= OnKeyDown;
            _inputLayer.KeyUp -= OnKeyUp;
            _inputLayer.PointerMoved -= OnPointerMoved;
            _inputLayer.PointerPressed -= OnPointerPressed;
            _inputLayer.PointerReleased -= OnPointerReleased;
            _inputLayer.PointerWheelChanged -= OnPointerWheelChanged;
            _inputLayer.PointerExited -= OnPointerExited;
        }

        _inputLayer = inputLayer;
        inputLayer.KeyDown += OnKeyDown;
        inputLayer.KeyUp += OnKeyUp;
        inputLayer.PointerMoved += OnPointerMoved;
        inputLayer.PointerPressed += OnPointerPressed;
        inputLayer.PointerReleased += OnPointerReleased;
        inputLayer.PointerWheelChanged += OnPointerWheelChanged;
        inputLayer.PointerExited += OnPointerExited;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        IPlatformHandle handle = base.CreateNativeControlCore(parent);
        _nativeControl = handle;
        RestartPreview();
        return handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        _resizeTimer.Stop();
        _preview.Stop();
        _nativeControl = null;
        base.DestroyNativeControlCore(control);
    }

    public void RestartPreview()
    {
        if (_nativeControl is null || !HasUsableBounds())
        {
            _restartPending = true;
            SetStatus("Scene viewport: waiting for native surface");
            return;
        }

        _restartPending = false;
        (int width, int height) = GetPixelSize();

        bool started = _preview.StartOrRestart(
            _workspace,
            _nativeControl.Handle,
            _nativeControl.HandleDescriptor,
            width,
            height);
        if (started)
        {
            SetStatus("Scene viewport: starting embedded Vulkan");
            return;
        }

        _resizeTimer.Stop();
        SetStatus($"Scene viewport: failed ({_preview.LastError})");
    }

    public void StopPreview()
    {
        _restartPending = false;
        _resizeTimer.Stop();
        _preview.Stop();
        SetStatus("Scene viewport: stopped");
    }

    public void ReloadPreviewScene()
    {
        RestartPreview();
    }

    public void UpdateOverlay(EditorWorkspace workspace)
    {
        if (!_preview.IsRunning)
        {
            RestartPreview();
        }

        _preview.UpdateOverlay(workspace);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (_restartPending && _nativeControl is not null)
        {
            RestartPreview();
            return;
        }

        if (_preview.IsRunning)
        {
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _resizeTimer.Stop();
        _preview.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetStatus(string value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetStatus(value));
            return;
        }

        StatusChanged?.Invoke(value);
    }

    private bool HasUsableBounds()
    {
        return Bounds.Width >= 16 && Bounds.Height >= 16;
    }

    private void ResizePreview()
    {
        if (_nativeControl is null || !HasUsableBounds())
        {
            return;
        }

        (int width, int height) = GetPixelSize();
        _preview.ResizeViewport(width, height);
    }

    private (int Width, int Height) GetPixelSize()
    {
        double scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        return (
            Math.Max(1, (int)Math.Round(Bounds.Width * scale)),
            Math.Max(1, (int)Math.Round(Bounds.Height * scale)));
    }

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (TryMapKey(args.Key, out InputKey key))
        {
            _preview.SetKey(key, isDown: true);
            args.Handled = true;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs args)
    {
        if (TryMapKey(args.Key, out InputKey key))
        {
            _preview.SetKey(key, isDown: false);
            args.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs args)
    {
        Point position = args.GetPosition(this);
        _preview.SetMousePosition((float)position.X, (float)position.Y);
        if (_hasPointerPosition && _isLookActive)
        {
            _preview.AddMouseDelta(
                (float)(position.X - _lastPointerPosition.X),
                (float)(position.Y - _lastPointerPosition.Y));
            args.Handled = true;
        }

        _lastPointerPosition = position;
        _hasPointerPosition = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (sender is Control inputControl)
        {
            inputControl.Focus();
        }
        else
        {
            Focus();
        }

        args.Pointer.Capture(this);
        _lastPointerPosition = args.GetPosition(this);
        _hasPointerPosition = true;

        if (TryMapMouseButton(args.GetCurrentPoint(this).Properties.PointerUpdateKind, out InputMouseButton button))
        {
            _isLookActive = button == InputMouseButton.Right || _isLookActive;
            _preview.SetMouseButton(button, isDown: true);
            args.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        if (TryMapMouseButton(args.GetCurrentPoint(this).Properties.PointerUpdateKind, out InputMouseButton button))
        {
            if (button == InputMouseButton.Right)
            {
                _isLookActive = false;
            }

            _preview.SetMouseButton(button, isDown: false);
            args.Handled = true;
        }

        args.Pointer.Capture(null);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs args)
    {
        _preview.AddMouseWheelDelta((float)args.Delta.Y);
        args.Handled = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs args)
    {
        if (!_isLookActive)
        {
            _hasPointerPosition = false;
        }
    }

    private static bool TryMapKey(Key key, out InputKey inputKey)
    {
        inputKey = key switch
        {
            Key.W => InputKey.W,
            Key.A => InputKey.A,
            Key.S => InputKey.S,
            Key.D => InputKey.D,
            Key.Q => InputKey.Q,
            Key.E => InputKey.E,
            Key.C => InputKey.C,
            Key.Space => InputKey.Space,
            Key.LeftShift => InputKey.LeftShift,
            Key.RightShift => InputKey.RightShift,
            Key.Up => InputKey.Up,
            Key.Down => InputKey.Down,
            Key.Left => InputKey.Left,
            Key.Right => InputKey.Right,
            _ => default
        };

        return key is Key.W or Key.A or Key.S or Key.D or Key.Q or Key.E or Key.C or Key.Space or
            Key.LeftShift or Key.RightShift or Key.Up or Key.Down or Key.Left or Key.Right;
    }

    private static bool TryMapMouseButton(PointerUpdateKind kind, out InputMouseButton button)
    {
        button = kind switch
        {
            PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => InputMouseButton.Left,
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => InputMouseButton.Right,
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => InputMouseButton.Middle,
            _ => default
        };

        return kind is PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased or
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased or
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased;
    }
}

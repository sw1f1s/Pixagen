using Veldrid;

namespace Pixagen.Rendering.Vulkan;

internal sealed class VulkanGpuFrameTracker : IDisposable
{
    private const int MaxGpuFramesInFlight = 2;
    private static readonly TimeSpan GpuFenceTimeout = TimeSpan.FromSeconds(5);

    private readonly PerformanceStats _performanceStats;
    private readonly List<PendingGpuFrame> _pendingGpuFrames = new();
    private long _currentGpuFrameStartTicks;
    private bool _hasCurrentGpuFrame;

    public VulkanGpuFrameTracker(PerformanceStats performanceStats)
    {
        _performanceStats = performanceStats;
    }

    public bool IsStalled { get; private set; }

    public void BeginGpuWork()
    {
        if (_hasCurrentGpuFrame)
        {
            return;
        }

        _currentGpuFrameStartTicks = PerformanceStats.Timestamp;
        _hasCurrentGpuFrame = true;
    }

    public void TrackSubmittedFrame(Fence fence)
    {
        if (!_hasCurrentGpuFrame)
        {
            _currentGpuFrameStartTicks = PerformanceStats.Timestamp;
        }

        _pendingGpuFrames.Add(new PendingGpuFrame(fence, _currentGpuFrameStartTicks));
    }

    public void EndLogicalFrame()
    {
        _hasCurrentGpuFrame = false;
    }

    public void Throttle(GraphicsDevice graphicsDevice)
    {
        PollCompleted();
        while (_pendingGpuFrames.Count >= MaxGpuFramesInFlight)
        {
            WaitForGpuFrame(graphicsDevice, _pendingGpuFrames[0]);
            CompleteGpuFrame(0);
        }
    }

    public void WaitForPending(GraphicsDevice graphicsDevice)
    {
        PollCompleted();
        while (_pendingGpuFrames.Count > 0)
        {
            WaitForGpuFrame(graphicsDevice, _pendingGpuFrames[0]);
            CompleteGpuFrame(0);
        }
    }

    public void PollCompleted()
    {
        for (int i = _pendingGpuFrames.Count - 1; i >= 0; i--)
        {
            PendingGpuFrame pendingFrame = _pendingGpuFrames[i];
            if (!pendingFrame.Fence.Signaled)
            {
                continue;
            }

            CompleteGpuFrame(i);
        }
    }

    public void Dispose()
    {
        foreach (PendingGpuFrame pendingFrame in _pendingGpuFrames)
        {
            pendingFrame.Fence.Dispose();
        }

        _pendingGpuFrames.Clear();
    }

    private void WaitForGpuFrame(GraphicsDevice graphicsDevice, PendingGpuFrame pendingFrame)
    {
        if (graphicsDevice.WaitForFence(pendingFrame.Fence, GpuFenceTimeout))
        {
            return;
        }

        IsStalled = true;
        throw new InvalidOperationException(
            $"GPU frame did not complete within {GpuFenceTimeout.TotalSeconds:0.#} seconds. " +
            "The Vulkan backend will request engine shutdown to avoid a repeating driver stall.");
    }

    private void CompleteGpuFrame(int index)
    {
        PendingGpuFrame pendingFrame = _pendingGpuFrames[index];
        _performanceStats.RecordGpuFrameSince(pendingFrame.StartTicks);
        pendingFrame.Fence.Dispose();
        _pendingGpuFrames.RemoveAt(index);
    }

    private readonly record struct PendingGpuFrame(Fence Fence, long StartTicks);
}

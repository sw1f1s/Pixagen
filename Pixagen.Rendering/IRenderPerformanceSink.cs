namespace Pixagen.Rendering;

public interface IRenderPerformanceSink
{
    long CurrentTimestamp { get; }

    void RecordGpuFrameSince(long startTicks);
    void RecordBackendFrame(int drawCalls, int passes, long vramBytes);
}

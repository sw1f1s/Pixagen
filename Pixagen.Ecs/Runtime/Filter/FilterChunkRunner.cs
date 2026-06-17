using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Pixagen.Ecs.Runtime;

public interface IFilterChunkJob
{
    void Execute(int chunkIndex);
}

public interface IFilterChunkProcessor
{
    void Execute(FilterChunk chunk);
}

public static class FilterChunkRunner
{
    private const int ChunksPerWorker = 2;
    [ThreadStatic]
    private static int _runnerDepth;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run<TJob>(FilterChunks chunks, TJob job, bool allowParallel)
        where TJob : struct, IFilterChunkProcessor
    {
        if (!allowParallel || chunks.EntityCount < Filter.DefaultParallelEntityThreshold)
        {
            RunSequential(chunks, job);
            return;
        }

        Run(chunks.Count, new ChunkProcessorJob<TJob>(chunks, job));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run<TJob>(int chunkCount, TJob job)
        where TJob : struct, IFilterChunkJob
    {
        if (chunkCount <= 0)
        {
            return;
        }

        int workerCount = GetWorkerCount(chunkCount);
        if (_runnerDepth > 0)
        {
            workerCount = 1;
        }

        if (workerCount <= 1)
        {
            RunSequential(chunkCount, job);
            return;
        }

        RunState<TJob> state = Pool<TJob>.RentState();
        state.Reset(chunkCount, workerCount, job);

        PersistentWorkerPool.Queue(state, workerCount - 1);
        state.ExecuteWorker();
        state.Wait();

        Exception? exception = state.Exception;
        Pool<TJob>.ReturnState(state);
        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RunSequential<TJob>(FilterChunks chunks, TJob job)
        where TJob : struct, IFilterChunkProcessor
    {
        for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
        {
            job.Execute(chunks[chunkIndex]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RunSequential<TJob>(int chunkCount, TJob job)
        where TJob : struct, IFilterChunkJob
    {
        for (int i = 0; i < chunkCount; i++)
        {
            job.Execute(i);
        }
    }

    private static void ExecuteWorker<TJob>(RunState<TJob> state)
        where TJob : struct, IFilterChunkJob
    {
        _runnerDepth++;
        try
        {
            while (true)
            {
                int chunkIndex = Interlocked.Increment(ref state.NextChunk) - 1;
                if (chunkIndex >= state.ChunkCount)
                {
                    return;
                }

                state.Job.Execute(chunkIndex);
            }
        }
        catch (Exception exception)
        {
            Interlocked.CompareExchange(ref state.Exception, exception, null);
        }
        finally
        {
            _runnerDepth--;
            if (Interlocked.Decrement(ref state.RemainingWorkers) == 0)
            {
                state.SetComplete();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetWorkerCount(int chunkCount)
    {
        int workerLimitByChunks = Math.Max(1, (chunkCount + ChunksPerWorker - 1) / ChunksPerWorker);
        return Math.Min(chunkCount, Math.Min(Environment.ProcessorCount, workerLimitByChunks));
    }

    private static class Pool<TJob>
        where TJob : struct, IFilterChunkJob
    {
        private static readonly ConcurrentQueue<RunState<TJob>> States = new();

        public static RunState<TJob> RentState()
        {
            return States.TryDequeue(out RunState<TJob>? state) ? state : new RunState<TJob>();
        }

        public static void ReturnState(RunState<TJob> state)
        {
            state.Clear();
            States.Enqueue(state);
        }
    }

    private interface IChunkRunState
    {
        void ExecuteWorker();
    }

    private sealed class RunState<TJob>
        : IChunkRunState
        where TJob : struct, IFilterChunkJob
    {
        private readonly ManualResetEventSlim _complete = new(false);

        public TJob Job;
        public int ChunkCount;
        public int NextChunk;
        public int RemainingWorkers;
        public Exception? Exception;

        public void Reset(int chunkCount, int workerCount, TJob job)
        {
            Job = job;
            ChunkCount = chunkCount;
            NextChunk = 0;
            RemainingWorkers = workerCount;
            Exception = null;
            _complete.Reset();
        }

        public void Wait()
        {
            _complete.Wait();
        }

        public void SetComplete()
        {
            _complete.Set();
        }

        public void ExecuteWorker()
        {
            FilterChunkRunner.ExecuteWorker(this);
        }

        public void Clear()
        {
            Job = default;
            ChunkCount = 0;
            NextChunk = 0;
            RemainingWorkers = 0;
            Exception = null;
        }
    }

    private static class PersistentWorkerPool
    {
        private static readonly ConcurrentQueue<IChunkRunState> QueueItems = new();
        private static readonly SemaphoreSlim Signal = new(0);
        private static int _started;

        public static void Queue(IChunkRunState state, int workerCount)
        {
            if (workerCount <= 0)
            {
                return;
            }

            EnsureStarted();
            for (int i = 0; i < workerCount; i++)
            {
                QueueItems.Enqueue(state);
                Signal.Release();
            }
        }

        private static void EnsureStarted()
        {
            if (Volatile.Read(ref _started) != 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
            {
                return;
            }

            int workerCount = Math.Max(0, Environment.ProcessorCount - 1);
            for (int i = 0; i < workerCount; i++)
            {
                var thread = new Thread(WorkerLoop)
                {
                    IsBackground = true,
                    Name = $"Pixagen Filter Worker {i + 1}"
                };
                thread.Start();
            }
        }

        private static void WorkerLoop()
        {
            while (true)
            {
                Signal.Wait();
                if (QueueItems.TryDequeue(out IChunkRunState? state))
                {
                    state.ExecuteWorker();
                }
            }
        }
    }

    private readonly struct ChunkProcessorJob<TJob> : IFilterChunkJob
        where TJob : struct, IFilterChunkProcessor
    {
        private readonly FilterChunks _chunks;
        private readonly TJob _job;

        public ChunkProcessorJob(FilterChunks chunks, TJob job)
        {
            _chunks = chunks;
            _job = job;
        }

        public void Execute(int chunkIndex)
        {
            _job.Execute(_chunks[chunkIndex]);
        }
    }
}

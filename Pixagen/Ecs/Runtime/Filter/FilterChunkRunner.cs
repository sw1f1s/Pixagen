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

        int workerCount = Math.Min(chunkCount, Environment.ProcessorCount);
        if (workerCount <= 1)
        {
            RunSequential(chunkCount, job);
            return;
        }

        RunState<TJob> state = Pool<TJob>.RentState();
        state.Reset(chunkCount, workerCount, job);

        for (int i = 1; i < workerCount; i++)
        {
            Worker<TJob> worker = Pool<TJob>.RentWorker();
            worker.State = state;
            if (!ThreadPool.UnsafeQueueUserWorkItem(static queuedWorker => queuedWorker.Execute(), worker, false))
            {
                worker.Execute();
            }
        }

        ExecuteWorker(state);
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
            if (Interlocked.Decrement(ref state.RemainingWorkers) == 0)
            {
                state.SetComplete();
            }
        }
    }

    private static class Pool<TJob>
        where TJob : struct, IFilterChunkJob
    {
        private static readonly ConcurrentQueue<RunState<TJob>> States = new();
        private static readonly ConcurrentQueue<Worker<TJob>> Workers = new();

        public static RunState<TJob> RentState()
        {
            return States.TryDequeue(out RunState<TJob>? state) ? state : new RunState<TJob>();
        }

        public static void ReturnState(RunState<TJob> state)
        {
            state.Clear();
            States.Enqueue(state);
        }

        public static Worker<TJob> RentWorker()
        {
            return Workers.TryDequeue(out Worker<TJob>? worker) ? worker : new Worker<TJob>();
        }

        public static void ReturnWorker(Worker<TJob> worker)
        {
            Workers.Enqueue(worker);
        }
    }

    private sealed class RunState<TJob>
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

        public void Clear()
        {
            Job = default;
            ChunkCount = 0;
            NextChunk = 0;
            RemainingWorkers = 0;
            Exception = null;
        }
    }

    private sealed class Worker<TJob>
        where TJob : struct, IFilterChunkJob
    {
        public RunState<TJob>? State;

        public void Execute()
        {
            RunState<TJob> state = State ?? throw new InvalidOperationException("Chunk worker state was not assigned.");
            State = null;
            try
            {
                ExecuteWorker(state);
            }
            finally
            {
                Pool<TJob>.ReturnWorker(this);
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

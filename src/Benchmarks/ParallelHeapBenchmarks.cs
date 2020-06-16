﻿using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace Benchmarks
{
    public class ParallelHeapBenchmarks
    {
        private DataTarget _dataTarget;
        private ClrRuntime _runtime;
        private ThreadParallelRunner<ClrSegment> _runner;

        [Params(
            32 * 1024 * 1024,
            128 * 1024 * 1024,
            1024 * 1024 * 1024)]
        public int CacheSize { get; set; }

        [ParamsSource(nameof(OSMemoryFeaturesSource))]
        public bool UseOSMemoryFeatures { get; set; }

        public IEnumerable<bool> OSMemoryFeaturesSource
        {
            get
            {
                yield return false;
                if (Program.ShouldTestOSMemoryFeatures)
                    yield return true;
            }
        }

        [Params(1, 4, 8, 12)]
        public int Threads { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            CacheOptions options = new CacheOptions()
            {
                CacheFields = true,
                CacheMethods = true,
                CacheTypes = true,

                CacheFieldNames = StringCaching.Cache,
                CacheMethodNames = StringCaching.Cache,
                CacheTypeNames = StringCaching.Cache,

                MaxDumpCacheSize = CacheSize,
                UseOSMemoryFeatures = UseOSMemoryFeatures,
            };

            _dataTarget = DataTarget.LoadDump(Program.CrashDump, options);
            _runtime = _dataTarget.ClrVersions.Single().CreateRuntime();
        }

        [IterationSetup]
        public void InitRunner()
        {
            _runner = new ThreadParallelRunner<ClrSegment>(Threads, _runtime.Heap.Segments);
            _runner.Setup();
        }

        [IterationCleanup]
        public void ClearCached()
        {
            _runtime.FlushCachedData();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _runtime.Dispose();
            _dataTarget?.Dispose();
        }

        [Benchmark]
        public void ParallelEnumerateHeapWithReferences()
        {
            _runner.Run(WalkSegment);
        }

        private static void WalkSegment(ClrSegment seg)
        {
            foreach (ClrObject obj in seg.EnumerateObjects().Take(2048))
            {
                foreach (ClrReference reference in obj.EnumerateReferencesWithFields(carefully: false, considerDependantHandles: true))
                {
                    _ = reference.Object;
                }
            }
        }
    }
}

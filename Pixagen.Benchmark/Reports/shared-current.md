# Pixagen Benchmark

- Frames: 16
- Warmup: 4
- Render: 160x90

## Legend

- `setup`: scenario creation, resource loading, ECS entity/component creation, and system init.
- `mean/p95/worst`: measured frame time after warmup.
- `alloc/f`: managed bytes allocated per measured frame.
- `managed`: managed heap observed after measurement.
- `working`: process working set observed after measurement.
- `cpu`: process CPU time divided by wall time and logical CPU count.
- `gc`: Gen0/Gen1/Gen2 collections during measured frames.
- `counters`: scenario-specific work counters, printed below each timing table.

## shared.systems

Movement, rotation, lerp, hierarchy, enable/disable triggers, destroy, and one-tick cleanup.

| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 100,000 | 192.181 ms / 180.04 MB | 120.665 ms | 122.35 ms | 122.35 ms | 8.3 | 54.28 KB | 131.7 MB | 187 MB | 10.3% | 0/0/0 |

Counters:
- `100,000`: hierarchyPairs=20000; toggleCandidates=12500


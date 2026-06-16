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

## ecs.storage

Entity create/setup plus hot filter iteration, component get/replace, and sparse storage access.

| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 56.312 ms / 5.21 MB | 0.004 ms | 0.007 ms | 0.007 ms | 264633.4 | 189 B | 5.8 MB | 46.3 MB | 153.9% | 0/0/0 |
| 100 | 0.705 ms / 5.21 MB | 0.036 ms | 0.041 ms | 0.041 ms | 27442.3 | 210 B | 5.9 MB | 47.7 MB | 11.5% | 0/0/0 |
| 1,000 | 1.138 ms / 5.17 MB | 0.465 ms | 3.148 ms | 3.148 ms | 2151.6 | 210 B | 5.9 MB | 53 MB | 10.1% | 0/0/0 |
| 10,000 | 14.794 ms / 17.43 MB | 1.741 ms | 1.973 ms | 1.973 ms | 574.5 | 189 B | 14.8 MB | 67.5 MB | 10% | 0/0/0 |
| 100,000 | 44.687 ms / 109.12 MB | 2.206 ms | 2.333 ms | 2.333 ms | 453.3 | 200.5 B | 66 MB | 131.1 MB | 10.8% | 0/0/0 |

Counters:
- `10`: movingFilter=9; materialFilter=5; checksum=1180
- `100`: movingFilter=90; materialFilter=50; checksum=91900
- `1,000`: movingFilter=909; materialFilter=500; checksum=9117280
- `10,000`: movingFilter=9090; materialFilter=5000; checksum=909190900
- `100,000`: movingFilter=90909; materialFilter=50000; checksum=90911727280

## shared.systems

Movement, rotation, lerp, hierarchy, enable/disable triggers, destroy, and one-tick cleanup.

| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 9.599 ms / 5.47 MB | 0.085 ms | 0.119 ms | 0.119 ms | 11764.7 | 1.19 KB | 6.6 MB | 61.2 MB | 11.1% | 0/0/0 |
| 100 | 0.98 ms / 5.48 MB | 0.761 ms | 1.539 ms | 1.539 ms | 1314.3 | 1.21 KB | 6.6 MB | 61.5 MB | 10.1% | 0/0/0 |
| 1,000 | 1.489 ms / 5.56 MB | 7.746 ms | 15.057 ms | 15.057 ms | 129.1 | 1.17 KB | 6.7 MB | 61.7 MB | 10% | 0/0/0 |
| 10,000 | 17.41 ms / 23.74 MB | 13.152 ms | 26.294 ms | 26.294 ms | 76 | 1.17 KB | 21.3 MB | 75.4 MB | 12.1% | 0/0/0 |
| 100,000 | 61.342 ms / 161.59 MB | 122.813 ms | 134.783 ms | 134.783 ms | 8.1 | 55.22 KB | 110 MB | 176.7 MB | 10% | 0/0/0 |

Counters:
- `10`: hierarchyPairs=2; toggleCandidates=2
- `100`: hierarchyPairs=20; toggleCandidates=13
- `1,000`: hierarchyPairs=200; toggleCandidates=125
- `10,000`: hierarchyPairs=2000; toggleCandidates=1250
- `100,000`: hierarchyPairs=20000; toggleCandidates=12500

## physics.fixed

Bepu body creation, activation, kinematic sync, fixed timestep, and transform sync.

| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 11.859 ms / 5.23 MB | 0.016 ms | 0.025 ms | 0.025 ms | 61253.6 | 880.5 B | 6.2 MB | 70.9 MB | 13.9% | 0/0/0 |
| 100 | 0.863 ms / 5.23 MB | 0.098 ms | 0.131 ms | 0.131 ms | 10183 | 922.5 B | 6.3 MB | 68.6 MB | 10.7% | 0/0/0 |
| 1,000 | 2.613 ms / 5.32 MB | 1.282 ms | 8.313 ms | 8.313 ms | 779.7 | 922.5 B | 6.4 MB | 70 MB | 10.1% | 0/0/0 |
| 10,000 | 50.265 ms / 17.61 MB | 5.299 ms | 5.881 ms | 5.881 ms | 188.7 | 880.5 B | 14.7 MB | 80.8 MB | 10% | 0/0/0 |
| 100,000 | 353.994 ms / 110.54 MB | 22.844 ms | 23.803 ms | 23.803 ms | 43.8 | 880.5 B | 63.7 MB | 175 MB | 10.4% | 0/0/0 |

Counters:
- `10`: dynamicBodies=7; kinematicBodies=1; staticBodies=2
- `100`: dynamicBodies=68; kinematicBodies=17; staticBodies=15
- `1,000`: dynamicBodies=686; kinematicBodies=171; staticBodies=143
- `10,000`: dynamicBodies=6857; kinematicBodies=1714; staticBodies=1429
- `100,000`: dynamicBodies=68572; kinematicBodies=17142; staticBodies=14286

## render.dynamic

Dynamic mesh extraction, material resolve, frustum culling, raycast request, and headless compute load.

| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 17.907 ms / 5.41 MB | 0.105 ms | 0.203 ms | 0.203 ms | 9529.5 | 1.52 KB | 6.6 MB | 112.9 MB | 10.6% | 0/0/0 |
| 100 | 0.654 ms / 5.41 MB | 0.176 ms | 0.24 ms | 0.24 ms | 5667.5 | 7.96 KB | 6.8 MB | 113 MB | 10.5% | 0/0/0 |
| 1,000 | 1.066 ms / 5.41 MB | 1.137 ms | 2.905 ms | 2.905 ms | 879.9 | 72.94 KB | 8.5 MB | 113.8 MB | 10% | 0/0/0 |
| 10,000 | 8.918 ms / 16.9 MB | 7.204 ms | 7.924 ms | 7.924 ms | 138.8 | 723.33 KB | 30.9 MB | 138.3 MB | 11% | 1/0/0 |
| 100,000 | 36.145 ms / 102.66 MB | 25.427 ms | 33.563 ms | 33.563 ms | 39.3 | 7.06 MB | 126.8 MB | 287.4 MB | 10.1% | 18/1/0 |

Counters:
- `10`: triangles=20; shadowTriangles=6; textures=1; textureBytes=340; computePixels=14400; computeTriangles=20; computeChecksum=9649.444
- `100`: triangles=200; shadowTriangles=50; textures=1; textureBytes=340; computePixels=14400; computeTriangles=200; computeChecksum=97394.974
- `1,000`: triangles=2000; shadowTriangles=500; textures=1; textureBytes=340; computePixels=14400; computeTriangles=2000; computeChecksum=970664.254
- `10,000`: triangles=20000; shadowTriangles=4985; textures=1; textureBytes=340; computePixels=14400; computeTriangles=20000; computeChecksum=9708534.145
- `100,000`: triangles=106524; shadowTriangles=11525; textures=1; textureBytes=340; computePixels=14400; computeTriangles=106524; computeChecksum=48488037.244

## render.static-rebuild

Static render cache rebuild under dirty static meshes, then culling and headless compute render.

| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 1.471 ms / 5.45 MB | 0.082 ms | 0.095 ms | 0.095 ms | 12139.6 | 1.41 KB | 6.6 MB | 116 MB | 10.7% | 0/0/0 |
| 100 | 0.682 ms / 5.54 MB | 0.107 ms | 0.117 ms | 0.117 ms | 9306.4 | 7.22 KB | 6.9 MB | 113.8 MB | 10.5% | 0/0/0 |
| 1,000 | 1.478 ms / 6.3 MB | 0.565 ms | 3.455 ms | 3.455 ms | 1769 | 65.35 KB | 8.4 MB | 115.1 MB | 10.1% | 0/0/0 |
| 10,000 | 15.678 ms / 30.06 MB | 2.801 ms | 3.36 ms | 3.36 ms | 357 | 646.6 KB | 30.4 MB | 139.1 MB | 10% | 1/0/0 |
| 100,000 | 80.227 ms / 226.86 MB | 24.315 ms | 31.482 ms | 31.482 ms | 41.1 | 6.31 MB | 138.2 MB | 285.5 MB | 10% | 16/1/0 |

Counters:
- `10`: triangles=20; shadowTriangles=8; textures=1; textureBytes=340; computePixels=14400; computeTriangles=20; computeChecksum=10000.481
- `100`: triangles=200; shadowTriangles=68; textures=1; textureBytes=340; computePixels=14400; computeTriangles=200; computeChecksum=100965.408
- `1,000`: triangles=2000; shadowTriangles=668; textures=1; textureBytes=340; computePixels=14400; computeTriangles=2000; computeChecksum=1007226.155
- `10,000`: triangles=20000; shadowTriangles=6650; textures=1; textureBytes=340; computePixels=14400; computeTriangles=20000; computeChecksum=10068454.761
- `100,000`: triangles=106524; shadowTriangles=15352; textures=1; textureBytes=340; computePixels=14400; computeTriangles=106524; computeChecksum=49320688.845

## ui.overlay

FPS/profiler text mutation, UI sorting, overlay command generation, and backend present.

| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 5.194 ms / 5.23 MB | 0.058 ms | 0.109 ms | 0.109 ms | 17170.5 | 1.08 KB | 6.2 MB | 115 MB | 11.1% | 0/0/0 |
| 100 | 0.454 ms / 5.14 MB | 0.087 ms | 0.107 ms | 0.107 ms | 11498.1 | 1.12 KB | 6.2 MB | 115.1 MB | 10.7% | 0/0/0 |
| 1,000 | 0.885 ms / 5.18 MB | 0.69 ms | 2.028 ms | 2.028 ms | 1450 | 1.66 KB | 6.4 MB | 118.6 MB | 10.1% | 0/0/0 |
| 10,000 | 10.67 ms / 11.22 MB | 5.757 ms | 5.946 ms | 5.946 ms | 173.7 | 7.08 KB | 13.6 MB | 123 MB | 10% | 0/0/0 |
| 100,000 | 34.869 ms / 57.35 MB | 22.101 ms | 23.727 ms | 23.727 ms | 45.2 | 66.58 KB | 56.8 MB | 180.4 MB | 10.1% | 0/0/0 |

Counters:
- `10`: presentCalls=20; overlayTexts=10; uiCommands=10; presentChecksum=107332680
- `100`: presentCalls=20; overlayTexts=100; uiCommands=100; presentChecksum=107395300
- `1,000`: presentCalls=20; overlayTexts=1000; uiCommands=1000; presentChecksum=108455740
- `10,000`: presentCalls=20; overlayTexts=10000; uiCommands=10000; presentChecksum=163612280
- `100,000`: presentCalls=20; overlayTexts=100000; uiCommands=100000; presentChecksum=5170171680

## scene.startup

Synthetic scene resource discovery/load and runtime entity hierarchy creation.

| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 9.391 ms / 5.32 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 9.8 MB | 120.6 MB | 0% | 0/0/0 |
| 100 | 3.078 ms / 5.4 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 10.3 MB | 120.7 MB | 0% | 0/0/0 |
| 1,000 | 2.85 ms / 6.19 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 10.8 MB | 120.7 MB | 0% | 0/0/0 |
| 10,000 | 38.157 ms / 34.89 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 38.7 MB | 134.4 MB | 0% | 0/0/0 |
| 100,000 | 315.219 ms / 277.55 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 271.9 MB | 291.9 MB | 0% | 0/0/0 |

Counters:
- `10`: sceneEntities=10; meshes=2; textures=1; textureBytes=340
- `100`: sceneEntities=100; meshes=2; textures=1; textureBytes=340
- `1,000`: sceneEntities=1000; meshes=2; textures=1; textureBytes=340
- `10,000`: sceneEntities=10000; meshes=2; textures=1; textureBytes=340
- `100,000`: sceneEntities=100000; meshes=2; textures=1; textureBytes=340

## full.mixed

Headless engine frame with controller input, physics subset, shared transforms, render, and UI.

| entities | setup | mean | p95 | worst | fps | alloc/f | managed | working | cpu | gc |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 10 | 7.278 ms / 6.83 MB | 0.142 ms | 0.239 ms | 0.239 ms | 7017.7 | 3.81 KB | 9.1 MB | 179 MB | 10.4% | 0/0/0 |
| 100 | 1.155 ms / 6.83 MB | 0.637 ms | 3.395 ms | 3.395 ms | 1570.3 | 6.76 KB | 9.3 MB | 176.4 MB | 10% | 0/0/0 |
| 1,000 | 1.793 ms / 6.93 MB | 1.763 ms | 2.861 ms | 2.861 ms | 567.3 | 39.32 KB | 10.7 MB | 172.7 MB | 10% | 0/0/0 |
| 10,000 | 21.504 ms / 56.27 MB | 15.126 ms | 23.766 ms | 23.766 ms | 66.1 | 363.71 KB | 60.9 MB | 179.9 MB | 10% | 0/0/0 |
| 100,000 | 117.981 ms / 429.64 MB | 128.894 ms | 135.574 ms | 135.574 ms | 7.8 | 3.57 MB | 332.8 MB | 483.1 MB | 10.6% | 9/1/0 |

Counters:
- `10`: triangles=18; shadowTriangles=14; textures=1; textureBytes=340; presentCalls=20; overlayTexts=2; computePixels=14400; computeTriangles=18; computeChecksum=10975.342; renderables=5; physicsBodies=1; uiEntities=2; controllers=2; fpsCharacters=1
- `100`: triangles=160; shadowTriangles=62; textures=1; textureBytes=340; presentCalls=20; overlayTexts=19; computePixels=14400; computeTriangles=160; computeChecksum=81208.671; renderables=50; physicsBodies=10; uiEntities=20; controllers=3; fpsCharacters=1
- `1,000`: triangles=1696; shadowTriangles=570; textures=1; textureBytes=340; presentCalls=20; overlayTexts=204; computePixels=14400; computeTriangles=1696; computeChecksum=855223.377; renderables=500; physicsBodies=106; uiEntities=200; controllers=24; fpsCharacters=8
- `10,000`: triangles=17060; shadowTriangles=5679; textures=1; textureBytes=340; presentCalls=20; overlayTexts=2055; computePixels=14400; computeTriangles=17060; computeChecksum=8599634.2; renderables=5000; physicsBodies=1063; uiEntities=2000; controllers=236; fpsCharacters=79
- `100,000`: triangles=90897; shadowTriangles=12762; textures=1; textureBytes=340; presentCalls=20; overlayTexts=20512; computePixels=14400; computeTriangles=90897; computeChecksum=42065711.594; renderables=50000; physicsBodies=10625; uiEntities=20000; controllers=2345; fpsCharacters=782


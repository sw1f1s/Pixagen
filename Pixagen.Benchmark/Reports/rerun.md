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
| 10 | 56.344 ms / 5.26 MB | 0.004 ms | 0.008 ms | 0.008 ms | 253465.3 | 189 B | 5.9 MB | 46.3 MB | 120.4% | 0/0/0 |
| 100 | 0.915 ms / 5.17 MB | 0.039 ms | 0.046 ms | 0.046 ms | 25572.8 | 189 B | 5.9 MB | 47 MB | 11.2% | 0/0/0 |
| 1,000 | 1.603 ms / 5.26 MB | 0.484 ms | 3.341 ms | 3.341 ms | 2066.8 | 210 B | 6.1 MB | 52.5 MB | 10.1% | 0/0/0 |
| 10,000 | 19.177 ms / 19.44 MB | 1.729 ms | 1.796 ms | 1.796 ms | 578.3 | 189 B | 17.3 MB | 67.7 MB | 10% | 0/0/0 |
| 100,000 | 93.136 ms / 127.44 MB | 2.179 ms | 2.238 ms | 2.238 ms | 458.9 | 200.5 B | 87.7 MB | 140.3 MB | 10.5% | 0/0/0 |

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
| 10 | 10.792 ms / 5.47 MB | 0.077 ms | 0.08 ms | 0.08 ms | 12911 | 6.08 KB | 6.7 MB | 66.8 MB | 10.7% | 0/0/0 |
| 100 | 1.165 ms / 5.49 MB | 0.811 ms | 1.722 ms | 1.722 ms | 1232.9 | 55.93 KB | 7.5 MB | 66.4 MB | 10.1% | 0/0/0 |
| 1,000 | 1.734 ms / 5.65 MB | 7.926 ms | 15.179 ms | 15.179 ms | 126.2 | 556.06 KB | 9.6 MB | 66.2 MB | 10% | 1/0/0 |
| 10,000 | 23.514 ms / 25.75 MB | 15.263 ms | 28.183 ms | 28.183 ms | 65.5 | 5.43 MB | 26.9 MB | 82.9 MB | 11.8% | 14/1/0 |
| 100,000 | 118.531 ms / 179.91 MB | 130.105 ms | 142.886 ms | 142.886 ms | 7.7 | 54.32 MB | 134.1 MB | 190.7 MB | 10% | 146/3/1 |

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
| 10 | 13.455 ms / 5.23 MB | 0.022 ms | 0.08 ms | 0.08 ms | 45416.7 | 5.09 KB | 6.3 MB | 79.9 MB | 13.5% | 0/0/0 |
| 100 | 0.836 ms / 5.26 MB | 0.102 ms | 0.109 ms | 0.109 ms | 9829.5 | 49.26 KB | 7 MB | 76.4 MB | 10.7% | 0/0/0 |
| 1,000 | 3.248 ms / 5.55 MB | 1.401 ms | 9.131 ms | 9.131 ms | 713.9 | 491.28 KB | 8.2 MB | 76 MB | 10% | 1/0/0 |
| 10,000 | 55.853 ms / 21.06 MB | 5.85 ms | 6.959 ms | 6.959 ms | 170.9 | 4.8 MB | 22.1 MB | 87.5 MB | 10% | 12/0/0 |
| 100,000 | 360.271 ms / 143.24 MB | 28.451 ms | 29.669 ms | 29.669 ms | 35.1 | 47.96 MB | 86.9 MB | 193.7 MB | 10.3% | 128/0/0 |

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
| 10 | 18.131 ms / 5.42 MB | 0.098 ms | 0.122 ms | 0.122 ms | 10207.3 | 3.6 KB | 6.6 MB | 123.3 MB | 10.5% | 0/0/0 |
| 100 | 0.883 ms / 5.42 MB | 0.176 ms | 0.19 ms | 0.19 ms | 5666.1 | 29.35 KB | 7.2 MB | 123 MB | 10.3% | 0/0/0 |
| 1,000 | 1.293 ms / 5.5 MB | 1.101 ms | 2.63 ms | 2.63 ms | 908.3 | 287.75 KB | 12 MB | 122.5 MB | 10.1% | 0/0/0 |
| 10,000 | 13.631 ms / 18.91 MB | 7.759 ms | 9.671 ms | 9.671 ms | 128.9 | 2.8 MB | 31.1 MB | 144.7 MB | 12.3% | 7/1/0 |
| 100,000 | 74.228 ms / 120.97 MB | 27.217 ms | 29.505 ms | 29.505 ms | 36.7 | 28.04 MB | 149.1 MB | 299.7 MB | 10% | 74/1/0 |

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
| 10 | 1.411 ms / 5.45 MB | 0.083 ms | 0.106 ms | 0.106 ms | 12102.5 | 1.32 KB | 6.6 MB | 151.2 MB | 10.8% | 0/0/0 |
| 100 | 1.171 ms / 5.55 MB | 0.109 ms | 0.149 ms | 0.149 ms | 9181.3 | 7.11 KB | 6.9 MB | 151.6 MB | 11% | 0/0/0 |
| 1,000 | 1.746 ms / 6.38 MB | 0.533 ms | 3.09 ms | 3.09 ms | 1875.1 | 65.25 KB | 8.6 MB | 150.9 MB | 10.1% | 0/0/0 |
| 10,000 | 19.677 ms / 32.07 MB | 2.754 ms | 3.266 ms | 3.266 ms | 363.1 | 646.5 KB | 32.8 MB | 153.1 MB | 10% | 1/0/0 |
| 100,000 | 117.794 ms / 253.18 MB | 23.391 ms | 24.784 ms | 24.784 ms | 42.8 | 6.31 MB | 163.9 MB | 287 MB | 10.5% | 16/0/0 |

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
| 10 | 5.442 ms / 5.16 MB | 0.063 ms | 0.18 ms | 0.18 ms | 15755.1 | 2.54 KB | 6.2 MB | 149.1 MB | 10.9% | 0/0/0 |
| 100 | 0.462 ms / 5.17 MB | 0.095 ms | 0.139 ms | 0.139 ms | 10495.2 | 18.16 KB | 6.5 MB | 149 MB | 10.5% | 0/0/0 |
| 1,000 | 0.985 ms / 5.29 MB | 0.734 ms | 2.344 ms | 2.344 ms | 1363 | 177.1 KB | 9.3 MB | 148.3 MB | 10% | 0/0/0 |
| 10,000 | 14.452 ms / 13.96 MB | 5.99 ms | 6.483 ms | 6.483 ms | 166.9 | 1.72 MB | 20.1 MB | 148.6 MB | 10% | 4/0/0 |
| 100,000 | 73.19 ms / 81.64 MB | 29.833 ms | 32.174 ms | 32.174 ms | 33.5 | 17.24 MB | 82.8 MB | 184.9 MB | 10% | 46/16/0 |

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
| 10 | 8.343 ms / 5.32 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 9.8 MB | 159.5 MB | 0% | 0/0/0 |
| 100 | 3.035 ms / 5.39 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 10.3 MB | 158.1 MB | 0% | 0/0/0 |
| 1,000 | 2.77 ms / 6.19 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 10.8 MB | 158.1 MB | 0% | 0/0/0 |
| 10,000 | 35.681 ms / 34.89 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 38.7 MB | 157.1 MB | 0% | 0/0/0 |
| 100,000 | 307.218 ms / 277.55 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 264.6 MB | 292.4 MB | 0% | 0/0/0 |

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
| 10 | 7.244 ms / 6.85 MB | 0.152 ms | 0.237 ms | 0.237 ms | 6558.6 | 7.9 KB | 9.2 MB | 204.5 MB | 10.3% | 0/0/0 |
| 100 | 1.163 ms / 6.86 MB | 0.638 ms | 3.66 ms | 3.66 ms | 1568.2 | 65.72 KB | 10.3 MB | 201.5 MB | 10% | 0/0/0 |
| 1,000 | 2.089 ms / 7.06 MB | 1.797 ms | 2.708 ms | 2.708 ms | 556.4 | 655.97 KB | 14.5 MB | 195.1 MB | 10% | 1/0/0 |
| 10,000 | 26.97 ms / 59.18 MB | 16 ms | 24.067 ms | 24.067 ms | 62.5 | 6.4 MB | 58.8 MB | 171.5 MB | 10% | 17/0/0 |
| 100,000 | 171.893 ms / 463.77 MB | 139.152 ms | 158.389 ms | 158.389 ms | 7.2 | 64.06 MB | 359.6 MB | 455.8 MB | 10.5% | 172/16/2 |

Counters:
- `10`: triangles=18; shadowTriangles=14; textures=1; textureBytes=340; presentCalls=20; overlayTexts=2; computePixels=14400; computeTriangles=18; computeChecksum=10975.342; renderables=5; physicsBodies=1; uiEntities=2; controllers=2; fpsCharacters=1
- `100`: triangles=160; shadowTriangles=62; textures=1; textureBytes=340; presentCalls=20; overlayTexts=19; computePixels=14400; computeTriangles=160; computeChecksum=81208.671; renderables=50; physicsBodies=10; uiEntities=20; controllers=3; fpsCharacters=1
- `1,000`: triangles=1696; shadowTriangles=570; textures=1; textureBytes=340; presentCalls=20; overlayTexts=204; computePixels=14400; computeTriangles=1696; computeChecksum=855223.377; renderables=500; physicsBodies=106; uiEntities=200; controllers=24; fpsCharacters=8
- `10,000`: triangles=17060; shadowTriangles=5679; textures=1; textureBytes=340; presentCalls=20; overlayTexts=2055; computePixels=14400; computeTriangles=17060; computeChecksum=8599634.2; renderables=5000; physicsBodies=1063; uiEntities=2000; controllers=236; fpsCharacters=79
- `100,000`: triangles=90897; shadowTriangles=12762; textures=1; textureBytes=340; presentCalls=20; overlayTexts=20512; computePixels=14400; computeTriangles=90897; computeChecksum=42065711.594; renderables=50000; physicsBodies=10625; uiEntities=20000; controllers=2345; fpsCharacters=782


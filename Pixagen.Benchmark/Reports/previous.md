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
| 10 | 57.05 ms / 5.21 MB | 0.004 ms | 0.005 ms | 0.005 ms | 282565.7 | 189 B | 5.9 MB | 46.5 MB | 126.1% | 0/0/0 |
| 100 | 0.705 ms / 5.22 MB | 0.036 ms | 0.041 ms | 0.041 ms | 27653.8 | 210 B | 5.9 MB | 47.4 MB | 11.3% | 0/0/0 |
| 1,000 | 1.456 ms / 5.26 MB | 0.462 ms | 3.067 ms | 3.067 ms | 2164.2 | 210 B | 6.1 MB | 52.8 MB | 10.2% | 0/0/0 |
| 10,000 | 19.404 ms / 19.44 MB | 1.723 ms | 1.916 ms | 1.916 ms | 580.5 | 189 B | 17.3 MB | 67.4 MB | 10% | 0/0/0 |
| 100,000 | 91.337 ms / 127.44 MB | 2.141 ms | 2.276 ms | 2.276 ms | 467 | 200.5 B | 87.7 MB | 139.6 MB | 10.4% | 0/0/0 |

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
| 10 | 9.512 ms / 5.47 MB | 0.076 ms | 0.097 ms | 0.097 ms | 13145.7 | 6.08 KB | 6.7 MB | 66.1 MB | 11.4% | 0/0/0 |
| 100 | 1.075 ms / 5.49 MB | 0.777 ms | 1.595 ms | 1.595 ms | 1286.7 | 55.93 KB | 7.5 MB | 66.4 MB | 10.1% | 0/0/0 |
| 1,000 | 1.85 ms / 5.65 MB | 7.812 ms | 14.271 ms | 14.271 ms | 128 | 556.06 KB | 9.6 MB | 65.4 MB | 10% | 1/0/0 |
| 10,000 | 23.288 ms / 25.75 MB | 14.286 ms | 30.706 ms | 30.706 ms | 70 | 5.43 MB | 26.9 MB | 82 MB | 12.2% | 14/1/0 |
| 100,000 | 123.38 ms / 179.91 MB | 128.715 ms | 137.746 ms | 137.746 ms | 7.8 | 54.32 MB | 134.1 MB | 189.6 MB | 10% | 146/3/1 |

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
| 10 | 11.779 ms / 5.23 MB | 0.023 ms | 0.086 ms | 0.086 ms | 44107.9 | 5.05 KB | 6.3 MB | 78.9 MB | 12.6% | 0/0/0 |
| 100 | 0.938 ms / 5.26 MB | 0.109 ms | 0.193 ms | 0.193 ms | 9195.6 | 49.22 KB | 7 MB | 75.3 MB | 10.6% | 0/0/0 |
| 1,000 | 3.468 ms / 5.55 MB | 1.426 ms | 8.941 ms | 8.941 ms | 701.3 | 491.28 KB | 8.2 MB | 75.1 MB | 10% | 1/0/0 |
| 10,000 | 57.847 ms / 21.06 MB | 5.998 ms | 7.022 ms | 7.022 ms | 166.7 | 4.8 MB | 22.1 MB | 86.8 MB | 10% | 12/0/0 |
| 100,000 | 415.706 ms / 143.24 MB | 31.814 ms | 48.957 ms | 48.957 ms | 31.4 | 47.96 MB | 86.9 MB | 193.3 MB | 10.1% | 128/0/0 |

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
| 10 | 17.795 ms / 5.42 MB | 0.099 ms | 0.157 ms | 0.157 ms | 10134.6 | 3.66 KB | 6.6 MB | 123 MB | 10.8% | 0/0/0 |
| 100 | 0.919 ms / 5.42 MB | 0.178 ms | 0.225 ms | 0.225 ms | 5616.5 | 29.29 KB | 7.2 MB | 122.5 MB | 10.5% | 0/0/0 |
| 1,000 | 1.341 ms / 5.5 MB | 1.163 ms | 2.932 ms | 2.932 ms | 860.1 | 287.69 KB | 12 MB | 121.9 MB | 10% | 0/0/0 |
| 10,000 | 14.693 ms / 18.91 MB | 7.683 ms | 9.071 ms | 9.071 ms | 130.2 | 2.8 MB | 31.1 MB | 144.5 MB | 13% | 7/1/0 |
| 100,000 | 76.385 ms / 120.97 MB | 29.724 ms | 34.034 ms | 34.034 ms | 33.6 | 28.04 MB | 149.1 MB | 299.4 MB | 9.9% | 74/0/0 |

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
| 10 | 1.53 ms / 5.45 MB | 0.095 ms | 0.228 ms | 0.228 ms | 10542.2 | 1.25 KB | 6.6 MB | 126.5 MB | 9.9% | 0/0/0 |
| 100 | 1.21 ms / 5.55 MB | 0.146 ms | 0.359 ms | 0.359 ms | 6854.6 | 7.07 KB | 6.9 MB | 123.5 MB | 8.7% | 0/0/0 |
| 1,000 | 2.646 ms / 6.38 MB | 0.683 ms | 4.732 ms | 4.732 ms | 1464.5 | 65.19 KB | 8.6 MB | 123.4 MB | 8.9% | 0/0/0 |
| 10,000 | 21.668 ms / 32.07 MB | 3.003 ms | 3.903 ms | 3.903 ms | 333 | 646.44 KB | 32.8 MB | 144.5 MB | 9.9% | 1/0/0 |
| 100,000 | 130.234 ms / 253.18 MB | 28.646 ms | 39.693 ms | 39.693 ms | 34.9 | 6.31 MB | 163.9 MB | 287.3 MB | 9.4% | 16/1/0 |

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
| 10 | 5.994 ms / 5.16 MB | 0.066 ms | 0.095 ms | 0.095 ms | 15073.6 | 2.48 KB | 6.2 MB | 121.7 MB | 10.8% | 0/0/0 |
| 100 | 0.653 ms / 5.17 MB | 0.1 ms | 0.148 ms | 0.148 ms | 9982.1 | 18.16 KB | 6.5 MB | 121.8 MB | 10.5% | 0/0/0 |
| 1,000 | 1.342 ms / 5.29 MB | 0.802 ms | 2.5 ms | 2.5 ms | 1246.6 | 177.01 KB | 9.3 MB | 121.1 MB | 9.9% | 0/0/0 |
| 10,000 | 16.852 ms / 13.96 MB | 6.622 ms | 7.606 ms | 7.606 ms | 151 | 1.72 MB | 20.1 MB | 127.3 MB | 9.9% | 4/0/0 |
| 100,000 | 80.013 ms / 81.64 MB | 33.085 ms | 38.802 ms | 38.802 ms | 30.2 | 17.24 MB | 82.8 MB | 184.9 MB | 9.8% | 46/16/0 |

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
| 10 | 8.725 ms / 5.32 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 9.8 MB | 132.5 MB | 0% | 0/0/0 |
| 100 | 2.817 ms / 5.39 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 10.3 MB | 131.6 MB | 0% | 0/0/0 |
| 1,000 | 2.431 ms / 6.19 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 10.8 MB | 131.6 MB | 0% | 0/0/0 |
| 10,000 | 35.641 ms / 34.89 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 38.7 MB | 139.9 MB | 0% | 0/0/0 |
| 100,000 | 316.088 ms / 277.55 MB | 0 ms | 0 ms | 0 ms | 0 | 0 B | 265.3 MB | 293 MB | 0% | 0/0/0 |

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
| 10 | 6.97 ms / 6.94 MB | 0.142 ms | 0.259 ms | 0.259 ms | 7048.9 | 7.84 KB | 9.2 MB | 178.8 MB | 10.5% | 0/0/0 |
| 100 | 1.227 ms / 6.87 MB | 0.617 ms | 3.381 ms | 3.381 ms | 1620.9 | 65.72 KB | 10.3 MB | 176 MB | 10.1% | 0/0/0 |
| 1,000 | 1.959 ms / 7.06 MB | 1.867 ms | 2.75 ms | 2.75 ms | 535.7 | 655.91 KB | 14.5 MB | 170 MB | 10% | 1/0/0 |
| 10,000 | 29.076 ms / 59.18 MB | 16.546 ms | 24.909 ms | 24.909 ms | 60.4 | 6.4 MB | 58.8 MB | 163.7 MB | 9.9% | 17/0/0 |
| 100,000 | 172.334 ms / 463.77 MB | 156.35 ms | 187.43 ms | 187.43 ms | 6.4 | 64.06 MB | 359.6 MB | 422.8 MB | 10.2% | 172/16/2 |

Counters:
- `10`: triangles=18; shadowTriangles=14; textures=1; textureBytes=340; presentCalls=20; overlayTexts=2; computePixels=14400; computeTriangles=18; computeChecksum=10975.342; renderables=5; physicsBodies=1; uiEntities=2; controllers=2; fpsCharacters=1
- `100`: triangles=160; shadowTriangles=62; textures=1; textureBytes=340; presentCalls=20; overlayTexts=19; computePixels=14400; computeTriangles=160; computeChecksum=81208.671; renderables=50; physicsBodies=10; uiEntities=20; controllers=3; fpsCharacters=1
- `1,000`: triangles=1696; shadowTriangles=570; textures=1; textureBytes=340; presentCalls=20; overlayTexts=204; computePixels=14400; computeTriangles=1696; computeChecksum=855223.377; renderables=500; physicsBodies=106; uiEntities=200; controllers=24; fpsCharacters=8
- `10,000`: triangles=17060; shadowTriangles=5679; textures=1; textureBytes=340; presentCalls=20; overlayTexts=2055; computePixels=14400; computeTriangles=17060; computeChecksum=8599634.2; renderables=5000; physicsBodies=1063; uiEntities=2000; controllers=236; fpsCharacters=79
- `100,000`: triangles=90897; shadowTriangles=12762; textures=1; textureBytes=340; presentCalls=20; overlayTexts=20512; computePixels=14400; computeTriangles=90897; computeChecksum=42065711.594; renderables=50000; physicsBodies=10625; uiEntities=20000; controllers=2345; fpsCharacters=782


# Pixagen

Pixagen is a pixel-oriented engine built around a custom Entity Component System architecture. It focuses on compact raycast-rendered scenes and low-level rendering experiments where simulation, physics, and rendering stay close to the metal.

The project is designed as a playground for retro-style visuals, engine structure experiments, and game logic systems.

## Technology Stack

| Area | Technology | Purpose |
| --- | --- | --- |
| Runtime | [.NET 8](https://github.com/dotnet/runtime) | Main application runtime and tooling. |
| ECS | Custom Pixagen ECS | Entity/component storage, filters, system phases, and DI-style system wiring. |
| FP math | [FixedMathSharp.Lean 5.0.0](https://github.com/mrdav30/FixedMathSharp) | Deterministic fixed-point math for simulation-friendly transforms and game state. |
| Physics | [BepuPhysics 2.4.0](https://github.com/bepu/bepuphysics2) | 3D rigid-body physics integration. |
| Render | [Veldrid 4.9.0](https://github.com/mellinoe/veldrid) | Low-level graphics abstraction used by the Vulkan rendering backend. |
| Shaders | [Veldrid.SPIRV 1.0.15](https://github.com/mellinoe/veldrid-spirv) | SPIR-V shader workflow for graphics and compute shaders. |
| Windowing | [SDL 2.32.10](https://github.com/libsdl-org/SDL) via `Ultz.Native.SDL` | Native window and input support for the rendering layer. |
| macOS Vulkan | [MoltenVK](https://github.com/KhronosGroup/MoltenVK) via `AiDotNet.Native.MoltenVK` | Vulkan-on-Metal compatibility for macOS. |

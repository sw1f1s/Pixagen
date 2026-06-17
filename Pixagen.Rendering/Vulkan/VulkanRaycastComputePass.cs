using System.Numerics;
using System.Runtime.InteropServices;
using Pixagen.Rendering.Raycasting;
using Pixagen.Rendering.Textures;
using Veldrid;

namespace Pixagen.Rendering.Vulkan;

internal sealed class VulkanRaycastComputePass : IDisposable
{
    private ResourceLayout? _layout;
    private ResourceSet? _resourceSet;
    private Pipeline? _pipeline;
    private DeviceBuffer? _paramsBuffer;
    private DeviceBuffer? _triangleBuffer;
    private DeviceBuffer? _shadowTriangleBuffer;
    private DeviceBuffer? _textureInfoBuffer;
    private DeviceBuffer? _texturePixelBuffer;
    private DeviceBuffer? _tileRangeBuffer;
    private DeviceBuffer? _tileTriangleIndexBuffer;
    private DeviceBuffer? _shadowCellRangeBuffer;
    private DeviceBuffer? _shadowCellTriangleIndexBuffer;
    private int _triangleCapacity;
    private int _shadowTriangleCapacity;
    private int _textureInfoCapacity;
    private int _texturePixelCapacity;
    private int _tileRangeCapacity;
    private int _tileTriangleIndexCapacity;
    private int _shadowCellRangeCapacity;
    private int _shadowCellTriangleIndexCapacity;
    private GpuTriangle[] _gpuTriangles = [GpuTriangle.Empty];
    private GpuTriangle[] _gpuShadowTriangles = [GpuTriangle.Empty];
    private GpuTextureInfo[] _gpuTextureInfos = [GpuTextureInfo.Empty];
    private Vector4[] _gpuTexturePixels = [Vector4.One];
    private GpuTileRange[] _gpuTileRanges = [GpuTileRange.Empty];
    private uint[] _gpuTileTriangleIndices = [0];
    private GpuTileRange[] _gpuShadowCellRanges = [GpuTileRange.Empty];
    private uint[] _gpuShadowCellTriangleIndices = [0];
    private readonly Dictionary<TextureAsset, int> _textureIndices = new();
    private readonly List<TextureAsset> _textures = new();

    public uint DispatchX { get; private set; }
    public uint DispatchY { get; private set; }

    public void WarmUp(
        GraphicsDevice graphicsDevice,
        Texture sceneTexture,
        VulkanGpuFrameTracker frameTracker,
        Func<ResourceFactory, VulkanShaderResource> loadShaders)
    {
        EnsurePipeline(graphicsDevice, frameTracker, loadShaders);
        EnsureResourceSet(graphicsDevice, sceneTexture);
    }

    public bool Prepare(
        GraphicsDevice graphicsDevice,
        Texture sceneTexture,
        VulkanGpuFrameTracker frameTracker,
        Func<ResourceFactory, VulkanShaderResource> loadShaders,
        in RaycastComputeRequest request)
    {
        if (request.Width <= 0 || request.Height <= 0)
        {
            DispatchX = 0;
            DispatchY = 0;
            return false;
        }

        EnsurePipeline(graphicsDevice, frameTracker, loadShaders);
        UploadScene(graphicsDevice, frameTracker, request);
        EnsureResourceSet(graphicsDevice, sceneTexture);
        DispatchX = (uint)((request.Width + 7) / 8);
        DispatchY = (uint)((request.Height + 7) / 8);
        return true;
    }

    public void Dispatch(CommandList commandList)
    {
        commandList.SetPipeline(_pipeline);
        commandList.SetComputeResourceSet(0, _resourceSet);
        commandList.Dispatch(DispatchX, DispatchY, 1);
    }

    public void InvalidateResourceSet()
    {
        _resourceSet?.Dispose();
        _resourceSet = null;
    }

    public long EstimateVramBytes()
    {
        long bytes = 0;
        if (_paramsBuffer is not null)
        {
            bytes += Marshal.SizeOf<GpuRaycastParams>();
        }

        bytes += (long)_triangleCapacity * Marshal.SizeOf<GpuTriangle>();
        bytes += (long)_shadowTriangleCapacity * Marshal.SizeOf<GpuTriangle>();
        bytes += (long)_textureInfoCapacity * Marshal.SizeOf<GpuTextureInfo>();
        bytes += (long)_texturePixelCapacity * Marshal.SizeOf<Vector4>();
        bytes += (long)_tileRangeCapacity * Marshal.SizeOf<GpuTileRange>();
        bytes += (long)_tileTriangleIndexCapacity * sizeof(uint);
        bytes += (long)_shadowCellRangeCapacity * Marshal.SizeOf<GpuTileRange>();
        bytes += (long)_shadowCellTriangleIndexCapacity * sizeof(uint);
        return bytes;
    }

    public void Dispose()
    {
        _pipeline?.Dispose();
        _resourceSet?.Dispose();
        _layout?.Dispose();
        _paramsBuffer?.Dispose();
        _triangleBuffer?.Dispose();
        _shadowTriangleBuffer?.Dispose();
        _textureInfoBuffer?.Dispose();
        _texturePixelBuffer?.Dispose();
        _tileRangeBuffer?.Dispose();
        _tileTriangleIndexBuffer?.Dispose();
        _shadowCellRangeBuffer?.Dispose();
        _shadowCellTriangleIndexBuffer?.Dispose();
    }

    private void EnsurePipeline(
        GraphicsDevice graphicsDevice,
        VulkanGpuFrameTracker frameTracker,
        Func<ResourceFactory, VulkanShaderResource> loadShaders)
    {
        if (_pipeline is not null)
        {
            return;
        }

        ResourceFactory factory = graphicsDevice.ResourceFactory;
        VulkanShaderResource shaders = loadShaders(factory);

        _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("OutputTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("RaycastParams", ResourceKind.UniformBuffer, ShaderStages.Compute),
            new ResourceLayoutElementDescription("Triangles", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("ShadowTriangles", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("TextureInfos", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("TexturePixels", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("TileRanges", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("TileTriangleIndices", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("ShadowCellRanges", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("ShadowCellTriangleIndices", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute)));

        ComputePipelineDescription pipelineDescription = new(
            shaders.RaycastShader,
            _layout,
            8,
            8,
            1);
        _pipeline = factory.CreateComputePipeline(ref pipelineDescription);

        _paramsBuffer = factory.CreateBuffer(new BufferDescription(
            (uint)Marshal.SizeOf<GpuRaycastParams>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _triangleBuffer, ref _triangleCapacity, 1, Marshal.SizeOf<GpuTriangle>());
        EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _shadowTriangleBuffer, ref _shadowTriangleCapacity, 1, Marshal.SizeOf<GpuTriangle>());
        EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _textureInfoBuffer, ref _textureInfoCapacity, 1, Marshal.SizeOf<GpuTextureInfo>());
        EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _texturePixelBuffer, ref _texturePixelCapacity, 1, Marshal.SizeOf<Vector4>());
        EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _tileRangeBuffer, ref _tileRangeCapacity, 1, Marshal.SizeOf<GpuTileRange>());
        EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _tileTriangleIndexBuffer, ref _tileTriangleIndexCapacity, 1, sizeof(uint));
        EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _shadowCellRangeBuffer, ref _shadowCellRangeCapacity, 1, Marshal.SizeOf<GpuTileRange>());
        EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _shadowCellTriangleIndexBuffer, ref _shadowCellTriangleIndexCapacity, 1, sizeof(uint));
    }

    private void UploadScene(
        GraphicsDevice graphicsDevice,
        VulkanGpuFrameTracker frameTracker,
        in RaycastComputeRequest request)
    {
        GpuRaycastParams parameters = GpuRaycastParams.From(request);
        graphicsDevice.UpdateBuffer(_paramsBuffer!, 0, ref parameters);

        _textureIndices.Clear();
        _textures.Clear();
        try
        {
            int triangleCount = FillTriangles(
                ref _gpuTriangles,
                request.StaticPrimitives.Triangles,
                request.DynamicPrimitives.Triangles,
                _textureIndices,
                _textures);
            int shadowTriangleCount = request.ShadowQuality == ShadowQuality.Off
                ? ClearTriangles(ref _gpuShadowTriangles)
                : FillTriangles(
                    ref _gpuShadowTriangles,
                    request.StaticPrimitives.ShadowTriangles,
                    request.DynamicPrimitives.ShadowTriangles,
                    _textureIndices,
                    _textures);
            int textureInfoCount = FillTextures(ref _gpuTextureInfos, ref _gpuTexturePixels, _textures);
            int tileRangeCount = FillTileRanges(ref _gpuTileRanges, request.TileBins);
            int tileTriangleIndexCount = FillTileTriangleIndices(ref _gpuTileTriangleIndices, request.TileBins);
            int shadowCellRangeCount = request.ShadowQuality == ShadowQuality.Off
                ? ClearTileRanges(ref _gpuShadowCellRanges)
                : FillShadowCellRanges(ref _gpuShadowCellRanges, request.ShadowBins);
            int shadowCellTriangleIndexCount = request.ShadowQuality == ShadowQuality.Off
                ? ClearTriangleIndices(ref _gpuShadowCellTriangleIndices)
                : FillShadowCellTriangleIndices(ref _gpuShadowCellTriangleIndices, request.ShadowBins);

            EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _triangleBuffer, ref _triangleCapacity, Math.Max(1, triangleCount), Marshal.SizeOf<GpuTriangle>());
            EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _shadowTriangleBuffer, ref _shadowTriangleCapacity, Math.Max(1, shadowTriangleCount), Marshal.SizeOf<GpuTriangle>());
            EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _textureInfoBuffer, ref _textureInfoCapacity, Math.Max(1, textureInfoCount), Marshal.SizeOf<GpuTextureInfo>());
            EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _texturePixelBuffer, ref _texturePixelCapacity, Math.Max(1, _gpuTexturePixels.Length), Marshal.SizeOf<Vector4>());
            EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _tileRangeBuffer, ref _tileRangeCapacity, Math.Max(1, tileRangeCount), Marshal.SizeOf<GpuTileRange>());
            EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _tileTriangleIndexBuffer, ref _tileTriangleIndexCapacity, Math.Max(1, tileTriangleIndexCount), sizeof(uint));
            EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _shadowCellRangeBuffer, ref _shadowCellRangeCapacity, Math.Max(1, shadowCellRangeCount), Marshal.SizeOf<GpuTileRange>());
            EnsureStructuredBuffer(graphicsDevice, frameTracker, ref _shadowCellTriangleIndexBuffer, ref _shadowCellTriangleIndexCapacity, Math.Max(1, shadowCellTriangleIndexCount), sizeof(uint));

            graphicsDevice.UpdateBuffer(_triangleBuffer!, 0, _gpuTriangles.AsSpan(0, Math.Max(1, triangleCount)));
            graphicsDevice.UpdateBuffer(_shadowTriangleBuffer!, 0, _gpuShadowTriangles.AsSpan(0, Math.Max(1, shadowTriangleCount)));
            graphicsDevice.UpdateBuffer(_textureInfoBuffer!, 0, _gpuTextureInfos.AsSpan(0, Math.Max(1, textureInfoCount)));
            graphicsDevice.UpdateBuffer(_texturePixelBuffer!, 0, _gpuTexturePixels.AsSpan(0, Math.Max(1, _gpuTexturePixels.Length)));
            graphicsDevice.UpdateBuffer(_tileRangeBuffer!, 0, _gpuTileRanges.AsSpan(0, Math.Max(1, tileRangeCount)));
            graphicsDevice.UpdateBuffer(_tileTriangleIndexBuffer!, 0, _gpuTileTriangleIndices.AsSpan(0, Math.Max(1, tileTriangleIndexCount)));
            graphicsDevice.UpdateBuffer(_shadowCellRangeBuffer!, 0, _gpuShadowCellRanges.AsSpan(0, Math.Max(1, shadowCellRangeCount)));
            graphicsDevice.UpdateBuffer(_shadowCellTriangleIndexBuffer!, 0, _gpuShadowCellTriangleIndices.AsSpan(0, Math.Max(1, shadowCellTriangleIndexCount)));
        }
        finally
        {
            _textureIndices.Clear();
            _textures.Clear();
        }
    }

    private void EnsureResourceSet(GraphicsDevice graphicsDevice, Texture sceneTexture)
    {
        if (_resourceSet is not null)
        {
            return;
        }

        _resourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _layout,
            sceneTexture,
            _paramsBuffer,
            _triangleBuffer,
            _shadowTriangleBuffer,
            _textureInfoBuffer,
            _texturePixelBuffer,
            _tileRangeBuffer,
            _tileTriangleIndexBuffer,
            _shadowCellRangeBuffer,
            _shadowCellTriangleIndexBuffer));
    }

    private void EnsureStructuredBuffer(
        GraphicsDevice graphicsDevice,
        VulkanGpuFrameTracker frameTracker,
        ref DeviceBuffer? buffer,
        ref int capacity,
        int requiredCount,
        int stride)
    {
        requiredCount = Math.Max(1, requiredCount);
        if (buffer is not null && capacity >= requiredCount)
        {
            return;
        }

        if (buffer is not null)
        {
            frameTracker.WaitForPending(graphicsDevice);
        }

        int newCapacity = Math.Max(requiredCount, Math.Max(1, capacity * 2));
        buffer?.Dispose();
        buffer = graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)(newCapacity * stride),
            BufferUsage.StructuredBufferReadOnly,
            (uint)stride));
        capacity = newCapacity;
        InvalidateResourceSet();
    }

    private static int FillTriangles(
        ref GpuTriangle[] destination,
        List<TrianglePrimitive> staticPrimitives,
        List<TrianglePrimitive> dynamicPrimitives,
        Dictionary<TextureAsset, int> textureIndices,
        List<TextureAsset> textures)
    {
        int count = staticPrimitives.Count + dynamicPrimitives.Count;
        destination = EnsureArray(destination, Math.Max(1, count));
        int index = 0;
        foreach (TrianglePrimitive triangle in staticPrimitives)
        {
            destination[index++] = GpuTriangle.From(triangle, GetTextureIndex(triangle.Material.Texture, textureIndices, textures));
        }

        foreach (TrianglePrimitive triangle in dynamicPrimitives)
        {
            destination[index++] = GpuTriangle.From(triangle, GetTextureIndex(triangle.Material.Texture, textureIndices, textures));
        }

        if (count == 0)
        {
            destination[0] = GpuTriangle.Empty;
        }

        return count;
    }

    private static int ClearTriangles(ref GpuTriangle[] destination)
    {
        destination = EnsureArray(destination, 1);
        destination[0] = GpuTriangle.Empty;
        return 0;
    }

    private static int FillTileRanges(ref GpuTileRange[] destination, RaycastTileBins tileBins)
    {
        int count = Math.Max(1, tileBins.TileCount);
        destination = EnsureArray(destination, count);
        if (tileBins.TileCount == 0)
        {
            destination[0] = GpuTileRange.Empty;
            return 1;
        }

        for (int i = 0; i < tileBins.TileCount; i++)
        {
            RaycastTileRange range = tileBins.Ranges[i];
            destination[i] = new GpuTileRange(range.Offset, range.Count);
        }

        return tileBins.TileCount;
    }

    private static int FillTileTriangleIndices(ref uint[] destination, RaycastTileBins tileBins)
    {
        int count = Math.Max(1, tileBins.IndexCount);
        destination = EnsureArray(destination, count);
        if (tileBins.IndexCount == 0)
        {
            destination[0] = 0;
            return 1;
        }

        for (int i = 0; i < tileBins.IndexCount; i++)
        {
            destination[i] = (uint)Math.Max(0, tileBins.TriangleIndices[i]);
        }

        return tileBins.IndexCount;
    }

    private static int FillShadowCellRanges(ref GpuTileRange[] destination, RaycastShadowBins shadowBins)
    {
        int count = Math.Max(1, shadowBins.CellCount);
        destination = EnsureArray(destination, count);
        if (shadowBins.CellCount == 0)
        {
            destination[0] = GpuTileRange.Empty;
            return 1;
        }

        for (int i = 0; i < shadowBins.CellCount; i++)
        {
            RaycastTileRange range = shadowBins.Ranges[i];
            destination[i] = new GpuTileRange(range.Offset, range.Count);
        }

        return shadowBins.CellCount;
    }

    private static int FillShadowCellTriangleIndices(ref uint[] destination, RaycastShadowBins shadowBins)
    {
        int count = Math.Max(1, shadowBins.IndexCount);
        destination = EnsureArray(destination, count);
        if (shadowBins.IndexCount == 0)
        {
            destination[0] = 0;
            return 1;
        }

        for (int i = 0; i < shadowBins.IndexCount; i++)
        {
            destination[i] = (uint)Math.Max(0, shadowBins.TriangleIndices[i]);
        }

        return shadowBins.IndexCount;
    }

    private static int ClearTileRanges(ref GpuTileRange[] destination)
    {
        destination = EnsureArray(destination, 1);
        destination[0] = GpuTileRange.Empty;
        return 1;
    }

    private static int ClearTriangleIndices(ref uint[] destination)
    {
        destination = EnsureArray(destination, 1);
        destination[0] = 0;
        return 1;
    }

    private static int FillTextures(
        ref GpuTextureInfo[] textureInfos,
        ref Vector4[] texturePixels,
        List<TextureAsset> textures)
    {
        int textureCount = textures.Count;
        textureInfos = EnsureArray(textureInfos, Math.Max(1, textureCount));

        int pixelCount = 0;
        for (int i = 0; i < textures.Count; i++)
        {
            TextureAsset texture = textures[i];
            textureInfos[i] = new GpuTextureInfo(texture.Width, texture.Height, pixelCount, texture.MipCount);
            pixelCount += texture.MipPixelCount;
        }

        texturePixels = EnsureArray(texturePixels, Math.Max(1, pixelCount));
        int pixelIndex = 0;
        foreach (TextureAsset texture in textures)
        {
            foreach (TextureMipLevel mipLevel in texture.MipLevels)
            {
                foreach (TexturePixel pixel in mipLevel.Pixels)
                {
                    texturePixels[pixelIndex++] = new Vector4(
                        pixel.R / 255f,
                        pixel.G / 255f,
                        pixel.B / 255f,
                        pixel.A / 255f);
                }
            }
        }

        if (textureCount == 0)
        {
            textureInfos[0] = GpuTextureInfo.Empty;
        }

        if (pixelCount == 0)
        {
            texturePixels[0] = Vector4.One;
        }

        return textureCount;
    }

    private static int GetTextureIndex(
        TextureAsset? texture,
        Dictionary<TextureAsset, int> textureIndices,
        List<TextureAsset> textures)
    {
        if (texture is null)
        {
            return -1;
        }

        if (textureIndices.TryGetValue(texture, out int index))
        {
            return index;
        }

        index = textures.Count;
        textures.Add(texture);
        textureIndices[texture] = index;
        return index;
    }

    private static T[] EnsureArray<T>(T[] source, int requiredLength)
    {
        return source.Length >= requiredLength ? source : new T[requiredLength];
    }

    private static Vector4 ToVector4(PixelColor color, float alpha)
    {
        const float scale = 1f / 255f;
        return new Vector4(color.R * scale, color.G * scale, color.B * scale, Math.Clamp(alpha, 0f, 1f));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GpuRaycastParams
    {
        public Vector4 View;
        public Vector4 Counts;
        public Vector4 ShadowCounts;
        public Vector4 TileInfo;
        public Vector4 ShadowGridMinCellSize;
        public Vector4 ShadowGridCounts;
        public Vector4 Origin;
        public Vector4 StartDirection;
        public Vector4 XDelta;
        public Vector4 YDelta;
        public Vector4 LightDirectionIntensity;
        public Vector4 LightSettings;
        public Vector4 ShadowSettings;
        public Vector4 SkyColor;

        public static GpuRaycastParams From(in RaycastComputeRequest request)
        {
            RayBuilder rayBuilder = request.RayBuilder;
            DirectionalLight light = request.Light;

            return new GpuRaycastParams
            {
                View = new Vector4(
                    request.Width,
                    request.Height,
                    request.MaxDistance,
                    1f),
                Counts = new Vector4(
                    request.StaticPrimitives.Triangles.Count + request.DynamicPrimitives.Triangles.Count,
                    0f,
                    0f,
                    (float)request.ShadowQuality),
                ShadowCounts = new Vector4(
                    ResolveShadowTriangleCount(request),
                    0f,
                    0f,
                    0f),
                TileInfo = new Vector4(
                    Math.Max(1, request.TileBins.TileSize),
                    Math.Max(1, request.TileBins.TileColumns),
                    Math.Max(1, request.TileBins.TileRows),
                    0f),
                ShadowGridMinCellSize = ResolveShadowGridMinCellSize(request),
                ShadowGridCounts = ResolveShadowGridCounts(request),
                Origin = new Vector4(rayBuilder.Origin, 0f),
                StartDirection = new Vector4(rayBuilder.StartDirection, 0f),
                XDelta = new Vector4(rayBuilder.XDelta, 0f),
                YDelta = new Vector4(rayBuilder.YDelta, 0f),
                LightDirectionIntensity = new Vector4(light.Direction, light.Intensity),
                LightSettings = new Vector4(
                    light.AmbientIntensity,
                    light.ShadowIntensity,
                    light.ShadowBias,
                    light.ShadowMaxDistance),
                ShadowSettings = new Vector4(
                    Math.Clamp(request.ShadowSoftness, 0f, 0.25f),
                    0f,
                    0f,
                    0f),
                SkyColor = new Vector4(109f / 255f, 154f / 255f, 184f / 255f, 1f)
            };
        }

        private static int ResolveShadowTriangleCount(in RaycastComputeRequest request)
        {
            return request.ShadowQuality == ShadowQuality.Off
                ? 0
                : request.StaticPrimitives.ShadowTriangles.Count + request.DynamicPrimitives.ShadowTriangles.Count;
        }

        private static Vector4 ResolveShadowGridMinCellSize(in RaycastComputeRequest request)
        {
            if (request.ShadowQuality == ShadowQuality.Off || request.ShadowBins.CellCount == 0)
            {
                return new Vector4(0f, 0f, 0f, 1f);
            }

            return new Vector4(
                request.ShadowBins.GridMin,
                Math.Max(RenderMath.Epsilon, request.ShadowBins.CellSize));
        }

        private static Vector4 ResolveShadowGridCounts(in RaycastComputeRequest request)
        {
            if (request.ShadowQuality == ShadowQuality.Off || request.ShadowBins.CellCount == 0)
            {
                return Vector4.Zero;
            }

            return new Vector4(
                Math.Max(0, request.ShadowBins.CellCountX),
                Math.Max(0, request.ShadowBins.CellCountY),
                Math.Max(0, request.ShadowBins.CellCountZ),
                Math.Max(0, request.ShadowBins.MaxRayCells));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuTileRange
    {
        public static GpuTileRange Empty => new(0, 0);

        public readonly uint Offset;
        public readonly uint Count;
        public readonly uint Padding0;
        public readonly uint Padding1;

        public GpuTileRange(int offset, int count)
        {
            Offset = (uint)Math.Max(0, offset);
            Count = (uint)Math.Max(0, count);
            Padding0 = 0;
            Padding1 = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuTriangle
    {
        public static GpuTriangle Empty => new(
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            Vector4.Zero,
            new Vector4(-1f, 0f, 0f, 0f),
            Vector4.Zero);

        public readonly Vector4 A;
        public readonly Vector4 B;
        public readonly Vector4 C;
        public readonly Vector4 Normal;
        public readonly Vector4 UvA_UvB;
        public readonly Vector4 UvC_Material;
        public readonly Vector4 Color;
        public readonly Vector4 Material;
        public readonly Vector4 TextureTransform;

        private GpuTriangle(
            Vector4 a,
            Vector4 b,
            Vector4 c,
            Vector4 normal,
            Vector4 uvA_UvB,
            Vector4 uvC_Material,
            Vector4 color,
            Vector4 material,
            Vector4 textureTransform)
        {
            A = a;
            B = b;
            C = c;
            Normal = normal;
            UvA_UvB = uvA_UvB;
            UvC_Material = uvC_Material;
            Color = color;
            Material = material;
            TextureTransform = textureTransform;
        }

        public static GpuTriangle From(TrianglePrimitive triangle, int textureIndex)
        {
            SurfaceMaterial material = triangle.Material;
            return new GpuTriangle(
                new Vector4(triangle.A, 0f),
                new Vector4(triangle.B, 0f),
                new Vector4(triangle.C, 0f),
                new Vector4(triangle.Normal, 0f),
                new Vector4(triangle.UvA.X, triangle.UvA.Y, triangle.UvB.X, triangle.UvB.Y),
                new Vector4(triangle.UvC.X, triangle.UvC.Y, textureIndex, (float)material.Shader),
                ToVector4(material.Color, material.Opacity),
                new Vector4(
                    material.AlphaCutoff,
                    textureIndex >= 0 ? 1f : 0f,
                    0f,
                    0f),
                new Vector4(
                    material.TextureTiling.X,
                    material.TextureTiling.Y,
                    material.TextureOffset.X,
                    material.TextureOffset.Y));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GpuTextureInfo
    {
        public static GpuTextureInfo Empty => new(1, 1, 0, 1);

        public readonly Vector4 SizeOffset;

        public GpuTextureInfo(int width, int height, int offset, int mipCount)
        {
            SizeOffset = new Vector4(width, height, offset, Math.Max(1, mipCount));
        }
    }
}

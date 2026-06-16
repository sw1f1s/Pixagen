#version 450

const float Epsilon = 0.0001;

struct Triangle
{
    vec4 A;
    vec4 B;
    vec4 C;
    vec4 Normal;
    vec4 UvA_UvB;
    vec4 UvC_Material;
    vec4 Color;
    vec4 Material;
    vec4 TextureTransform;
};

struct TextureInfo
{
    vec4 SizeOffset;
};

struct SurfaceSample
{
    vec3 Color;
    float Alpha;
    float AlphaCutoff;
    int Shader;
};

struct Hit
{
    bool HitSomething;
    float Distance;
    vec3 Point;
    vec3 Normal;
    vec3 Color;
    float Alpha;
    int Shader;
};

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, rgba8) uniform writeonly image2D OutputTexture;

layout(set = 0, binding = 1) uniform RaycastParams
{
    vec4 View;
    vec4 Counts;
    vec4 ShadowCounts;
    vec4 Origin;
    vec4 StartDirection;
    vec4 XDelta;
    vec4 YDelta;
    vec4 LightDirectionIntensity;
    vec4 LightSettings;
    vec4 SkyColor;
} Params;

layout(std430, set = 0, binding = 2) readonly buffer TriangleBuffer
{
    Triangle Triangles[];
};

layout(std430, set = 0, binding = 3) readonly buffer ShadowTriangleBuffer
{
    Triangle ShadowTriangles[];
};

layout(std430, set = 0, binding = 4) readonly buffer TextureInfoBuffer
{
    TextureInfo TextureInfos[];
};

layout(std430, set = 0, binding = 5) readonly buffer TexturePixelBuffer
{
    vec4 TexturePixels[];
};

vec3 normalizeOr(vec3 value, vec3 fallback)
{
    float lengthSquared = dot(value, value);
    return lengthSquared <= Epsilon * Epsilon ? fallback : value * inversesqrt(lengthSquared);
}

bool intersectTriangle(vec3 origin, vec3 direction, Triangle triangle, float maxDistance, out float distance, out float u, out float v)
{
    vec3 edge1 = triangle.B.xyz - triangle.A.xyz;
    vec3 edge2 = triangle.C.xyz - triangle.A.xyz;
    vec3 p = cross(direction, edge2);
    float determinant = dot(edge1, p);

    if (abs(determinant) <= Epsilon)
    {
        distance = 0.0;
        u = 0.0;
        v = 0.0;
        return false;
    }

    float inverseDeterminant = 1.0 / determinant;
    vec3 t = origin - triangle.A.xyz;
    u = dot(t, p) * inverseDeterminant;
    if (u < 0.0 || u > 1.0)
    {
        distance = 0.0;
        v = 0.0;
        return false;
    }

    vec3 q = cross(t, edge1);
    v = dot(direction, q) * inverseDeterminant;
    if (v < 0.0 || u + v > 1.0)
    {
        distance = 0.0;
        return false;
    }

    distance = dot(edge2, q) * inverseDeterminant;
    return distance > Epsilon && distance < maxDistance;
}

int textureMipOffset(TextureInfo info, int level)
{
    int offset = int(info.SizeOffset.z);
    int width = max(1, int(info.SizeOffset.x));
    int height = max(1, int(info.SizeOffset.y));

    for (int i = 0; i < level; i++)
    {
        offset += width * height;
        width = max(1, width / 2);
        height = max(1, height / 2);
    }

    return offset;
}

vec4 sampleTextureLevel(TextureInfo info, vec2 uv, int level)
{
    int textureWidth = max(1, int(info.SizeOffset.x));
    int textureHeight = max(1, int(info.SizeOffset.y));
    for (int i = 0; i < level; i++)
    {
        textureWidth = max(1, textureWidth / 2);
        textureHeight = max(1, textureHeight / 2);
    }

    int textureOffset = textureMipOffset(info, level);
    int x = clamp(int(floor(uv.x * float(textureWidth))), 0, textureWidth - 1);
    int y = clamp(int(floor((1.0 - uv.y) * float(textureHeight))), 0, textureHeight - 1);
    return TexturePixels[textureOffset + y * textureWidth + x];
}

vec4 sampleTexture(TextureInfo info, vec2 uv, float lod)
{
    int mipCount = max(1, int(info.SizeOffset.w));
    float clampedLod = clamp(lod, 0.0, float(mipCount - 1));
    int level0 = int(floor(clampedLod));
    int level1 = min(level0 + 1, mipCount - 1);
    float blend = clampedLod - float(level0);

    vec4 a = sampleTextureLevel(info, uv, level0);
    vec4 b = sampleTextureLevel(info, uv, level1);
    return mix(a, b, blend);
}

float estimateTextureLod(Triangle triangle, vec3 direction, float distance, TextureInfo info)
{
    vec2 uvA = triangle.UvA_UvB.xy;
    vec2 uvB = triangle.UvA_UvB.zw;
    vec2 uvC = triangle.UvC_Material.xy;
    vec2 tiling = triangle.TextureTransform.xy;

    vec3 edgeAB = triangle.B.xyz - triangle.A.xyz;
    vec3 edgeAC = triangle.C.xyz - triangle.A.xyz;
    vec2 uvAB = (uvB - uvA) * tiling;
    vec2 uvAC = (uvC - uvA) * tiling;
    float uvPerWorld = max(
        length(uvAB) / max(length(edgeAB), Epsilon),
        length(uvAC) / max(length(edgeAC), Epsilon));

    float pixelAngle = max(length(Params.XDelta.xyz), length(Params.YDelta.xyz));
    float surfaceAngle = max(abs(dot(normalizeOr(triangle.Normal.xyz, vec3(0.0, 1.0, 0.0)), -direction)), 0.05);
    float worldFootprint = distance * pixelAngle / surfaceAngle;
    float textureSize = max(info.SizeOffset.x, info.SizeOffset.y);
    float texelFootprint = max(worldFootprint * uvPerWorld * textureSize, 1.0);
    return log2(texelFootprint);
}

SurfaceSample sampleTriangle(Triangle triangle, float u, float v, vec3 direction, float distance, bool useMipLod)
{
    vec2 uvA = triangle.UvA_UvB.xy;
    vec2 uvB = triangle.UvA_UvB.zw;
    vec2 uvC = triangle.UvC_Material.xy;
    vec2 uv = uvA + (uvB - uvA) * u + (uvC - uvA) * v;

    vec4 color = triangle.Color;
    if (triangle.Material.y > 0.5)
    {
        int textureIndex = int(triangle.UvC_Material.z);
        TextureInfo info = TextureInfos[textureIndex];
        vec2 textureUv = fract(uv * triangle.TextureTransform.xy + triangle.TextureTransform.zw);
        float textureLod = useMipLod ? estimateTextureLod(triangle, direction, distance, info) : 0.0;
        vec4 texel = sampleTexture(info, textureUv, textureLod);
        color.rgb *= texel.rgb;
        color.a *= texel.a;
    }

    SurfaceSample surface;
    surface.Color = color.rgb;
    surface.Alpha = clamp(color.a, 0.0, 1.0);
    surface.AlphaCutoff = clamp(triangle.Material.x, 0.0, 1.0);
    surface.Shader = int(triangle.UvC_Material.w);
    return surface;
}

void castScene(vec3 origin, vec3 direction, float maxDistance, out Hit closest)
{
    closest.HitSomething = false;
    closest.Distance = maxDistance;
    closest.Point = vec3(0.0);
    closest.Normal = vec3(0.0, 1.0, 0.0);
    closest.Color = vec3(0.0);
    closest.Alpha = 1.0;
    closest.Shader = 0;

    int triangleCount = int(Params.Counts.x);
    for (int i = 0; i < triangleCount; i++)
    {
        float distance;
        float u;
        float v;
        if (!intersectTriangle(origin, direction, Triangles[i], closest.Distance, distance, u, v))
        {
            continue;
        }

        SurfaceSample surface = sampleTriangle(Triangles[i], u, v, direction, distance, true);
        if (surface.Alpha <= surface.AlphaCutoff)
        {
            continue;
        }

        closest.HitSomething = true;
        closest.Distance = distance;
        closest.Point = origin + direction * distance;
        closest.Normal = normalizeOr(Triangles[i].Normal.xyz, vec3(0.0, 1.0, 0.0));
        if (dot(closest.Normal, direction) > 0.0)
        {
            closest.Normal = -closest.Normal;
        }

        closest.Color = surface.Color;
        closest.Alpha = surface.Alpha;
        closest.Shader = surface.Shader;
    }
}

bool intersectsShadowCaster(vec3 origin, vec3 direction, float maxDistance)
{
    int triangleCount = int(Params.ShadowCounts.x);
    for (int i = 0; i < triangleCount; i++)
    {
        float distance;
        float u;
        float v;
        if (!intersectTriangle(origin, direction, ShadowTriangles[i], maxDistance, distance, u, v))
        {
            continue;
        }

        SurfaceSample surface = sampleTriangle(ShadowTriangles[i], u, v, direction, distance, false);
        if (surface.Alpha > max(surface.AlphaCutoff, 0.5))
        {
            return true;
        }
    }

    return false;
}

bool isShadowed(Hit hit, ivec2 pixel, vec3 lightDirection)
{
    int shadowQuality = int(Params.Counts.w);
    float shadowIntensity = Params.LightSettings.y;
    int shadowTriangleCount = int(Params.ShadowCounts.x);

    if (shadowQuality == 0 ||
        shadowIntensity <= 0.0 ||
        dot(hit.Normal, lightDirection) <= Epsilon ||
        shadowTriangleCount <= 0)
    {
        return false;
    }

    if (shadowQuality == 1 && ((pixel.x + pixel.y) & 1) != 0)
    {
        return false;
    }

    float shadowBias = Params.LightSettings.z;
    float shadowMaxDistance = Params.LightSettings.w;
    vec3 shadowOrigin = hit.Point + hit.Normal * shadowBias + lightDirection * shadowBias;
    return intersectsShadowCaster(shadowOrigin, lightDirection, shadowMaxDistance);
}

vec3 shade(Hit hit, ivec2 pixel)
{
    if (hit.Shader == 1)
    {
        return hit.Color;
    }

    float maxDistance = Params.View.z;
    vec3 lightDirection = normalizeOr(Params.LightDirectionIntensity.xyz, vec3(0.0, 1.0, 0.0));
    float lightIntensity = max(Params.LightDirectionIntensity.w, 0.0);
    float ambientIntensity = clamp(Params.LightSettings.x, 0.0, 1.0);
    float shadowIntensity = clamp(Params.LightSettings.y, 0.0, 1.0);

    float directLight = max(dot(hit.Normal, lightDirection), 0.0);
    if (directLight > 0.0 && isShadowed(hit, pixel, lightDirection))
    {
        directLight *= 1.0 - shadowIntensity;
    }

    float surfaceLight = clamp(ambientIntensity + directLight * lightIntensity, 0.0, 1.0);
    float depthFade = 1.0 - clamp(hit.Distance / maxDistance, 0.0, 1.0);
    float intensity = clamp(surfaceLight * depthFade + ambientIntensity / 2.0, 0.0, 1.0);
    return hit.Color * (0.25 + intensity * 0.75);
}

vec3 traceColor(vec3 origin, vec3 direction, ivec2 pixel)
{
    const int MaxLayers = 3;
    float maxDistance = Params.View.z;
    float remainingDistance = maxDistance;
    float transmittance = 1.0;
    vec3 accumulated = vec3(0.0);
    vec3 currentOrigin = origin;

    for (int layer = 0; layer < MaxLayers; layer++)
    {
        if (transmittance <= 0.01 || remainingDistance <= Epsilon)
        {
            break;
        }

        Hit hit;
        castScene(currentOrigin, direction, remainingDistance, hit);
        if (!hit.HitSomething)
        {
            accumulated += Params.SkyColor.rgb * transmittance;
            transmittance = 0.0;
            break;
        }

        float alpha = clamp(hit.Alpha, 0.0, 1.0);
        accumulated += shade(hit, pixel) * alpha * transmittance;
        transmittance *= 1.0 - alpha;
        currentOrigin = hit.Point + direction * Epsilon * 4.0;
        remainingDistance -= hit.Distance + Epsilon * 4.0;
    }

    if (transmittance > 0.0)
    {
        accumulated += Params.SkyColor.rgb * transmittance;
    }

    return accumulated;
}

void main()
{
    ivec2 pixel = ivec2(gl_GlobalInvocationID.xy);
    int width = int(Params.View.x);
    int height = int(Params.View.y);
    if (pixel.x >= width || pixel.y >= height)
    {
        return;
    }

    vec3 direction = normalizeOr(
        Params.StartDirection.xyz +
        Params.XDelta.xyz * float(pixel.x) +
        Params.YDelta.xyz * float(pixel.y),
        vec3(0.0, 0.0, 1.0));

    vec3 color = traceColor(Params.Origin.xyz, direction, pixel);
    imageStore(OutputTexture, pixel, vec4(color, 1.0));
}

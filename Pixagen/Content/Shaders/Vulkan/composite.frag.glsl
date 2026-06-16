#version 450

layout(set = 0, binding = 0) uniform texture2D SceneTexture;
layout(set = 0, binding = 1) uniform sampler SceneSampler;
layout(set = 0, binding = 2) uniform texture2D OverlayTexture;
layout(set = 0, binding = 3) uniform sampler OverlaySampler;

layout(location = 0) in vec2 fsin_UV;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    vec4 scene = texture(sampler2D(SceneTexture, SceneSampler), fsin_UV);
    vec4 overlay = texture(sampler2D(OverlayTexture, OverlaySampler), fsin_UV);
    fsout_Color = mix(scene, overlay, overlay.a);
}

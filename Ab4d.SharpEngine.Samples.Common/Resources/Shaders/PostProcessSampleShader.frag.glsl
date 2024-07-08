#version 450
#define VULKAN 100
#pragma shader_stage(fragment)

layout(set = 0, binding = 0) uniform sampler2D inTexture;

layout(location = 0) in vec2 inUV;

layout(location = 0) out vec4 outColor;

void main()
{
    outColor = vec4(0.5, 0.0, 0.0, 0.0) + texture(inTexture, inUV);
}
#version 460
#pragma shader_stage(vertex)

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUv;
layout(location = 3) in vec4 inColor;

// In the future this stuct may change
layout(set = 0, binding = 0) uniform Scene {
    mat4 viewProjection;
} scene;

layout(set = 2, binding = 0) readonly buffer Matrices {
    mat4 world[];
} matrices;

layout(push_constant) uniform PushConstants {
    int matrixIndex;
} pushConstants;

layout(location = 0) out vec3 outPosition;
layout(location = 1) out vec3 outNormal;
layout(location = 2) out vec2 outUv;
layout(location = 3) out vec4 outColor;

void main() {
    mat4 toWorld = matrices.world[pushConstants.matrixIndex];

    vec4 worldPosition = toWorld * vec4(inPosition, 1.0);
    gl_Position = scene.viewProjection * worldPosition;

    outPosition = worldPosition.xyz;
    outNormal = (toWorld * vec4(inNormal, 0.0)).xyz;
    outUv = inUv;
    outColor = inColor;
}

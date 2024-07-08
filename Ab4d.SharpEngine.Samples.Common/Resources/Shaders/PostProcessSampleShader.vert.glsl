#version 450
#pragma shader_stage(vertex)

layout(set = 0, binding = 0) uniform Scene {
    mat4 viewProjection;
} scene;

layout(set = 1, binding = 0) readonly buffer Matrices {
    mat4 world[];
} matrices;

layout(push_constant) uniform PushConstants {
    int matrixIndex;
} pushConstants;

layout(location = 0) in vec3 inPosition;

void main()
{
    gl_Position = (scene.viewProjection * matrices.world[pushConstants.matrixIndex]) * vec4(inPosition, 1.0);
}
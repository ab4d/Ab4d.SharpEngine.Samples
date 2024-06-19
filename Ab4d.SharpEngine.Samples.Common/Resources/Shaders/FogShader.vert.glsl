#version 450

#pragma shader_stage(vertex)


layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inNormal;
layout (location = 2) in vec2 inUV;


// Note that in the future, this stuct may change
layout(set = 0, binding = 0) uniform sceneUniformBuffer {
    mat4 viewProjection;

    vec3 eyePosW;

	vec2 viewSize;
	float dpiScale;
	float superSampling;

	vec3 ambientColor;	

	int dirLightStart; // start index of Directional lights in allLights array
	int dirLightCount;
	
	int pointLightStart; // start index of Point lights in allLights array
	int pointLightCount;
	
	int spotLightStart; // start index of Spot lights in allLights array
	int spotLightCount;
} scene;

//layout(set = 1, binding = 0) readonly buffer AllLightsBuffer { // used in fragment shader

layout(set = 2, binding = 0) readonly buffer modelMatricesBuffer {
  mat4 world[];
} matricesBuffer;

//layout(set = 3, binding = 0) readonly buffer StandardMaterialsBuffer { // used in fragment shader


// matrix index is defined by using push_constant
layout(push_constant) uniform indexSetup {
  layout(offset = 0) int matrixIndex;
} pushConstants;


out gl_PerVertex 
{
	vec4 gl_Position;
};


layout (location = 0) out vec3 outWorldPos;
layout (location = 1) out vec3 outNormal;
layout (location = 2) out vec2 outUV;


/* vertex shader with fixed matrices:

void main() 
{

    // viewProjection get from the following camera:
    // TargetPositionCamera:
    // ProjectionType: Perspective
    // Heading: -40
    // Attitude: -25
    // TargetPosition: 0 0 0
    // Distance: 1500
    // FieldOfView: 45
    // ViewWidth: 1000
    // NearPlaneDistance: 1242.3704
    // FarPlaneDistance: 1780.287
    // CameraPosition: 874 634 1,041
    // LookDirection: -0.58 -0.42 -0.69
    // UpDirection: -0.27 0.91 -0.32
    // AspectRatio: 1.5035886

    mat4 viewProjection = mat4(1.85,  -0.99,   -1.93,    -0.58,  
                               0.00,   3.29,   -1.40,    -0.42,  
                              -1.55,  -1.18,   -2.30,    -0.69,  
                              -0.00,  -0.00,  852.65,  1500.00);

    viewProjection = transpose(viewProjection);

    
    mat4 world = mat4(1.00, 0.00, 0.00, 0.00,
                      0.00, 1.00, 0.00, 0.00,
                      0.00, 0.00, 1.00, 0.00,
                      0.00, 0.00, 0.00, 1.00);

    mat4 wvp = viewProjection * world;
    gl_Position = wvp * vec4(inPosition, 1.0);

    outNormal = (world * vec4(inNormal, 0.0)).xyz;

    outUV = inUV;
}

*/

// Vertex shader that use scene and model matrices

void main()
{
    mat4 modelWorld = matricesBuffer.world[pushConstants.matrixIndex];

    vec4 posWorld4 = modelWorld * vec4(inPosition, 1.0);

    gl_Position = scene.viewProjection * posWorld4;

    outWorldPos = posWorld4.xyz;

    outNormal = (modelWorld * vec4(inNormal, 0.0)).xyz;

    outUV = inUV;
}

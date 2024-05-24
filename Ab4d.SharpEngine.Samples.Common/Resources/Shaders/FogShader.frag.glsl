#version 450

#pragma shader_stage(fragment)

layout (location = 0) in vec3 inPosW;
layout (location = 1) in vec3 inNormalW;
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


struct Light
{
	vec3 pos;
	float phi;                // phi = cos(OuterConeAngleInRad / 2)
	vec3 dir;
	float theta;              // theta = cos(InnerConeAngleInRad / 2)
	vec3 diffuse;
	float falloff;           // 1 = linear (see http://msdn.microsoft.com/en-us/library/windows/desktop/bb174697%28v=vs.85%29.aspx)
	vec3 spec;
	vec3 att;
	float range;
};

layout(set = 1, binding = 0) readonly buffer AllLightsBuffer {
  Light allLights[];
} allLightsBuffer;

// used in vertex shader:
// layout(set = 2, binding = 0) readonly buffer modelMatricesBuffer {
//   mat4 world[];
// } matricesBuffer;

struct FogMaterial
{
    vec4 diffuseColor;

	float fogStart;
	float fogFullColorStart;
	vec3 fogColor;
};

layout(set = 3, binding = 0) readonly buffer FogMaterialsBuffer {
  FogMaterial materials[];
} materialsBuffer;


layout(push_constant) uniform indexSetup {
  layout(offset = 4) int materialIndex;
} pushConstants;


layout(location = 0) out vec4 outColor;


/* fragment shader with fixed color 

void main() 
{
    //outColor = vec4(inNormal, 1);
    outColor = vec4(0, 1, 0, 1);
}
*/


void main() 
{
    int usedMaterialIndex = abs(pushConstants.materialIndex);
	FogMaterial material = materialsBuffer.materials[usedMaterialIndex];
	
	vec4 diffuseColor = material.diffuseColor;

	//vec4 diffuseColor = vec4(0, 1, 0, 1);

	
	//// When pushConstants.materialIndex is negative, we need to multiply normal by minus 1.
	float multiplyNormal = sign(pushConstants.materialIndex);
	//
	//// Interpolating normal can unnormalize it, so normalize it; then we multiply by multiplyNormal
	vec3 normal = normalize(inNormalW) * multiplyNormal;

	//vec3 normal = normalize(inNormalW);

	vec3 toEye = normalize(scene.eyePosW - inPosW);	
	
	vec3 finalDiffuseColor = vec3(0, 0, 0);

	//directional lights
    for(int iDir = 0; iDir < scene.dirLightCount; iDir++)
    {
	    Light oneLightInfo = allLightsBuffer.allLights[iDir + scene.dirLightStart];
	
		// The light vector aims opposite the direction the light rays travel.
		vec3 lightVec = -(oneLightInfo.dir);
		
		float diffuseFactor = dot(lightVec, normal);

		if(diffuseFactor > 0.0f)
			finalDiffuseColor = diffuseFactor * oneLightInfo.diffuse;
    }

	// Add ambient color and multiply by the material's color
	finalDiffuseColor  = clamp(finalDiffuseColor + scene.ambientColor, 0, 1) * diffuseColor.xyz;


	// Add fog

	// Get distance from the current point to camera (in world coordinates)
	float distanceW = length(scene.eyePosW - inPosW);

	// Fog settings (in world coordinates)
	//float fogStart = 150.0; // fog starts when objects are 150 units from camera
	//float fogEndW = 220.0;   // full color fog start at 220 units
	//float3 fogColor = float3(1, 1, 1); // fog color in RGB

	// interpolate from 0 to 1: 0 starting at fogStart and 1 at fogEnd 
	// saturate clamps the specified value within the range of 0 to 1.
	float fogFactor = clamp((distanceW - material.fogStart) / (material.fogFullColorStart - material.fogStart), 0, 1);

	// lerp lineary interpolates the color
	finalDiffuseColor = mix(finalDiffuseColor, material.fogColor, fogFactor);


	// Add alpha
	outColor = clamp(vec4(finalDiffuseColor, diffuseColor.a), 0, 1);
}
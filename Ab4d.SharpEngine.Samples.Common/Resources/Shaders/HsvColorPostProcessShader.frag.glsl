#version 450
#extension GL_ARB_shading_language_include : require

#pragma shader_stage(fragment)

// Based on https://stackoverflow.com/questions/15095909/from-rgb-to-hsv-in-opengl-glsl


layout (location = 0) in vec2 inUV;

layout (location = 0) out vec4 outColor;

layout (set = 1, binding = 0) uniform sampler2D inputTexture;


// All components are in the range [0�1], including hue.
vec3 rgb2hsv(vec3 c)
{
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// All components are in the range [0�1], including hue.
vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

layout(push_constant) uniform postProcessSetup {
	layout(offset = 0) float hueOffset;
	layout(offset = 4) float saturationFactor;
	layout(offset = 8) float brightnessFactor;
} pushConstants;

void main() 
{
	vec4 color = texture(inputTexture, inUV);

    vec3 hsv = rgb2hsv(color.rgb);
    hsv = vec3(hsv.x + pushConstants.hueOffset, hsv.y * pushConstants.saturationFactor, hsv.z * pushConstants.brightnessFactor);

    vec3 rgb = hsv2rgb(hsv);

	outColor = vec4(clamp(rgb, 0, 1), color.a);
}

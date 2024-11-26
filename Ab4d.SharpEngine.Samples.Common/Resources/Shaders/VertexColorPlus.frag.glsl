#version 460
#pragma shader_stage(fragment)

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUv;
layout(location = 3) in vec4 inColor;

layout(constant_id = 0) const bool USE_DIFFUSE_TEXTURE = false;
layout(constant_id = 1) const bool USE_VERTEX_COLORS = false;

// In the future this stuct may change
layout(set = 0, binding = 0) uniform Scene {
    mat4 viewProjection;
    vec3 eyePosW;

    vec2 viewSize;
    float dpiScale;
    float superSampling;

    vec3 ambientColor;
    int dirLightStart;
    int dirLightCount;
    int pointLightStart;
    int pointLightCount;
    int spotLightStart;
    int spotLightCount;
} scene;

struct Light
{
    vec3 pos;
    float phi;
    vec3 dir;
    float theta;
    vec3 diffuse;
    float falloff;
    vec3 spec;
    vec3 att;
    float range;
};
layout(set = 1, binding = 0) readonly buffer Lights {
    Light lights[];
} lights;

struct SurfaceInfo {
    vec3 pos;
    vec3 normal;
    float specPower;
    vec3 toEye;
};

struct ColorPair {
    vec3 diffuse;
    vec3 specular;
};

struct Mat {
    vec4 diffuseColor;
    vec3 specularColor;
    float specularPower;
    vec3 emissiveColor;
    float alphaClipThreshold;
    bool isTwoSided;
    bool isSolidColor;
    bool blendVertexColors;
    float vertexColorsOpacity;
};
layout(set = 3, binding = 0) readonly buffer Materials {
    Mat materials[];
} materials;

layout(set = 3, binding = 1) uniform sampler2D diffuseTextureSampler;

layout(push_constant) uniform PushConstants {
    layout(offset = 4) int materialIndex;
} pushConstants;

ColorPair DirectionalLight(SurfaceInfo v, Light L)
{
    ColorPair colors = ColorPair(vec3(0.0), vec3(0.0));
    vec3 lightVec = -L.dir;
    float diffuseFactor = dot(lightVec, v.normal);
    if (diffuseFactor > 0.0) {
        colors.diffuse = L.diffuse * diffuseFactor;
        if (v.specPower > 0.0) {
            vec3 halfDir = normalize(lightVec + v.toEye);
            float specFactor = pow(clamp(dot(v.normal, halfDir), 0.0, 1.0), v.specPower);
            colors.specular = L.spec * specFactor;
        }
    }
    return colors;
}

ColorPair PointLight(SurfaceInfo v, Light L)
{
    ColorPair colors = ColorPair(vec3(0.0), vec3(0.0));
    vec3 lightVec = L.pos - v.pos;
    float d = length(lightVec);
    if (d > L.range) {
        return colors;
    }
    else {
        lightVec /= vec3(d);
        float attenuation = dot(L.att, vec3(1.0, d, d * d));
        float diffuseFactor = dot(lightVec, v.normal);
        if (diffuseFactor > 0.0) {
            colors.diffuse = L.diffuse * diffuseFactor;
            if (v.specPower > 0.0) {
                vec3 halfDir = normalize(lightVec + v.toEye);
                float specFactor = pow(clamp(dot(v.normal, halfDir), 0.0, 1.0), v.specPower);
                colors.specular = L.spec * specFactor;
            }
        }
        colors.diffuse /= vec3(attenuation);
        return colors;
    }
}

ColorPair Spotlight(SurfaceInfo v, Light L)
{
    ColorPair colors = ColorPair(vec3(0.0), vec3(0.0));
    vec3 lightVec = L.pos - v.pos;
    float d = length(lightVec);
    if (d > L.range) {
        return colors;
    }
    else {
        lightVec /= vec3(d);
        float attenuation = dot(L.att, vec3(1.0, d, d * d));
        float diffuseFactor = dot(lightVec, v.normal);
        if (diffuseFactor > 0.0) {
            colors.diffuse = L.diffuse * diffuseFactor;
            if (v.specPower > 0.0) {
                vec3 halfDir = normalize(lightVec + v.toEye);
                float specFactor = pow(clamp(dot(v.normal, halfDir), 0.0, 1.0), v.specPower);
                colors.specular = L.spec * specFactor;
            }
        }
        colors.diffuse /= vec3(attenuation);
        float rho = dot(L.dir, -lightVec);
        float s;
        if (rho <= L.phi) {
            s = 0.0;
        }
        else {
            if (rho > L.theta) {
                s = 1.0;
            }
            else {
                s = pow(max((rho - L.phi) / (L.theta - L.phi), 0.0), L.falloff);
            }
        }
        colors.diffuse *= s;
        colors.specular *= s;
        return colors;
    }
}

layout(location = 0) out vec4 outColor;

void main() {
    int materialSign = sign(pushConstants.materialIndex);

    // Blending of diffuse color, texture and vertex colors

    Mat material = materials.materials[pushConstants.materialIndex * materialSign];
    if (material.blendVertexColors || !USE_VERTEX_COLORS) {
        if (USE_DIFFUSE_TEXTURE) {
            outColor = texture(diffuseTextureSampler, inUv);
            outColor.rgb = outColor.a * outColor.rgb + (1.0 - outColor.a) * material.diffuseColor.rgb;
            outColor.a += material.diffuseColor.a;
        }
        else {
            outColor = material.diffuseColor;
        }

        if (USE_VERTEX_COLORS) {
            float inAlpha = inColor.a * material.vertexColorsOpacity;
            outColor.rgb = inAlpha * inColor.rgb + (1.0 - inAlpha) * outColor.rgb;
            outColor.a += inAlpha;
        }
    }
    else {
        outColor = inColor;
        outColor.a *= material.vertexColorsOpacity;
    }

    // Lighting

    if (!material.isSolidColor) {
        float multiplyNormal = float(materialSign);
        vec3 normal = normalize(inNormal) * multiplyNormal;
        vec3 toEye = normalize(scene.eyePosW - inPosition);
        SurfaceInfo surface = SurfaceInfo(inPosition, normal, material.specularPower, toEye);
        vec3 finalDiffuse = vec3(0.0f);
        vec3 finalSpecular = vec3(0.0f);
        int i;
        Light light;
        ColorPair lightColors;
        for (i = scene.dirLightCount - 1; i >= scene.dirLightStart; i--) {
            light = lights.lights[i];
            lightColors = DirectionalLight(surface, light);
            finalDiffuse += lightColors.diffuse;
            finalSpecular += lightColors.specular;
        }
        for (i = scene.pointLightCount - 1; i >= scene.pointLightStart; i--) {
            light = lights.lights[i];
            lightColors = PointLight(surface, light);
            finalDiffuse += lightColors.diffuse;
            finalSpecular += lightColors.specular;
        }
        for (i = scene.spotLightCount - 1; i >= scene.spotLightStart; i--) {
            light = lights.lights[i];
            lightColors = Spotlight(surface, light);
            finalDiffuse += lightColors.diffuse;
            finalSpecular += lightColors.specular;
        }
        finalDiffuse = clamp(finalDiffuse + scene.ambientColor, vec3(0.0), vec3(1.0)) * outColor.rgb;
        finalSpecular = clamp(finalSpecular, vec3(0.0), vec3(1.0)) * material.specularColor;
        vec3 finalColor = (finalDiffuse + finalSpecular) + material.emissiveColor;
        outColor.rgb = clamp(finalColor, vec3(0.0), vec3(1.0));
    }
}

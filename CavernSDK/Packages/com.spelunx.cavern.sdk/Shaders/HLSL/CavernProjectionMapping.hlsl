// Header Guards
#ifndef CAVERN_PROJECTION_MAPPING_HLSL
#define CAVERN_PROJECTION_MAPPING_HLSL

// Define Built-In Macros
// #define SHADERPASS SHADERPASS_BLIT

// Include URP library functions.
// URP library functions can be found via the Unity Editor in "Packages/Universal RP/Shader Library/".
// HLSL shader files for URP are in the "Packages/com.unity.render-pipelines.universal/ShaderLibrary/" directory in your project.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Textures Uniforms
TEXTURECUBE(_CubemapNorth); // Also used for monoscopic rendering.
SAMPLER(sampler_CubemapNorth);
float4 _CubemapNorth_ST;

TEXTURECUBE(_CubemapSouth);
SAMPLER(sampler_CubemapSouth);
float4 _CubemapSouth_ST;

TEXTURECUBE(_CubemapEast);
SAMPLER(sampler_CubemapEast);
float4 _CubemapEast_ST;

TEXTURECUBE(_CubemapWest);
SAMPLER(sampler_CubemapWest);
float4 _CubemapWest_ST;

// Cavern Dimensions Uniforms
float _CavernHeight;
float _CavernRadius;
float _CavernAngle;
float _CavernElevation;

// Head Tracking Uniforms
float3 _HeadPosition;

// Stereoscopic Rendering Uniforms
int _EnableStereoscopic;
int _EnableConvergence; // Add a toggle for convergence because it results in more faces needing to be rendered for honestly not much visible difference.
float _InterpupillaryDistance;
int _SwapEyes;

// Vertex attributes.
// Fields are automatically populated according to their semantic. (https://docs.unity3d.com/Manual/SL-VertexProgramInputs.html)
struct CustomAttributes { // We can name this struct anything we want.
    float3 positionOS : POSITION; // Position in object space.
    float2 uv : TEXCOORD0; // Material texture UVs.
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Data passed from the vertex function to the fragment function.
struct Vert2Frag { // We can name this struct anything we want.
    float4 positionCS : SV_POSITION; // Clip space position must have the semantics SV_POSITION.
    float2 uv : TEXCOORD0; // Render texture UV coordinates.
    UNITY_VERTEX_OUTPUT_STEREO
};

// The vertex function, runs once per vertex.
Vert2Frag Vertex(CustomAttributes input) {
    // Helper function from ShaderVariableFunctions.hlsl in the URP package
    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);

    // Since this shader is only ever used for blitting, use FullScreenTriangle functions.
    Vert2Frag output;
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
    return output;
}

float4 SampleLeftEye(float3 headToScreen, float fragmentRelativeAngle, float3 ipdOffsetZ, float3 ipdOffsetX) {
    // Physcial screen rear quadrant relative to head position.
    if (fragmentRelativeAngle > 135.0f || fragmentRelativeAngle < -135.0f) {
        return SAMPLE_TEXTURECUBE(_CubemapEast, sampler_CubemapEast, _EnableConvergence ? (headToScreen - ipdOffsetX) : headToScreen);
    }
    // Physcial screen left quadrant relative to head position.
    if (fragmentRelativeAngle < -45.0f) {
        return SAMPLE_TEXTURECUBE(_CubemapSouth, sampler_CubemapSouth, _EnableConvergence ? (headToScreen + ipdOffsetZ) : headToScreen);
    }
    // Physcial screen right quadrant relative to head position.
    if (fragmentRelativeAngle > 45.0f)  {
        return SAMPLE_TEXTURECUBE(_CubemapNorth, sampler_CubemapNorth, _EnableConvergence ? (headToScreen - ipdOffsetZ) : headToScreen);
    }
    // Physcial screen front quadrant relative to head position.
    return SAMPLE_TEXTURECUBE(_CubemapWest, sampler_CubemapWest, _EnableConvergence ? (headToScreen + ipdOffsetX) : headToScreen);
}

float4 SampleRightEye(float3 headToScreen, float fragmentRelativeAngle, float3 ipdOffsetZ, float3 ipdOffsetX) {
    // Physcial screen rear quadrant relative to head position.
    if (fragmentRelativeAngle > 135.0f || fragmentRelativeAngle < -135.0f) {
        return SAMPLE_TEXTURECUBE(_CubemapWest, sampler_CubemapWest, _EnableConvergence ? (headToScreen + ipdOffsetX) : headToScreen);
    }
    // Physcial screen left quadrant relative to head position.
    if (fragmentRelativeAngle < -45.0f) {
        return SAMPLE_TEXTURECUBE(_CubemapNorth, sampler_CubemapNorth, _EnableConvergence ? (headToScreen - ipdOffsetZ) : headToScreen);
    }
    // Physcial screen right quadrant relative to head position.
    if (fragmentRelativeAngle > 45.0f) {
        return SAMPLE_TEXTURECUBE(_CubemapSouth, sampler_CubemapSouth, _EnableConvergence ? (headToScreen + ipdOffsetZ) : headToScreen);
    }
    // Physcial screen front quadrant relative to head position.
    return SAMPLE_TEXTURECUBE(_CubemapEast, sampler_CubemapEast, _EnableConvergence ? (headToScreen - ipdOffsetX) : headToScreen);
}

// The fragment function, runs once per pixel on the screen.
// It must have a float4 return type and have the SV_TARGET semantic.
float4 Fragment(Vert2Frag input) : SV_TARGET {
    // Split the screen into 2 halves, top and bottom.
    // For stereoscopic rendering, the top will render the left eye, the bottom will render the right eye.
    // For monoscopic rendering, both halves will render the same thing.
    const bool isLeftEye = 0.5f < input.uv.y;
    // For the left eye, convert the UV's y component from the [0.5, 1] range to the [0, 1] range.
    // For the right eye, convert the UV's y component from the [0, 0.5] range to the [0, 1] range.
    const float2 uv = float2(input.uv.x, isLeftEye ? (input.uv.y - 0.5) * 2.0f : input.uv.y * 2.0f);

    // Find the angle of the fragment on screen. Take note that angle 0 points down the Z-axis, not the X-axis.
    const float fragmentAngle = (uv.x * 2.0f - 1.0f) * _CavernAngle * 0.5f; // (uv.x * 2 - 1) converts the uv.x from the [0, 1] range to the [-1, 1] range.
    // Find the direction from the head to the fragment.
    const float3 headToScreen = float3(_CavernRadius * sin(radians(fragmentAngle)),
                                       _CavernElevation + _CavernHeight * uv.y,
                                       _CavernRadius * cos(radians(fragmentAngle))) - _HeadPosition;

    // Monoscopic mode.
    if (!_EnableStereoscopic) {
        return SAMPLE_TEXTURECUBE(_CubemapNorth, sampler_CubemapNorth, headToScreen);
    }

    // Stereoscopic mode.
    const float3 ipdOffsetZ = float3(0.0f, 0.0f, _InterpupillaryDistance * 0.5f);
    const float3 ipdOffsetX = float3(_InterpupillaryDistance * 0.5f, 0.0f, 0.0f);
    
    // Because the head might not be in the centre of the screen, we want to find the fragment's angle relative to the head's position, not the centre.
    const float3 forwardDir = float3(0.0f, 0.0f, 1.0f);
    const float3 headToScreenXZ = normalize(float3(headToScreen.x, 0.0f, headToScreen.z));
    const float fragmentRelativeAngle = degrees(acos(dot(forwardDir, headToScreenXZ))) * ((0.0f < headToScreenXZ.x) ? 1.0f : -1.0f);

    return (!_SwapEyes && isLeftEye) || (_SwapEyes && !isLeftEye)
        ? SampleLeftEye(headToScreen, fragmentRelativeAngle, ipdOffsetZ, ipdOffsetX)
        : SampleRightEye(headToScreen, fragmentRelativeAngle, ipdOffsetZ, ipdOffsetX);
}

#endif // CAVERN_PROJECTION_MAPPING_HLSL
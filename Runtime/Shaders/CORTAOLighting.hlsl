#ifndef UNITYCODER_CORTAO_LIGHTING_INCLUDED
#define UNITYCODER_CORTAO_LIGHTING_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if defined(_CORTAO_BENT_NORMALS)
TEXTURE2D_X(_CORTAOBentNormalTexture);
#endif

// normalizedScreenUV must be the current eye's normalized screen position.
// Call UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX in the fragment shader first.
float3 CORTAOBentNormalWS(float2 normalizedScreenUV, float3 normalWS, float strength)
{
    float3 n = normalize(normalWS);
#if defined(_CORTAO_BENT_NORMALS)
    float2 uv = UnityStereoTransformScreenSpaceTex(normalizedScreenUV);
    float3 bentVS = SAMPLE_TEXTURE2D_X(_CORTAOBentNormalTexture, sampler_PointClamp, uv).xyz * 2.0 - 1.0;
    float3 bentWS = mul((float3x3)UNITY_MATRIX_I_V, bentVS);
    float len2 = dot(bentWS, bentWS);
    if (len2 > 1e-6)
    {
        bentWS *= rsqrt(len2);
        // Do not allow a reconstructed direction below the receiving surface.
        if (dot(bentWS, n) > 0.0)
            return normalize(lerp(n, bentWS, saturate(strength)));
    }
#endif
    return n;
}

// For probe/SH diffuse ambient only. Do not replace baked lightmaps with this.
// Returns unoccluded irradiance; apply scalar AO once in the lighting path.
float3 CORTAODirectionalAmbient(float2 normalizedScreenUV, float3 normalWS, float strength)
{
    return SampleSH(CORTAOBentNormalWS(normalizedScreenUV, normalWS, strength));
}

// Per-pixel APV diffuse irradiance. positionSS is in raster pixels, not UV.
// The bent direction also affects APV's internal normal bias; use modest strength
// near thin walls. Returns irradiance before scalar AO, as with the SH helper.
float3 CORTAODirectionalProbeAmbient(float2 uv, float3 positionWS,
    float3 normalWS, float3 viewDirWS, float2 positionSS, float strength)
{
    float3 direction = CORTAOBentNormalWS(uv, normalWS, strength);
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    if (_EnableProbeVolumes)
    {
        float3 irradiance;
        EvaluateAdaptiveProbeVolume(GetAbsolutePositionWS(positionWS), direction,
            viewDirWS, positionSS, GetMeshRenderingLayer(), irradiance);
#if defined(UNITY_COLORSPACE_GAMMA)
        irradiance = LinearToSRGB(irradiance);
#endif
        return max(irradiance, 0.0);
    }
#endif
    return SampleSH(direction);
}

// Shader Graph custom function entry (File mode, float precision).
void CORTAODirectionalAmbient_float(float2 UV, float3 NormalWS, float Strength, out float3 Ambient)
{
    Ambient = CORTAODirectionalAmbient(UV, NormalWS, Strength);
}
#endif

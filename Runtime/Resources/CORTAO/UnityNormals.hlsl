#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#if defined(CORTAO_STEREO_INPUT)
Texture2DArray<float4> _Normals;
int _CORTAOEye;
#else
Texture2D<float4> _Normals;
#endif
float4x4 _View;
float3 ReadNormal(int2 p)
{
#if defined(CORTAO_STEREO_INPUT)
    float3 n = _Normals.Load(int4(p,_CORTAOEye,0)).xyz;
#else
    float3 n = _Normals.Load(int3(p,0)).xyz;
#endif
    #if defined(_GBUFFER_NORMALS_OCT)
        n = UnpackNormalOctQuadEncode(Unpack888ToFloat2(n) * 2.0 - 1.0);
    #endif
    n = mul((float3x3)_View,n);
    return all(isfinite(n)) && dot(n,n) > 1e-8 ? normalize(n) : float3(0,0,1);
}

Shader "Hidden/CORTAO/EarlyMotion"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "MonoDepth"
            Cull Back ZWrite Off ZTest Always Blend Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "EarlyMotionCommon.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "StereoDepth"
            Cull Back ZWrite Off ZTest Always Blend Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #define CORTAO_MOTION_STEREO_DEPTH 1
            #include "EarlyMotionCommon.hlsl"
            ENDHLSL
        }
    }
}

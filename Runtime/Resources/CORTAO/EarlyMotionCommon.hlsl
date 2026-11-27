#ifndef CORTAO_EARLY_MOTION_COMMON
#define CORTAO_EARLY_MOTION_COMMON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #if defined(CORTAO_MOTION_STEREO_DEPTH)
            Texture2DArray<float> _CORTAOMotionDepthArray;
            int _CORTAOMotionEye;
            #else
            TEXTURE2D_FLOAT(_CORTAOMotionDepth);
            #endif
            float4x4 _CORTAOMotionCurrentMVP, _CORTAOMotionPreviousMVP, _CORTAOMotionPreviousMV;
            float _CORTAOMotionObjectValid, _CORTAOMotionDepthTolerance, _CORTAOMotionSkinned;
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 previousCS : TEXCOORD0;
                float previousDepth : TEXCOORD1;
            };
            Varyings Vert(float3 positionOS : POSITION, float3 previousOS : TEXCOORD7)
            {
                Varyings o;
                float4 p = float4(positionOS, 1);
                o.positionCS = mul(_CORTAOMotionCurrentMVP, p);
                float4 oldP = _CORTAOMotionSkinned > 0.5 ? float4(previousOS, 1) : p;
                o.previousCS = mul(_CORTAOMotionPreviousMVP, oldP);
                o.previousDepth = -mul(_CORTAOMotionPreviousMV, oldP).z;
                return o;
            }
            float4 Frag(Varyings i) : SV_Target
            {
                #if defined(CORTAO_MOTION_STEREO_DEPTH)
                float sceneDepth = _CORTAOMotionDepthArray.Load(int4(int2(i.positionCS.xy), _CORTAOMotionEye, 0));
                #else
                float sceneDepth = LOAD_TEXTURE2D(_CORTAOMotionDepth, int2(i.positionCS.xy)).r;
                #endif
                // Match only fragments visible in the actual camera prepass.
                // The tolerance is in device depth, not world units.
                if (abs(sceneDepth - i.positionCS.z) > _CORTAOMotionDepthTolerance) discard;
                if (_CORTAOMotionObjectValid < 0.5 || i.previousCS.w <= 0 || i.previousDepth <= 0)
                    return float4(0,0,0,-1);
                float2 uv = i.previousCS.xy / i.previousCS.w * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1 - uv.y;
                #endif
                return float4(uv, i.previousDepth, 1);
            }
#endif

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityCoder.CORTAO
{
    [Serializable, VolumeComponentMenu("Lighting/CORTAO")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class CORTAOVolume : VolumeComponent
    {
        [Tooltip("Ambient occlusion intensity.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 4f);
        [Tooltip("World-space occlusion radius in meters.")]
        public MinFloatParameter radius = new MinFloatParameter(1f, 0.001f);
        [Tooltip("Amount of ambient occlusion applied to direct lighting.")]
        public ClampedFloatParameter directLightingStrength = new ClampedFloatParameter(0f, 0f, 1f);
        [Tooltip("Linear camera depth where fading begins. Requires Distance Fade on the Renderer Feature.")]
        public MinFloatParameter distanceFadeStart = new MinFloatParameter(50f, 0f);
        [Tooltip("Linear camera depth beyond which tracing is skipped. Requires Distance Fade on the Renderer Feature.")]
        public MinFloatParameter maxDistance = new MinFloatParameter(100f, 0.01f);
    }
}

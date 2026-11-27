using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityCoder.CORTAO.Editor
{
    [CustomEditor(typeof(CORTAORendererFeature))]
    [CanEditMultipleObjects]
    internal sealed class CORTAOInspector : UnityEditor.Editor
    {
        private static readonly string[] PresetNames = { "Choose preset...", "Performance", "Balanced", "High Quality", "Ultra" };
        private static readonly string[] GroupNames = { "Ambient occlusion", "Quality and denoising", "Advanced tracing", "Advanced filtering", "Motion tracking (experimental)", "XR and dynamic resolution", "Shader resources", "Integration and diagnostics" };
        private static readonly string[][] GroupFields = {
            new string[] { "useVolumeOverrides", "intensity", "radius", "directLightingStrength", "distanceFade", "distanceFadeStart", "maxDistance" },
            new string[] { "resolution", "quality", "rayCount", "lookupQuality", "temporal", "temporalAtTracingResolution", "spatialDenoiseIterations", "spatialDenoiseStrength", "outputBentNormals", "temporalBentNormals" },
            new string[] { "normalBiasMultiplier", "maxScreenRadius", "mipBias", "maxSlopeCompensation", "backfaceThreshold", "backfaceBoost", "randomSeed", "animateNoise", "useMipmappedPyramid", "forceFullPrecisionAO" },
            new string[] { "filterDepthSensitivity", "filterNormalSensitivity", "upsampleDepthThreshold", "upsampleNormalPower", "validatedBilinearHistory", "preserveHistoryOnResize", "historyResponse", "historyWeight", "historyDepthThreshold", "historyNormalThreshold", "historyClampExpansion", "historyVarianceExpansion" },
            new string[] { "earlyRigidMotion", "automaticRigidMotion", "skinnedMotion", "automaticSkinnedMotion", "customDeformedMotion", "automaticMotionLayers", "motionDeviceDepthTolerance", "enableXRMultipassMotion", "enableXRSinglePassMotion" },
            new string[] { "enableXRMultipass", "enableXRSinglePass", "enableXRMultiview", "directStereoAO", "directStereoBent", "enableHardwareDynamicResolution", "enableXRDynamicResolution", "enableXRViewportScaling", "foveationPolicy", "externalFoveationActive" },
            new string[] { "prepass", "trace", "filter", "upsample", "temporalShader", "stereoTransfer", "bentFilter", "earlyMotionShader", "debugShader", "lookupTable" },
            new string[] { "injectionPoint", "debugView", "logDiagnostics", "traceDiagnostics" }
        };
        private readonly bool[] expanded = { true, true, false, false, false, false, false, true };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var settings = serializedObject.FindProperty("settings");
            EditorGUILayout.LabelField("CORTAO", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Presets change quality and denoising only. Visual settings, Volume overrides, shader references and XR configuration are preserved. Custom LUT assignments also preserve ray count.", MessageType.Info);
            int preset = EditorGUILayout.Popup("Apply Quality Preset", 0, PresetNames);
            if (preset != 0) ApplyPreset(settings, preset);
            for (int i = 0; i < GroupNames.Length; ++i)
            {
                expanded[i] = EditorGUILayout.Foldout(expanded[i], GroupNames[i], true);
                if (!expanded[i]) continue;
                EditorGUI.indentLevel++;
                foreach (string name in GroupFields[i])
                {
                    var property = settings.FindPropertyRelative(name);
                    if (property != null) EditorGUILayout.PropertyField(property, true);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private static void ApplyPreset(SerializedProperty s, int preset)
        {
            // Use serialized numeric enum values, not enumValueIndex: Quality is non-contiguous.
            SetInt(s, "resolution", preset <= 2 ? (int)CORTAORendererFeature.Resolution.Half : (int)CORTAORendererFeature.Resolution.Full);
            SetInt(s, "quality", (int)(preset == 1 ? CORTAORendererFeature.Quality.VeryLow : preset == 2 ? CORTAORendererFeature.Quality.Low : preset == 3 ? CORTAORendererFeature.Quality.Medium : CORTAORendererFeature.Quality.High));
            // A custom LUT is tied to its ray count. Preserve both, including mixed selections.
            var customLut = s.FindPropertyRelative("lookupTable");
            bool preserveRays = customLut.hasMultipleDifferentValues || customLut.objectReferenceValue != null;
            if (!preserveRays) SetInt(s, "rayCount", preset == 1 ? 16 : 32);
            SetInt(s, "lookupQuality", preset == 1 ? 0 : preset == 2 ? 1 : 2);
            s.FindPropertyRelative("temporal").boolValue = true;
            s.FindPropertyRelative("temporalAtTracingResolution").boolValue = true;
            s.FindPropertyRelative("validatedBilinearHistory").boolValue = true;
            s.FindPropertyRelative("historyWeight").floatValue = 0.95f;
            s.FindPropertyRelative("historyResponse").floatValue = 0.1f;
            s.FindPropertyRelative("historyVarianceExpansion").floatValue = 0.5f;
            SetInt(s, "spatialDenoiseIterations", preset <= 2 ? 1 : 0);
            s.FindPropertyRelative("spatialDenoiseStrength").floatValue = 0.8f;
            if (preserveRays)
                Debug.Log("CORTAO: Preset preserved the existing ray count because a custom LUT is assigned in the selection. Clear the custom LUT to use the preset ray count.");
        }

        private static void SetInt(SerializedProperty s, string name, int value)
        {
            s.FindPropertyRelative(name).intValue = value;
        }
    }

    // Read-only preflight. Do not change global stripping settings or renderer assets.
    internal sealed class CORTAOBuildAudit : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;
        public void OnPreprocessBuild(BuildReport report) => Audit();

        [MenuItem("Tools/CORTAO/Check Build Configuration")]
        private static void Audit()
        {
            var pipelines = new HashSet<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset defaultAsset)
                pipelines.Add(defaultAsset);
            for (int i = 0; i < QualitySettings.names.Length; ++i)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset asset)
                    pipelines.Add(asset);
            bool anyActive = false;
            foreach (var pipeline in pipelines)
            {
                var list = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
                if (list == null || !list.isArray)
                {
                    Debug.LogWarning("CORTAO build audit: Cannot inspect renderer list for " + pipeline.name, pipeline);
                    continue;
                }
                for (int i = 0; i < list.arraySize; ++i)
                {
                    if (!(list.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableRendererData data)) continue;
                    bool active = false, otherAO = false;
                    foreach (var feature in data.rendererFeatures)
                    {
                        if (feature is CORTAORendererFeature cortao && cortao.isActive)
                        {
                            active = anyActive = true;
                            var s = cortao.settings;
                            if (s == null || s.prepass == null || s.trace == null || s.filter == null ||
                                s.upsample == null || s.debugShader == null || s.temporalShader == null ||
                                s.bentFilter == null || s.stereoTransfer == null || s.earlyMotionShader == null)
                                Debug.LogWarning("CORTAO build audit: Missing serialized shader references on " + data.name + ". Resources fallback remains available. Open and save the renderer asset.", data);
                        }
                        else if (feature is ScreenSpaceAmbientOcclusion && feature.isActive) otherAO = true;
                    }
                    Debug.Log("CORTAO build audit: " + pipeline.name + " renderer[" + i + "] " + data.name + " active CORTAO=" + active, data);
                    if (active && otherAO)
                        Debug.LogWarning("CORTAO build audit: Another active URP SSAO feature is on " + data.name + ". Disable it to avoid competing AO outputs.", data);
                }
            }
            if (!anyActive) Debug.LogWarning("CORTAO build audit: No active CORTAO found in Graphics/Quality URP assets. Check the build profile quality settings and camera renderer index.");
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace UnityCoder.CORTAO
{
    // URP 6.7 build stripping recognizes SSAO subclasses. Never invoke its rendering implementation.
    [DisallowMultipleRendererFeature("CORTAO Renderer Feature")]
    [Tooltip("CORTAO screen-space ambient occlusion.")]
    public sealed class CORTAORendererFeature : ScreenSpaceAmbientOcclusion
    {
        public enum Resolution { Full, Half }
        public enum InjectionPoint { Forward, Deferred }
        public enum FoveationPolicy { RejectReportedFoveation, AllowUniformRaster, RejectAllXR }
        // Keep original numeric values and API names for existing renderer assets.
        public enum Quality
        {
            [InspectorName("Minimal (4 samples)")] Minimal = 3,
            [InspectorName("Very Low (6 samples)")] VeryLow = 4,
            [InspectorName("Low (8 samples)")] Low = 0,
            [InspectorName("Medium (12 samples)")] Medium12 = 5,
            [InspectorName("High (16 samples)")] Medium = 1,
            [InspectorName("Very High (24 samples)")] VeryHigh = 6,
            [InspectorName("Ultra (32 samples)")] High = 2
        }
        public enum DebugView { Off, RawAO, FilteredAO, TemporalAO, HistoryRejection, BentNormal, RawBentNormal, HistoryWeight }
        public enum RayCount { Rays8 = 8, Rays16 = 16, Rays32 = 32 }
        public enum LookupQuality { Standard, High, Ultra }
        [Serializable]
        public sealed class Options
        {
            public ComputeShader prepass;
            public ComputeShader trace;
            public ComputeShader filter;
            public ComputeShader upsample;
            public ComputeShader temporalShader;
            public bool temporal = false;
            [Tooltip("Accumulate scalar AO at half resolution before upsampling. Full-resolution fallback for temporal bent normals, early object motion or odd input dimensions.")]
            public bool temporalAtTracingResolution = true;
            [Tooltip("Use R32 float for scalar AO textures instead of R16 float. Automatically selected if R16 is unsupported.")]
            public bool forceFullPrecisionAO = false;
            [Tooltip("Experimental early motion reprojection for explicitly tracked rigid opaque MeshRenderers. Fixed-resolution desktop or opt-in XR.")]
            public bool earlyRigidMotion = false;
            [Tooltip("Experimental per-eye rigid and skinned motion in XR multipass.")]
            public bool enableXRMultipassMotion = false;
            [Tooltip("Experimental per-eye early motion in XR Single Pass Instanced on Direct3D. Requires the StereoDepth shader pass.")]
            public bool enableXRSinglePassMotion = false;
            [Tooltip("Automatically discover eligible non-static opaque URP Lit and Simple Lit meshes in play mode.")]
            public bool automaticRigidMotion = false;
            [Tooltip("Experimental CPU-baked motion for explicitly tracked opaque skinned meshes.")]
            public bool skinnedMotion = false;
            [Tooltip("Allow explicitly submitted CPU geometry proxies for custom vertex deformation.")]
            public bool customDeformedMotion = false;
            [Tooltip("Automatically discover eligible opaque skinned meshes in play mode. CPU baking can be expensive.")]
            public bool automaticSkinnedMotion = false;
            public LayerMask automaticMotionLayers = -1;
            [Range(0.0000001f, 0.0001f)] public float motionDeviceDepthTolerance = 0.000001f;
            public Shader earlyMotionShader;
            public bool validatedBilinearHistory = true;
            [Tooltip("Preserve temporal history across resolution changes using depth-validated reprojection.")]
            public bool preserveHistoryOnResize = false;
            [Range(0f, 1f)] public float historyResponse = 0.5f;
            public bool enableXRMultipass = false;
            public bool enableXRSinglePass = false;
            [Tooltip("Experimental XR Multiview using two-slice compute textures. Requires supported GPU storage formats.")]
            public bool enableXRMultiview = false;
            public bool directStereoAO = true;
            public bool directStereoBent = true;
            public bool logDiagnostics = true;
            [Tooltip("Select Deferred for Deferred or Deferred+ renderers.")]
            public InjectionPoint injectionPoint = InjectionPoint.Deferred;
            [Tooltip("Log pass scheduling and input availability once per camera and configuration.")]
            public bool traceDiagnostics = false;
            [Tooltip("Experimental hardware dynamic resolution. Requires matching dynamically scaled depth and normal textures. Disable for fixed-resolution rendering.")]
            public bool enableHardwareDynamicResolution = false;
            [Tooltip("Experimental XR dynamic resolution for uniformly scaled intermediate textures. Disable foveated rendering before enabling.")]
            public bool enableXRDynamicResolution = false;
            [Tooltip("Experimental origin-aligned XR viewport scaling. Requires matching eye viewports and depth/normal dimensions. Disable foveation, MSAA and TAA for initial testing.")]
            public bool enableXRViewportScaling = false;
            [Tooltip("Allow Uniform Raster only if the XR provider preserves linear pixel coordinates in depth, normals and lighting. Non-uniform rasterization is unsupported.")]
            public FoveationPolicy foveationPolicy = FoveationPolicy.RejectReportedFoveation;
            [Tooltip("Set when an external XR provider enables foveation without reporting it through XRDisplaySubsystem.")]
            public bool externalFoveationActive = false;
            public ComputeShader stereoTransfer;
            [Range(0f, 0.98f)] public float historyWeight = 0.85f;
            [Min(0.001f)] public float historyDepthThreshold = 0.05f;
            [Range(0f, 1f)] public float historyNormalThreshold = 0.9f;
            [Range(0f, 0.25f)] public float historyClampExpansion = 0.025f;
            [Tooltip("Expand history bounds in noisy neighborhoods to reduce premature clipping. Zero restores the original clamp.")]
            [Range(0f, 1f)] public float historyVarianceExpansion = 0.5f;
            public Resolution resolution = Resolution.Full;
            [Tooltip("Optional edge-aware scalar AO denoising at half resolution. Zero disables all extra passes.")]
            [Range(0, 2)] public int spatialDenoiseIterations = 0;
            [Range(0f, 1f)] public float spatialDenoiseStrength = 0.8f;
            [Min(0.001f)] public float upsampleDepthThreshold = 0.05f;
            [Min(1f)] public float upsampleNormalPower = 16f;
            public TextAsset lookupTable; // Custom table must match ray count; uint32 entries.
            public RayCount rayCount = RayCount.Rays32;
            public bool outputBentNormals = false;
            public bool temporalBentNormals = true;
            public ComputeShader bentFilter;
            public Quality quality = Quality.Medium;
            public DebugView debugView = DebugView.Off;
            public LookupQuality lookupQuality = LookupQuality.Standard;
            [Min(0f)] public float normalBiasMultiplier = 1f;
            public Shader debugShader;
            [Header("Visual Settings")]
            [Tooltip("Read the camera's evaluated CORTAO Volume overrides. No extra GPU passes.")]
            public bool useVolumeOverrides = false;
            [Tooltip("Fade AO by linear camera depth and skip tracing beyond Max Distance.")]
            public bool distanceFade = false;
            [Min(0f)] public float distanceFadeStart = 50f;
            [Min(0.01f)] public float maxDistance = 100f;
            [Min(0.001f)] public float radius = 1f;
            [Range(0f, 4f)] public float intensity = 1f;
            [Range(0f, 1f)] public float directLightingStrength = 0f;
            [Range(0.01f, 1f)] public float maxScreenRadius = 0.2f;
            [Range(-8f, 0f)] public float mipBias = -4f;
            [Min(1f)] public float maxSlopeCompensation = 4f;
            [Range(0f, 1f)] public float backfaceThreshold = 0.5f;
            [Range(0f, 1f)] public float backfaceBoost = 0f;
            [Min(1f)] public float filterDepthSensitivity = 512f;
            [Min(1f)] public float filterNormalSensitivity = 128f;
            [Tooltip("Experimental Direct3D12 packed mip chain. Disable to use separate textures.")]
            public bool useMipmappedPyramid = false;
            public int randomSeed = 12345;
            public bool animateNoise = false;
        }
        public Options settings = new Options();
        private Pass pass;
        private DebugPass debugPass;
        private GraphicsBuffer lookup;
        private TextAsset loadedLookup;
        private RayCount loadedRayCount;
        private bool warned;
        private readonly System.Collections.Generic.HashSet<string> schedulingMessages = new();
        private static bool PlayerDiagnostics => !Application.isEditor && Debug.isDebugBuild;
        private bool TraceEnabled => settings.traceDiagnostics || PlayerDiagnostics;
        private bool playerReported;
        private void ScheduleReport(string message)
        {
            if (TraceEnabled && schedulingMessages.Add(message)) Debug.Log("CORTAO: " + message);
        }
        private Material debugMaterial, motionMaterial;

        private bool AssignDefaultShaders()
        {
            bool changed = false;
            if (settings == null) { settings = new Options(); changed = true; }
            if (settings.prepass == null)
            {
                settings.prepass = Resources.Load<ComputeShader>("CORTAO/Prepass");
                changed |= settings.prepass != null;
            }
            if (settings.trace == null)
            {
                settings.trace = Resources.Load<ComputeShader>("CORTAO/Trace");
                changed |= settings.trace != null;
            }
            if (settings.filter == null)
            {
                settings.filter = Resources.Load<ComputeShader>("CORTAO/Filter");
                changed |= settings.filter != null;
            }
            if (settings.upsample == null)
            {
                settings.upsample = Resources.Load<ComputeShader>("CORTAO/Upsample");
                changed |= settings.upsample != null;
            }
            if (settings.temporalShader == null)
            {
                settings.temporalShader = Resources.Load<ComputeShader>("CORTAO/Temporal");
                changed |= settings.temporalShader != null;
            }
            if (settings.stereoTransfer == null)
            {
                settings.stereoTransfer = Resources.Load<ComputeShader>("CORTAO/StereoTransfer");
                changed |= settings.stereoTransfer != null;
            }
            if (settings.bentFilter == null)
            {
                settings.bentFilter = Resources.Load<ComputeShader>("CORTAO/BentFilter");
                changed |= settings.bentFilter != null;
            }
            if (settings.debugShader == null)
            {
                settings.debugShader = Resources.Load<Shader>("CORTAO/Debug");
                changed |= settings.debugShader != null;
            }
            if (settings.earlyMotionShader == null)
            {
                settings.earlyMotionShader = Resources.Load<Shader>("CORTAO/EarlyMotion");
                changed |= settings.earlyMotionShader != null;
            }
            return changed;
        }

        private readonly System.Collections.Generic.Dictionary<(RayCount,LookupQuality),TextAsset> defaultLuts = new();
        private TextAsset GetDefaultLut()
        {
            var key=(settings.rayCount,settings.lookupQuality);
            if (!defaultLuts.TryGetValue(key,out var asset) || asset==null)
            {
                asset=Resources.Load<TextAsset>("CORTAO/LUT" + (int)settings.rayCount + (settings.lookupQuality == LookupQuality.Ultra ? "Ultra" : settings.lookupQuality == LookupQuality.High ? "High" : ""));
                defaultLuts[key]=asset;
            }
            return asset;
        }
        public override void Create()
        {
            defaultLuts.Clear();
            bool assigned = AssignDefaultShaders();
#if UNITY_EDITOR
            if (assigned && !Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
            pass?.ReleaseHistory();
            lookup?.Dispose(); lookup = null; loadedLookup = null;
            debugPass = new DebugPass { renderPassEvent = RenderPassEvent.AfterRenderingTransparents };
            pass = new Pass { renderPassEvent = RenderPassEvent.AfterRenderingPrePasses };
            pass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            CoreUtils.Destroy(debugMaterial); debugMaterial = null;
            CoreUtils.Destroy(motionMaterial); motionMaterial = null;
            warned = false;
            schedulingMessages.Clear();
            playerReported = false;
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (PlayerDiagnostics && !playerReported)
            {
                playerReported = true;
                Debug.Log("CORTAO: Player renderer active. Pipeline=" + GraphicsSettings.currentRenderPipeline + " quality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] + " renderer=" + renderer.GetType().Name + " API=" + SystemInfo.graphicsDeviceType);
            }
            var cam = renderingData.cameraData.camera;
            if (!settings.temporal) pass?.ReleaseHistory();
            else pass?.PruneHistory(Time.frameCount);
            if (!settings.temporal || !settings.earlyRigidMotion || !settings.skinnedMotion || !settings.automaticSkinnedMotion)
                pass?.ClearAutomaticSkinnedMotion();
            if (cam == null) { ScheduleReport("No camera."); return; }
            if (TraceEnabled) ScheduleReport("AddRenderPasses: " + cam.name + " type=" + cam.cameraType + " stereo=" + cam.stereoEnabled + " debug=" + settings.debugView + " injection=" + settings.injectionPoint);
            if (!SystemInfo.supportsComputeShaders) { ScheduleReport("Skipped: compute shaders unsupported."); return; }
            if (cam.stereoEnabled && !settings.enableXRMultipass && !settings.enableXRSinglePass && !settings.enableXRMultiview) { ScheduleReport("Skipped: XR camera but no XR mode enabled."); return; }
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) { ScheduleReport("Skipped: preview or reflection camera."); return; }
            if (renderingData.cameraData.renderType == CameraRenderType.Overlay) { ScheduleReport("Skipped: overlay camera."); return; }
            if (!settings.useVolumeOverrides && settings.intensity <= 0f && settings.debugView == DebugView.Off) { ScheduleReport("Skipped: zero intensity."); return; }
            pass.renderPassEvent = settings.injectionPoint == InjectionPoint.Deferred ? RenderPassEvent.AfterRenderingGbuffer : RenderPassEvent.AfterRenderingPrePasses;
            var pre = settings.prepass != null ? settings.prepass : Resources.Load<ComputeShader>("CORTAO/Prepass");
            var trace = settings.trace != null ? settings.trace : Resources.Load<ComputeShader>("CORTAO/Trace");
            var filter = settings.filter != null ? settings.filter : Resources.Load<ComputeShader>("CORTAO/Filter");
            var upsample = settings.upsample != null ? settings.upsample : Resources.Load<ComputeShader>("CORTAO/Upsample");
            if (settings.resolution == Resolution.Half && upsample == null) { if (!warned) Debug.LogWarning("CORTAO: Missing upsample shader."); warned = true; return; }
            var lut = settings.lookupTable != null ? settings.lookupTable : GetDefaultLut();
            if (pre == null || trace == null || filter == null || lut == null)
            {
                if (!warned) Debug.LogWarning("CORTAO: Missing compute shader or invalid lookup table.");
                warned = true; return;
            }
            if (lookup == null || loadedLookup != lut || loadedRayCount != settings.rayCount)
            {
                var bytes = lut.bytes;
                if (bytes.Length != 24*24*16*4 && bytes.Length != 32*32*24*4 && bytes.Length != 64*64*32*4)
                {
                    if (!warned) Debug.LogWarning("CORTAO: Invalid lookup table byte count.");
                    warned = true;
                    return;
                }
                var values = new uint[bytes.Length / 4];
                Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
                int rays = (int)settings.rayCount;
                if (rays != 8 && rays != 16 && rays != 32) return;
                uint mask = rays == 32 ? uint.MaxValue : (1u << rays) - 1u;
                foreach (uint value in values)
                    if ((value & ~mask) != 0u)
                    {
                        if (!warned) Debug.LogWarning("CORTAO: Lookup table contains bits outside the selected ray count.");
                        warned = true;
                        return;
                    }
                pass?.ReleaseHistory();
                lookup?.Dispose(); lookup = null;
                lookup = new GraphicsBuffer(GraphicsBuffer.Target.Structured, values.Length, 4);
                lookup.SetData(values); loadedLookup = lut; loadedRayCount = settings.rayCount; warned = false;
            }
            if (settings.debugView != DebugView.Off)
            {
                var shader = settings.debugShader != null ? settings.debugShader : Resources.Load<Shader>("CORTAO/Debug");
                if (debugMaterial != null && debugMaterial.shader != shader) { CoreUtils.Destroy(debugMaterial); debugMaterial = null; }
                if (debugMaterial == null && shader != null && shader.isSupported) debugMaterial = CoreUtils.CreateEngineMaterial(shader);
            }
            if(settings.earlyRigidMotion && settings.temporal)
            {
                var shader = settings.earlyMotionShader != null ? settings.earlyMotionShader : Resources.Load<Shader>("CORTAO/EarlyMotion");
                if (motionMaterial != null && motionMaterial.shader != shader) { CoreUtils.Destroy(motionMaterial); motionMaterial = null; pass?.ReleaseHistory(); }
                if(motionMaterial == null && shader != null && shader.isSupported) motionMaterial = CoreUtils.CreateEngineMaterial(shader);
            }
            pass.Setup(settings, pre, trace, filter, upsample, lookup, debugMaterial, motionMaterial);
            renderer.EnqueuePass(pass);
            if (TraceEnabled) ScheduleReport("Enqueued AO for " + cam.name + " at " + pass.renderPassEvent);
            if (settings.debugView != DebugView.Off && debugMaterial == null) ScheduleReport("Debug material unavailable or unsupported.");
            if (settings.debugView != DebugView.Off && debugMaterial != null)
            {
                debugPass.report = TraceEnabled ? ScheduleReport : null;
                debugPass.material = debugMaterial; debugPass.color=settings.debugView==DebugView.BentNormal || settings.debugView==DebugView.RawBentNormal;
                renderer.EnqueuePass(debugPass);
            }
        }
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(debugMaterial); debugMaterial = null;
            CoreUtils.Destroy(motionMaterial); motionMaterial = null;
            pass?.ReleaseHistory();
            lookup?.Dispose(); lookup = null; loadedLookup = null;
        }
        private sealed class DebugData : ContextItem
        {
            public TextureHandle texture;
            public override void Reset() { texture = default; }
        }
        private sealed class DebugPass : ScriptableRenderPass
        {
            public Material material;
            public bool color;
            public Action<string> report;
            private sealed class BlitData { public TextureHandle source; public Material material; public int pass; }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                if (!frameData.Contains<DebugData>()) { report?.Invoke("Debug pass: AO did not publish a debug texture."); return; }
                var source = frameData.Get<DebugData>().texture;
                if (!source.IsValid()) { report?.Invoke("Debug pass: invalid source texture."); return; }
                report?.Invoke("Debug pass recorded.");
                var resources = frameData.Get<UniversalResourceData>();
                using (var builder = graph.AddRasterRenderPass<BlitData>("CORTAO - Debug view", out var data))
                {
                    data.source = source; data.material = material; data.pass=color?1:0;
                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (BlitData d, RasterGraphContext ctx) =>
                        Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1,1,0,0), d.material, d.pass));
                }
            }
        }
        private sealed class Pass : ScriptableRenderPass
        {
            private Options options;
            private VisualSettings visual;
            private struct VisualSettings
            {
                public float radius, intensity, direct, fadeStart, maxDistance;
                public bool fade;
                public static VisualSettings Resolve(Options o)
                {
                    var v = new VisualSettings { radius=o.radius, intensity=o.intensity,
                        direct=o.directLightingStrength, fade=o.distanceFade,
                        fadeStart=o.distanceFadeStart, maxDistance=o.maxDistance };
                    if (o.useVolumeOverrides)
                    {
                        var volume = VolumeManager.instance.stack?.GetComponent<CORTAOVolume>();
                        if (volume != null && volume.active)
                        {
                            if (volume.radius.overrideState) v.radius=volume.radius.value;
                            if (volume.intensity.overrideState) v.intensity=volume.intensity.value;
                            if (volume.directLightingStrength.overrideState) v.direct=volume.directLightingStrength.value;
                            if (volume.distanceFadeStart.overrideState) v.fadeStart=volume.distanceFadeStart.value;
                            if (volume.maxDistance.overrideState) v.maxDistance=volume.maxDistance.value;
                        }
                    }
                    v.radius=Mathf.Max(0.001f,v.radius);
                    v.intensity=Mathf.Clamp(v.intensity,0f,4f);
                    v.direct=Mathf.Clamp01(v.direct);
                    v.maxDistance=Mathf.Max(0.01f,v.maxDistance);
                    v.fadeStart=Mathf.Clamp(v.fadeStart,0f,Mathf.Max(0f,v.maxDistance-0.01f));
                    return v;
                }
                // Allow ordinary volume blending without resetting the entire history.
                // Reject large discontinuities (e.g. teleporting between distinct volumes).
                public bool Compatible(VisualSettings old)
                {
                    return fade==old.fade && Mathf.Abs(radius-old.radius)<=Mathf.Max(0.02f,old.radius*0.15f)
                        && Mathf.Abs(intensity-old.intensity)<=0.15f
                        && (!fade || (Mathf.Abs(fadeStart-old.fadeStart)<=Mathf.Max(1f,old.fadeStart*0.15f)
                            && Mathf.Abs(maxDistance-old.maxDistance)<=Mathf.Max(1f,old.maxDistance*0.15f)));
                }
                public Vector4 DistanceParams => fade ? new Vector4(fadeStart,maxDistance,1f/Mathf.Max(0.01f,maxDistance-fadeStart),1f) : Vector4.zero;
            }
            private readonly System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem> xrDisplays = new();
            private bool activeFoveation;
            private bool CheckFoveation(UniversalCameraData cam)
            {
                activeFoveation = false;
                if (!cam.xr.enabled) return true;
                if (options.foveationPolicy == FoveationPolicy.RejectAllXR)
                { Report("XR disabled by foveation policy."); return false; }
                xrDisplays.Clear();
                SubsystemManager.GetSubsystems(xrDisplays);
                activeFoveation = options.externalFoveationActive;
                foreach (var display in xrDisplays)
                    if (display.running && display.foveatedRenderingLevel > 0f)
                        activeFoveation = true;
                if (!activeFoveation) return true;
                if (options.foveationPolicy != FoveationPolicy.AllowUniformRaster ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                { Report("Reported XR foveation is unsupported by the selected policy. AO skipped."); return false; }
                Report("Experimental uniform-raster foveation enabled. Verify linear depth/normal coordinates and both eyes. Early motion disabled.");
                return true;
            }
            private ComputeShader pre, trace, filter, upsample, temporal, bentFilter;
            private Material motionMaterial;
            private GraphicsBuffer lut;
            private readonly System.Collections.Generic.Dictionary<(ComputeShader shader,string kernel),int> kernelCache = new();
            private readonly System.Collections.Generic.HashSet<string> reportedIssues = new();
            private void Report(string message)
            {
                if(options.logDiagnostics && reportedIssues.Add(message)) Debug.LogWarning("CORTAO: " + message);
            }
            private bool Require(ComputeShader shader,string kernel)
            {
                if(shader != null && kernelCache.ContainsKey((shader,kernel))) return true;
                if(shader == null || !shader.HasKernel(kernel))
                {
                    Report("Missing kernel " + kernel + " in " + (shader != null ? shader.name : "unassigned compute shader") + ". Replace all package shader resources together.");
                    return false;
                }
                kernelCache.Add((shader,kernel),shader.FindKernel(kernel));
                return true;
            }
            private int Kernel(ComputeShader shader,string name)
            {
                if (!kernelCache.TryGetValue((shader,name),out int index))
                {
                    index=shader.FindKernel(name);
                    kernelCache.Add((shader,name),index);
                }
                return index;
            }
            private void TraceReport(string message)
            {
                if (reportedIssues.Add(message)) Debug.Log("CORTAO: " + message);
            }
            private bool TemporalDebug => options.debugView == DebugView.HistoryRejection || options.debugView == DebugView.HistoryWeight;
            private bool ValidateKernels(UniversalCameraData cam,bool stereo,ComputeShader transfer)
            {
                string suffix=(cam.camera.orthographic?"Ortho":"")+(stereo?"Stereo":"");
                bool half=options.resolution==Resolution.Half;
                bool useTemporal=options.temporal && cam.camera.cameraType==CameraType.Game;
                if (TemporalDebug && !useTemporal) Report("Temporal debug requires temporal accumulation on a Game camera.");
                bool bent=options.outputBentNormals || options.debugView==DebugView.BentNormal;
                bool rawBent=bent || options.debugView==DebugView.RawBentNormal;
                bool temporalBent=useTemporal && bent && options.temporalBentNormals;
                bool aoArray=stereo && options.directStereoAO;
                bool bentArray=stereo && options.directStereoBent && bent;
                bool ok=Require(pre,"Prepass"+suffix);
                ok &= Require(trace,"Trace"+options.quality+(rawBent?"Bent":"")+"R"+(int)options.rayCount+suffix);
                ok &= Require(filter,aoArray&&!half&&!useTemporal?"FilterArray":"Filter");
                if(half && options.spatialDenoiseIterations>0 && options.spatialDenoiseStrength>0f)
                    ok &= Require(filter,"DenoiseAO");
                if(half) {
                    ok &= Require(upsample,"Upsample"+suffix+(aoArray&&!useTemporal?"Array":""));
                    if(useTemporal && options.temporalAtTracingResolution && aoArray) ok &= Require(upsample,"Upsample"+suffix+"Array");
                    if(useTemporal && options.temporalAtTracingResolution) ok &= Require(temporal,"Resolve"+(stereo?"Stereo":"")+(TemporalDebug?"Debug":""));
                }
                if(bent) ok &= Require(bentFilter,"FilterBent"+suffix+(bentArray&&!temporalBent?"Array":""));
                if(useTemporal) ok &= Require(temporal,(temporalBent?"ResolveBent":"Resolve")+(stereo?"Stereo":"")+(aoArray?"Array":"")+(temporalBent&&bentArray?"BentArray":"")+(TemporalDebug?"Debug":""));
                if(stereo) { ok &= Require(transfer,"CombineAO"); ok &= Require(transfer,"CombineColor"); }
                return ok;
            }
            private GraphicsFormat aoFormat = GraphicsFormat.R16_SFloat;
            private GraphicsFormat bentFormat = GraphicsFormat.R16G16B16A16_SFloat;
            private readonly System.Collections.Generic.Dictionary<GraphicsFormat,(bool sample,bool storage)> formatCache = new();
            private (bool sample,bool storage) Capabilities(GraphicsFormat format)
            {
                if (!formatCache.TryGetValue(format,out var caps))
                {
                    caps=(SystemInfo.IsFormatSupported(format,GraphicsFormatUsage.Sample),SystemInfo.IsFormatSupported(format,GraphicsFormatUsage.LoadStore));
                    formatCache.Add(format,caps);
                }
                return caps;
            }
            private bool SupportsStorage(GraphicsFormat format) { var caps=Capabilities(format); return caps.sample && caps.storage; }
            private string FormatStatus(GraphicsFormat format) { var caps=Capabilities(format); return format + " Sample=" + caps.sample + " LoadStore=" + caps.storage; }
            private bool SelectStorageFormats(UniversalCameraData cam)
            {
                aoFormat = !options.forceFullPrecisionAO && SupportsStorage(GraphicsFormat.R16_SFloat) ? GraphicsFormat.R16_SFloat : GraphicsFormat.R32_SFloat;
                bentFormat = SupportsStorage(GraphicsFormat.R16G16B16A16_SFloat) ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R32G32B32A32_SFloat;
                bool needsBent = options.outputBentNormals || options.debugView == DebugView.BentNormal || options.debugView == DebugView.RawBentNormal;
                bool needsHistory = options.temporal && cam.camera.cameraType == CameraType.Game;
                string Details() => SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType + ": " + FormatStatus(GraphicsFormat.R32_UInt) + "; " + FormatStatus(GraphicsFormat.R16_SFloat) + "; " + FormatStatus(GraphicsFormat.R32_SFloat) + "; " + FormatStatus(GraphicsFormat.R16G16B16A16_SFloat) + "; " + FormatStatus(GraphicsFormat.R32G32B32A32_SFloat);
                if (options.traceDiagnostics || PlayerDiagnostics) TraceReport("Storage capabilities: " + Details() + ". Selected AO=" + aoFormat + " Bent=" + bentFormat);
                // Packed uint textures use integer Load and UAV writes, not sampler operations.
                if (!Capabilities(GraphicsFormat.R32_UInt).storage) { Report("Packed depth/normal format unsupported. AO skipped. " + Details()); return false; }
                if (!SupportsStorage(aoFormat)) { Report("Scalar AO storage format unsupported. AO skipped. " + Details()); return false; }
                if (needsBent && !SupportsStorage(bentFormat)) { Report("Bent normal storage format unsupported. Disable bent normals and bent debug views. AO skipped. " + Details()); return false; }
                if (needsHistory && !SupportsStorage(GraphicsFormat.R32G32B32A32_SFloat)) { Report("Temporal history storage format unsupported. Disable Temporal. AO skipped. " + Details()); return false; }
                if ((options.traceDiagnostics || PlayerDiagnostics) && aoFormat == GraphicsFormat.R32_SFloat) TraceReport("Using R32_SFloat AO textures instead of R16_SFloat.");
                return true;
            }
            private static string EyeLabel(bool stereo,int eye) => stereo ? (eye==0?" [Left]":" [Right]") : "";

            private Material debugMaterial;
            
            private static readonly int PackedResolution = Shader.PropertyToID("_PackedResolution");
            private static readonly int Seed = Shader.PropertyToID("_Seed");
            private static readonly int MipCount = Shader.PropertyToID("_MipCount");
            private static readonly int Depth = Shader.PropertyToID("_Depth");
            private static readonly int Normals = Shader.PropertyToID("_Normals");
            private static readonly int View = Shader.PropertyToID("_View");
            private static readonly int RawAO = Shader.PropertyToID("_RawAO");
            private static readonly int FinalAO = Shader.PropertyToID("_FinalAO");
            private static readonly int Lookup = Shader.PropertyToID("_Lookup");
            private static readonly int AOGlobal = Shader.PropertyToID("_ScreenSpaceOcclusionTexture");
            private static readonly int AOParam = Shader.PropertyToID("_AmbientOcclusionParam");
            private static readonly GlobalKeyword BentKeyword = GlobalKeyword.Create("_CORTAO_BENT_NORMALS");
            private static readonly GlobalKeyword AOKeyword = GlobalKeyword.Create("_SCREEN_SPACE_OCCLUSION");
            private static readonly int MipmappedPyramid = Shader.PropertyToID("_MipmappedPyramid");
            private static readonly int[] Mips = Array.ConvertAll(new[]{0,1,2,3,4,5,6,7}, i => Shader.PropertyToID("_Mip"+i));
            private static readonly int IDBent = Shader.PropertyToID("_Bent");
            private static readonly int IDCORTAOBentNormalTexture = Shader.PropertyToID("_CORTAOBentNormalTexture");
            private static readonly int IDCORTAOEye = Shader.PropertyToID("_CORTAOEye");
            private static readonly int IDCORTAOMotionCurrentMVP = Shader.PropertyToID("_CORTAOMotionCurrentMVP");
            private static readonly int IDCORTAOMotionDepth = Shader.PropertyToID("_CORTAOMotionDepth");
            private static readonly int IDCORTAOMotionDepthArray = Shader.PropertyToID("_CORTAOMotionDepthArray");
            private static readonly int IDCORTAOMotionDepthTolerance = Shader.PropertyToID("_CORTAOMotionDepthTolerance");
            private static readonly int IDCORTAOMotionEye = Shader.PropertyToID("_CORTAOMotionEye");
            private static readonly int IDCORTAOMotionObjectValid = Shader.PropertyToID("_CORTAOMotionObjectValid");
            private static readonly int IDCORTAOMotionPreviousMV = Shader.PropertyToID("_CORTAOMotionPreviousMV");
            private static readonly int IDCORTAOMotionPreviousMVP = Shader.PropertyToID("_CORTAOMotionPreviousMVP");
            private static readonly int IDCORTAOMotionSkinned = Shader.PropertyToID("_CORTAOMotionSkinned");
            private static readonly int IDClampExpansion = Shader.PropertyToID("_ClampExpansion");
            private static readonly int IDCurrentAO = Shader.PropertyToID("_CurrentAO");
            private static readonly int IDCurrentBent = Shader.PropertyToID("_CurrentBent");
            private static readonly int IDDenoiseStep = Shader.PropertyToID("_DenoiseStep");
            private static readonly int IDDenoiseStrength = Shader.PropertyToID("_DenoiseStrength");
            private static readonly int IDDepthOffset = Shader.PropertyToID("_DepthOffset");
            private static readonly int IDDepthScale = Shader.PropertyToID("_DepthScale");
            private static readonly int IDDepthSensitivity = Shader.PropertyToID("_DepthSensitivity");
            private static readonly int IDDepthThreshold = Shader.PropertyToID("_DepthThreshold");
            private static readonly int IDDistanceFade = Shader.PropertyToID("_DistanceFade");
            private static readonly int IDEarlyMotion = Shader.PropertyToID("_EarlyMotion");
            private static readonly int IDFilteredBent = Shader.PropertyToID("_FilteredBent");
            private static readonly int IDFinalBent = Shader.PropertyToID("_FinalBent");
            private static readonly int IDFullHeight = Shader.PropertyToID("_FullHeight");
            private static readonly int IDFullWidth = Shader.PropertyToID("_FullWidth");
            private static readonly int IDHeight = Shader.PropertyToID("_Height");
            private static readonly int IDHistory = Shader.PropertyToID("_History");
            private static readonly int IDHistoryBent = Shader.PropertyToID("_HistoryBent");
            private static readonly int IDHistoryDepthThreshold = Shader.PropertyToID("_HistoryDepthThreshold");
            private static readonly int IDHistoryNormalThreshold = Shader.PropertyToID("_HistoryNormalThreshold");
            private static readonly int IDHistoryResponse = Shader.PropertyToID("_HistoryResponse");
            private static readonly int IDHistoryValid = Shader.PropertyToID("_HistoryValid");
            private static readonly int IDHistoryDebugMode = Shader.PropertyToID("_HistoryDebugMode");
            private static readonly int IDHistoryWeight = Shader.PropertyToID("_HistoryWeight");
            private static readonly int IDIntensity = Shader.PropertyToID("_Intensity");
            private static readonly int IDInverseVP = Shader.PropertyToID("_InverseVP");
            private static readonly int IDInverseView = Shader.PropertyToID("_InverseView");
            private static readonly int IDLeftAO = Shader.PropertyToID("_LeftAO");
            private static readonly int IDLeftColor = Shader.PropertyToID("_LeftColor");
            private static readonly int IDLowResolution = Shader.PropertyToID("_LowResolution");
            private static readonly int IDLutSize = Shader.PropertyToID("_LutSize");
            private static readonly int IDNextBent = Shader.PropertyToID("_NextBent");
            private static readonly int IDNextHistory = Shader.PropertyToID("_NextHistory");
            private static readonly int IDNormalPower = Shader.PropertyToID("_NormalPower");
            private static readonly int IDNormalSensitivity = Shader.PropertyToID("_NormalSensitivity");
            private static readonly int IDPreviousHeight = Shader.PropertyToID("_PreviousHeight");
            private static readonly int IDPreviousVP = Shader.PropertyToID("_PreviousVP");
            private static readonly int IDPreviousView = Shader.PropertyToID("_PreviousView");
            private static readonly int IDPreviousWidth = Shader.PropertyToID("_PreviousWidth");
            private static readonly int IDProject = Shader.PropertyToID("_Project");
            private static readonly int IDRejection = Shader.PropertyToID("_Rejection");
            private static readonly int IDRightAO = Shader.PropertyToID("_RightAO");
            private static readonly int IDRightColor = Shader.PropertyToID("_RightColor");
            private static readonly int IDStereoAO = Shader.PropertyToID("_StereoAO");
            private static readonly int IDStereoColor = Shader.PropertyToID("_StereoColor");
            private static readonly int IDTraceParams = Shader.PropertyToID("_TraceParams");
            private static readonly int IDTraceParams2 = Shader.PropertyToID("_TraceParams2");
            private static readonly int IDUnproject = Shader.PropertyToID("_Unproject");
            private static readonly int IDUseEarlyMotion = Shader.PropertyToID("_UseEarlyMotion");
            private static readonly int IDValidatedBilinearHistory = Shader.PropertyToID("_ValidatedBilinearHistory");
            private static readonly int IDVarianceExpansion = Shader.PropertyToID("_VarianceExpansion");
            private static readonly int IDWidth = Shader.PropertyToID("_Width");
            public void Setup(Options o, ComputeShader p, ComputeShader t, ComputeShader f, ComputeShader u, GraphicsBuffer l, Material m, Material motion)
            { options=o; pre=p; trace=t; filter=f; upsample=u; lut=l; debugMaterial=m; motionMaterial=motion; bentFilter=o.bentFilter != null ? o.bentFilter : Resources.Load<ComputeShader>("CORTAO/BentFilter"); temporal=o.temporalShader != null ? o.temporalShader : Resources.Load<ComputeShader>("CORTAO/Temporal"); }
            private sealed class Data
            {
                public ComputeShader shader;
                public bool publish, writeBent, publishBent;
                public int kernel, width, height, count, seed, packed, fullWidth, fullHeight, eye, denoiseStep;
                public bool stereoInput, mipmapped;
                public Vector4 lutSize;
                public TextureHandle depth, normals, raw, output, bent;
                public TextureHandle[] mips;
                public BufferHandle lookup;
                public Matrix4x4 view;
                public float upsampleThreshold, upsamplePower;
                public float depthScale, depthOffset, intensity, depthSensitivity, normalSensitivity, direct;
                public Vector4 unproject, project, traceParams, traceParams2, distanceFade;
            }
            private static TextureHandle Make(RenderGraph g,int w,int h,GraphicsFormat format,string name)
            {
                return g.CreateTexture(new TextureDesc(w,h) { name=name, colorFormat=format,
                    enableRandomWrite=true, filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp,
                    clearBuffer=false });
            }
            private static void Common(Data d, ComputeGraphContext ctx)
            {
                if(d.stereoInput) ctx.cmd.SetComputeIntParam(d.shader,IDCORTAOEye,d.eye);
                ctx.cmd.SetComputeIntParam(d.shader,PackedResolution,d.packed);
                ctx.cmd.SetComputeIntParam(d.shader,IDFullWidth,d.fullWidth);
                ctx.cmd.SetComputeIntParam(d.shader,IDFullHeight,d.fullHeight);
            }
            private static void SetFloat(Data d, ComputeGraphContext c,int id,float value)
            { c.cmd.SetComputeFloatParam(d.shader,id,value); }
            private static void SetVector(Data d, ComputeGraphContext c,int id,Vector4 value)
            { c.cmd.SetComputeVectorParam(d.shader,id,value); }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var res = frameData.Get<UniversalResourceData>();
                var cam = frameData.Get<UniversalCameraData>();
                visual = VisualSettings.Resolve(options);
                // Volume overrides may disable AO even when the renderer default is nonzero.
                if (visual.intensity <= 0f && options.debugView == DebugView.Off) return;
                if (options.traceDiagnostics || PlayerDiagnostics) TraceReport("RecordRenderGraph: " + cam.camera.name + " depth=" + res.cameraDepthTexture.IsValid() + " normals=" + res.cameraNormalsTexture.IsValid());
                if (!CheckFoveation(cam)) return;
                if (!res.cameraDepthTexture.IsValid() || !res.cameraNormalsTexture.IsValid()) { Report("Camera depth or normals unavailable. Check renderer prepass configuration."); return; }
                var depthDesc=graph.GetTextureDesc(res.cameraDepthTexture);
                var normalsDesc=graph.GetTextureDesc(res.cameraNormalsTexture);
                // URP render scale is already included in explicit texture dimensions.
                // Never apply the hardware scale to an already resized software target.
                if(depthDesc.sizeMode != TextureSizeMode.Explicit || normalsDesc.sizeMode != TextureSizeMode.Explicit)
                { Report("Non-explicit depth or normal texture sizing is not supported. AO skipped."); return; }
                // The compute kernels bind ordinary sampled textures, not Texture2DMS.
                if (depthDesc.msaaSamples != MSAASamples.None || normalsDesc.msaaSamples != MSAASamples.None)
                { Report("Multisampled depth or normals are unsupported. AO skipped; resolved inputs are required."); return; }
                if (!SelectStorageFormats(cam)) return;
                int w=depthDesc.width, h=depthDesc.height;
                if(normalsDesc.width!=w || normalsDesc.height!=h)
                { Report("Depth and normal texture dimensions differ. AO skipped to prevent invalid reads."); return; }
                if(depthDesc.useDynamicScale != normalsDesc.useDynamicScale || depthDesc.useDynamicScaleExplicit || normalsDesc.useDynamicScaleExplicit)
                { Report("Mixed or explicitly applied dynamic texture scaling is not supported. AO skipped."); return; }
                float sx=ScalableBufferManager.widthScaleFactor, sy=ScalableBufferManager.heightScaleFactor;
                bool hardwareScaled=cam.camera.allowDynamicResolution && (Mathf.Abs(sx-1f)>0.0001f || Mathf.Abs(sy-1f)>0.0001f);
                // Hardware-scaled intermediate targets and viewport-only eye targets
                // are distinct paths. Never apply both scale factors to the same input.
                if(cam.xr.enabled)
                {
                    float viewportScale=UnityEngine.XR.XRSettings.appliedRenderViewportScale;
                    bool viewportScaled=viewportScale>0f && Mathf.Abs(viewportScale-1f)>0.0001f;
                    if(viewportScaled && hardwareScaled)
                    {
                        if(!options.enableXRDynamicResolution || Mathf.Abs(sx-viewportScale)>0.005f || Mathf.Abs(sy-viewportScale)>0.005f)
                        { Report("XR intermediate and viewport scales do not match. AO skipped."); return; }
                    }
                    else if(viewportScaled)
                    {
                        if(!options.enableXRViewportScaling)
                        { Report("XR viewport scaling requires explicit opt-in. AO skipped."); return; }
                        if(!TryResolveXRViewport(cam,res,depthDesc,w,h,viewportScale,out int vw,out int vh)) return;
                        w=vw; h=vh;
                    }
                }
                if(hardwareScaled)
                {
                    if(!depthDesc.useDynamicScale || (cam.xr.enabled ? !options.enableXRDynamicResolution : !options.enableHardwareDynamicResolution))
                    { Report("Hardware dynamic resolution requires opt-in and matching scalable inputs. AO skipped."); return; }
                    if(cam.xr.enabled && Mathf.Abs(sx-sy)>0.0001f)
                    { Report("XR dynamic resolution requires uniform scaling. AO skipped."); return; }
                    if(!(sx>0f && sx<=1f && sy>0f && sy<=1f))
                    { Report("Unsupported hardware resolution scale. AO skipped."); return; }
                    w=Mathf.CeilToInt(w*sx); h=Mathf.CeilToInt(h*sy);
                }
                if (w<1 || h<1 || w>65535 || h>65535) { Report("Unsupported camera texture dimensions."); return; }
                bool stereo=cam.xr.enabled && cam.xr.singlePassEnabled;
                if(stereo)
                {
                    bool multiview = UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassMultiview;
                    if(multiview ? !options.enableXRMultiview : !options.enableXRSinglePass) return;
                    if(cam.xr.viewCount!=2 || depthDesc.dimension!=TextureDimension.Tex2DArray || normalsDesc.dimension!=TextureDimension.Tex2DArray || depthDesc.slices!=2 || normalsDesc.slices!=2)
                    { Report("XR single pass/multiview requires two-slice depth and normal texture arrays."); return; }
                    if(!SystemInfo.supports2DArrayTextures)
                    { Report("XR requires 2D texture arrays. AO skipped."); return; }
                    var transfer=options.stereoTransfer!=null?options.stereoTransfer:Resources.Load<ComputeShader>("CORTAO/StereoTransfer");
                    if(!ValidateKernels(cam,true,transfer)) return;
                    var sharedAO=options.directStereoAO ? graph.CreateTexture(new TextureDesc(w,h) { name="CORTAO Direct Stereo AO",dimension=TextureDimension.Tex2DArray,slices=2,colorFormat=aoFormat,enableRandomWrite=true,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp }) : default;
                    var sharedBent=options.directStereoBent && bentFilter!=null && (options.outputBentNormals || options.debugView==DebugView.BentNormal) ? graph.CreateTexture(new TextureDesc(w,h) { name="CORTAO Direct Stereo Bent",dimension=TextureDimension.Tex2DArray,slices=2,colorFormat=bentFormat,enableRandomWrite=true,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp }) : default;
                    var left=RecordEye(graph,cam,res.cameraDepthTexture,res.cameraNormalsTexture,w,h,0,100,false,true,sharedAO,sharedBent);
                    var right=RecordEye(graph,cam,res.cameraDepthTexture,res.cameraNormalsTexture,w,h,1,101,false,true,sharedAO,sharedBent);
                    var stereoAO=sharedAO.IsValid() ? sharedAO : RecordCombine(graph,transfer,left.ao,right.ao,w,h,false,AOGlobal,true);
                    TextureHandle stereoBent=sharedBent;
                    if(!sharedBent.IsValid() && options.outputBentNormals && left.bent.IsValid() && right.bent.IsValid())
                        stereoBent=RecordCombine(graph,transfer,left.bent,right.bent,w,h,true,IDCORTAOBentNormalTexture,false);
                    if(options.debugView!=DebugView.Off && debugMaterial!=null && left.debug.IsValid() && right.debug.IsValid())
                    {
                        bool color=options.debugView==DebugView.BentNormal || options.debugView==DebugView.RawBentNormal;
                        var desc=graph.GetTextureDesc(left.debug);
                        frameData.Create<DebugData>().texture=
                            left.debug.Equals(left.ao) && right.debug.Equals(right.ao) ? stereoAO :
                            stereoBent.IsValid() && left.debug.Equals(left.bent) && right.debug.Equals(right.bent) ? stereoBent :
                            RecordCombine(graph,transfer,left.debug,right.debug,desc.width,desc.height,color,-1,false);
                    }
                    return;
                }
                if(cam.xr.enabled && (!options.enableXRMultipass || cam.xr.viewCount!=1)) return;
                if(depthDesc.dimension!=TextureDimension.Tex2D || normalsDesc.dimension!=TextureDimension.Tex2D) { Report("Desktop/multipass requires 2D depth and normals."); return; }
                if(!ValidateKernels(cam,false,null)) return;
                var result=RecordEye(graph,cam,res.cameraDepthTexture,res.cameraNormalsTexture,w,h,0,cam.xr.enabled?cam.xr.multipassId:-1,true);
                if(options.debugView!=DebugView.Off && debugMaterial!=null && result.debug.IsValid()) frameData.Create<DebugData>().texture=result.debug;
            }
            private bool TryResolveXRViewport(UniversalCameraData cam, UniversalResourceData res, TextureDesc depthDesc, int width, int height, float scale, out int w, out int h)
            {
                w=width; h=height;
                if(!(scale>0f && scale<=1f) || depthDesc.useDynamicScale || cam.xr.viewCount<1 || cam.xr.viewCount>2)
                { Report("XR viewport-only path requires fixed-size input textures and one or two views."); return false; }
                Rect viewport=cam.xr.GetViewport(0);
                int vw=Mathf.RoundToInt(viewport.width), vh=Mathf.RoundToInt(viewport.height);
                if(Mathf.Abs(viewport.x)>0.01f || Mathf.Abs(viewport.y)>0.01f || vw<1 || vh<1 ||
                    Mathf.Abs(viewport.width-vw)>0.01f || Mathf.Abs(viewport.height-vh)>0.01f)
                { Report("XR viewport must be origin-aligned with integral dimensions."); return false; }
                for(int eye=1;eye<cam.xr.viewCount;eye++)
                {
                    Rect other=cam.xr.GetViewport(eye);
                    if(Mathf.Abs(other.x-viewport.x)>0.01f || Mathf.Abs(other.y-viewport.y)>0.01f ||
                        Mathf.Abs(other.width-viewport.width)>0.01f || Mathf.Abs(other.height-viewport.height)>0.01f)
                    { Report("XR eye viewports differ. AO skipped."); return false; }
                }
                // A prepass already allocated at viewport size must not be scaled again.
                if(width==vw && height==vh) return true;
                // Only crop padded inputs when rendering directly to the XR backbuffer,
                // and only when the actual viewport agrees with the reported scale.
                // Intermediate targets can have different viewport/RTHandle conventions.
                if(!res.isActiveTargetBackBuffer || width!=cam.xr.renderTargetDesc.width || height!=cam.xr.renderTargetDesc.height ||
                    vw>width || vh>height || Mathf.Abs(vw-width*scale)>2f || Mathf.Abs(vh-height*scale)>2f)
                { Report("XR viewport layout is ambiguous or does not match prepass allocation. AO skipped."); return false; }
                w=vw; h=vh; return true;
            }
            // Permute the entire 32-bit seed. Both eyes and both stochastic passes
            // receive the same seed, and static sampling remains unchanged.
            private static int FrameSeed(int seed, int frame)
            {
                unchecked
                {
                    uint x = (uint)seed + (uint)frame * 0x9e3779b9u;
                    x ^= x >> 16; x *= 0x7feb352du;
                    x ^= x >> 15; x *= 0x846ca68bu;
                    x ^= x >> 16;
                    return (int)x;
                }
            }
            private struct EyeResult { public TextureHandle ao,bent,debug; }
            private EyeResult RecordEye(RenderGraph graph,UniversalCameraData cam,TextureHandle depth,TextureHandle normals,int w,int h,int eye,int historyView,bool publish,bool stereoInput=false,TextureHandle sharedAO=default,TextureHandle sharedBent=default)
            {
                int fullW=w, fullH=h;
                bool half=options.resolution == Resolution.Half;
                if(half) { w=(w+1)/2; h=(h+1)/2; }
                var mips=new TextureHandle[8];
                int mipCount=Mathf.Min(8,1+Mathf.FloorToInt(Mathf.Log(Mathf.Min(w,h),2)));
                // All eight UAV slots must refer to valid, distinct mip subresources.
                bool mipmapped=options.useMipmappedPyramid && mipCount==8 && SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D12;
                if(mipmapped)
                {
                    var pyramid=graph.CreateTexture(new TextureDesc(w,h) { name="CORTAO Packed Pyramid", colorFormat=GraphicsFormat.R32_UInt,
                        enableRandomWrite=true, useMipMap=true, autoGenerateMips=false, filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp, clearBuffer=false });
                    for(int i=0;i<8;i++) mips[i]=pyramid;
                }
                else
                    for(int i=0;i<8;i++) mips[i]=Make(graph,Mathf.Max(1,w>>i),Mathf.Max(1,h>>i),GraphicsFormat.R32_UInt,"CORTAO Packed "+i);
                var raw=Make(graph,w,h,aoFormat,"CORTAO Raw AO");
                bool bentDebug=options.outputBentNormals || options.debugView==DebugView.BentNormal || options.debugView==DebugView.RawBentNormal;
                var bent=bentDebug ? Make(graph,w,h,bentFormat,"CORTAO Bent Normal") : default;
                bool useTemporal=options.temporal && temporal!=null && cam.camera.cameraType==CameraType.Game;
                if(!useTemporal) InvalidateHistory(cam.camera, historyView);
                bool directFilter=sharedAO.IsValid()&&!half&&!useTemporal;
                bool resolveBent=useTemporal && options.temporalBentNormals && bentFilter!=null && (options.outputBentNormals || options.debugView==DebugView.BentNormal);
                bool lowTemporal=useTemporal && half && options.temporalAtTracingResolution && !resolveBent && !options.earlyRigidMotion && fullW%2==0 && fullH%2==0;
                bool directUpsample=sharedAO.IsValid()&&half&&(!useTemporal||lowTemporal);
                var output=directFilter ? sharedAO : Make(graph,w,h,aoFormat,"CORTAO Filtered AO");
                var final=half ? (directUpsample ? sharedAO : Make(graph,fullW,fullH,aoFormat,"CORTAO Upsampled AO")) : output;
                
                var buffer=graph.ImportBuffer(lut);
                int packed=w|(h<<16), seed=(options.animateNoise||useTemporal)?FrameSeed(options.randomSeed,Time.frameCount):options.randomSeed;
                float near=cam.camera.nearClipPlane, far=cam.camera.farClipPlane;
                if(SystemInfo.usesReversedZBuffer) (near,far)=(far,near);
                bool ortho=cam.camera.orthographic;
                string suffix=ortho?"Ortho":"";
                string inputSuffix=stereoInput?"Stereo":"";
                float depthScale=ortho?far-near:(near-far)/(near*far);
                float depthOffset=ortho?near:1f/near;
                var projection=GL.GetGPUProjectionMatrix(cam.GetProjectionMatrix(eye), true);
                float px=projection.m00, py=projection.m11, ox=projection.m02, oy=projection.m12;
                float ySign=SystemInfo.graphicsUVStartsAtTop?-1f:1f;
                float invX=1f/px, invY=1f/py;
                float maxRadius=2f*Mathf.Abs(invY)*options.maxScreenRadius;
                using(var b=graph.AddComputePass<Data>("CORTAO - Packed depth and normals"+EyeLabel(stereoInput,eye),out var d))
                {
                    d.shader=pre; d.kernel=Kernel(pre,"Prepass"+suffix+inputSuffix); d.eye=eye; d.stereoInput=stereoInput; d.width=w; d.height=h; d.packed=packed; d.fullWidth=fullW; d.fullHeight=fullH;
                    d.seed=seed; d.count=mipCount; d.depth=depth; d.normals=normals;
                    d.mips=mips; d.mipmapped=mipmapped; d.view=cam.GetViewMatrix(eye); d.depthScale=depthScale; d.depthOffset=depthOffset;
                    b.UseTexture(d.depth,AccessFlags.Read); b.UseTexture(d.normals,AccessFlags.Read);
                    if(mipmapped) b.UseTexture(mips[0],AccessFlags.Write);
                    else foreach(var t in mips) b.UseTexture(t,AccessFlags.Write);
                    b.SetRenderFunc(static (Data a,ComputeGraphContext c)=>{
                        Common(a,c); c.cmd.SetComputeIntParam(a.shader,Seed,a.seed); c.cmd.SetComputeIntParam(a.shader,MipCount,a.count);
                        SetFloat(a,c,IDDepthScale,a.depthScale); SetFloat(a,c,IDDepthOffset,a.depthOffset);
                        c.cmd.SetComputeMatrixParam(a.shader,View,a.view);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,Depth,a.depth);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,Normals,a.normals);
                        for(int i=0;i<8;i++)
                        {
                            if(a.mipmapped) c.cmd.SetComputeTextureParam(a.shader,a.kernel,Mips[i],a.mips[0],i,RenderTextureSubElement.Color);
                            else c.cmd.SetComputeTextureParam(a.shader,a.kernel,Mips[i],a.mips[i]);
                        }
                        c.cmd.DispatchCompute(a.shader,a.kernel,(a.width+7)/8,(a.height+7)/8,1);
                    });
                }
                using(var b=graph.AddComputePass<Data>("CORTAO - Trace " + (int)options.rayCount + " rays"+EyeLabel(stereoInput,eye),out var d))
                {
                    d.shader=trace; d.kernel=Kernel(trace,"Trace"+options.quality+(bentDebug?"Bent":"")+"R"+(int)options.rayCount+suffix+inputSuffix); d.eye=eye; d.stereoInput=stereoInput; d.width=w; d.height=h; d.packed=packed; d.fullWidth=fullW; d.fullHeight=fullH;
                    d.lutSize = lut.count == 64*64*32 ? new Vector4(64,64,32,0) : lut.count == 32*32*24 ? new Vector4(32,32,24,0) : new Vector4(24,24,16,0);
                    d.seed=seed; d.count=mipCount; d.normals=normals; d.mips=mips; d.mipmapped=mipmapped; d.raw=raw; d.lookup=buffer; d.bent=bent; d.writeBent=bentDebug;
                    d.view=cam.GetViewMatrix(eye);
                    d.unproject=new Vector4(2*invX,2*ySign*invY,(-1+ox)*invX,(-ySign+oy)*invY);
                    d.project=new Vector4(0.5f*px,0.5f*ySign*py,-0.5f+0.5f*ox,-0.5f+0.5f*ySign*oy);
                    d.traceParams=new Vector4(Mathf.Abs(invX)/w*Mathf.Sqrt(2)*options.normalBiasMultiplier,visual.radius,maxRadius/Mathf.Sqrt(maxRadius*maxRadius+1),Mathf.Log(Mathf.Abs(py)*h*0.5f,2)+options.mipBias);
                    if(ortho)
                    {
                        d.unproject=new Vector4(2*invX,2*ySign*invY,(-1-projection.m03)*invX,(-ySign-projection.m13)*invY);
                        float radius=Mathf.Min(visual.radius,maxRadius);
                        d.traceParams.y=radius;
                        d.traceParams.w+=Mathf.Log(Mathf.Max(radius,0.000001f),2);
                    }
                    d.traceParams2=new Vector4(1f/Mathf.Max(1,options.maxSlopeCompensation),options.backfaceThreshold,options.backfaceBoost,0);
                    d.distanceFade=visual.DistanceParams;
                    b.UseTexture(d.normals,AccessFlags.Read);
                    if(mipmapped) b.UseTexture(mips[0],AccessFlags.Read);
                    else foreach(var t in mips) b.UseTexture(t,AccessFlags.Read);
                    b.UseBuffer(buffer,AccessFlags.Read); b.UseTexture(raw,AccessFlags.Write); if(bentDebug) b.UseTexture(bent,AccessFlags.Write);
                    b.SetRenderFunc(static (Data a,ComputeGraphContext c)=>{
                        Common(a,c); c.cmd.SetComputeIntParam(a.shader,Seed,a.seed); c.cmd.SetComputeIntParam(a.shader,MipCount,a.count);
                        SetVector(a,c,IDLutSize,a.lutSize);
                        SetVector(a,c,IDUnproject,a.unproject); SetVector(a,c,IDProject,a.project);
                        SetVector(a,c,IDTraceParams,a.traceParams); SetVector(a,c,IDTraceParams2,a.traceParams2); SetVector(a,c,IDDistanceFade,a.distanceFade);
                        c.cmd.SetComputeMatrixParam(a.shader,View,a.view);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,Normals,a.normals);
                        c.cmd.SetComputeIntParam(a.shader,MipmappedPyramid,a.mipmapped?1:0);
                        for(int i=0;i<8;i++) c.cmd.SetComputeTextureParam(a.shader,a.kernel,Mips[i],a.mips[i]);
                        c.cmd.SetComputeBufferParam(a.shader,a.kernel,Lookup,a.lookup);
                        if(a.writeBent) c.cmd.SetComputeTextureParam(a.shader,a.kernel,IDBent,a.bent);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,RawAO,a.raw);
                        c.cmd.DispatchCompute(a.shader,a.kernel,(a.width+7)/8,(a.height+7)/8,1);
                    });
                }
                using(var b=graph.AddComputePass<Data>("CORTAO - Reference spatial filter"+EyeLabel(stereoInput,eye),out var d))
                {
                    d.shader=filter; d.kernel=Kernel(filter,directFilter?"FilterArray":"Filter"); d.eye=eye; d.width=w; d.height=h; d.packed=packed; d.fullWidth=fullW; d.fullHeight=fullH;
                    d.publish=(publish&&!half&&!useTemporal)||(directFilter&&eye==1); d.mips=mips; d.raw=raw; d.output=output; d.intensity=visual.intensity; d.direct=visual.direct;
                    d.depthSensitivity=options.filterDepthSensitivity; d.normalSensitivity=options.filterNormalSensitivity;
                    b.UseTexture(mips[0],AccessFlags.Read); b.UseTexture(raw,AccessFlags.Read); b.UseTexture(output,directFilter?AccessFlags.ReadWrite:AccessFlags.Write);
                    
                    if(d.publish) { b.SetGlobalTextureAfterPass(output,AOGlobal); b.AllowGlobalStateModification(true); b.AllowPassCulling(false); }
                    b.SetRenderFunc(static (Data a,ComputeGraphContext c)=>{
                        c.cmd.SetComputeIntParam(a.shader,IDCORTAOEye,a.eye); c.cmd.SetComputeIntParam(a.shader,PackedResolution,a.packed); SetFloat(a,c,IDIntensity,a.intensity); SetFloat(a,c,IDDepthSensitivity,a.depthSensitivity);
                        SetFloat(a,c,IDNormalSensitivity,a.normalSensitivity);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,Mips[0],a.mips[0]);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,RawAO,a.raw);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,FinalAO,a.output);
                        c.cmd.DispatchCompute(a.shader,a.kernel,(a.width+15)/16,(a.height+15)/16,1);
                        if(a.publish) { c.cmd.SetGlobalVector(AOParam,new Vector4(1,0,0,a.direct)); c.cmd.EnableKeyword(AOKeyword); }
                    });
                }
                TextureHandle rejection=default;
                var upsampleInput=output;
                if(lowTemporal)
                    upsampleInput=RecordTemporal(graph,cam,depth,normals,output,w,h,eye,historyView,false,stereoInput,default,out rejection,out _,lowResolution:true);
                if(half && options.spatialDenoiseStrength>0f)
                {
                    int iterations=Mathf.Clamp(options.spatialDenoiseIterations,0,2);
                    for(int iteration=0;iteration<iterations;iteration++)
                    {
                        var denoised=Make(graph,w,h,aoFormat,"CORTAO Spatial Denoise "+iteration);
                        using(var b=graph.AddComputePass<Data>("CORTAO - Adaptive spatial denoise "+iteration+EyeLabel(stereoInput,eye),out var d))
                        {
                            d.shader=filter; d.kernel=Kernel(filter,"DenoiseAO"); d.width=w; d.height=h; d.packed=packed;
                            d.denoiseStep=1<<iteration; d.intensity=Mathf.Clamp01(options.spatialDenoiseStrength);
                            d.depth=mips[0]; d.raw=upsampleInput; d.output=denoised;
                            b.UseTexture(d.depth,AccessFlags.Read); b.UseTexture(d.raw,AccessFlags.Read); b.UseTexture(d.output,AccessFlags.Write);
                            b.SetRenderFunc(static (Data a,ComputeGraphContext c)=>{
                                c.cmd.SetComputeIntParam(a.shader,PackedResolution,a.packed);
                                c.cmd.SetComputeIntParam(a.shader,IDDenoiseStep,a.denoiseStep);
                                SetFloat(a,c,IDDenoiseStrength,a.intensity);
                                c.cmd.SetComputeTextureParam(a.shader,a.kernel,Mips[0],a.depth);
                                c.cmd.SetComputeTextureParam(a.shader,a.kernel,RawAO,a.raw);
                                c.cmd.SetComputeTextureParam(a.shader,a.kernel,FinalAO,a.output);
                                c.cmd.DispatchCompute(a.shader,a.kernel,(a.width+7)/8,(a.height+7)/8,1);
                            });
                        }
                        upsampleInput=denoised;
                    }
                }
                if(half)
                using(var b=graph.AddComputePass<Data>("CORTAO - Bilateral upsample"+EyeLabel(stereoInput,eye),out var d))
                {
                    d.shader=upsample; d.kernel=Kernel(upsample,"Upsample"+suffix+inputSuffix+(directUpsample?"Array":"")); d.eye=eye; d.stereoInput=stereoInput;
                    d.width=fullW; d.height=fullH; d.fullWidth=fullW; d.fullHeight=fullH; d.packed=packed;
                    d.depth=depth; d.normals=normals; d.raw=upsampleInput; d.output=final;
                    d.view=cam.GetViewMatrix(eye); d.depthScale=depthScale; d.depthOffset=depthOffset;
                    d.upsampleThreshold=options.upsampleDepthThreshold; d.upsamplePower=options.upsampleNormalPower;
                    d.direct=visual.direct; d.publish=(publish&&(!useTemporal||lowTemporal))||(directUpsample&&eye==1);
                    b.UseTexture(d.depth,AccessFlags.Read); b.UseTexture(d.normals,AccessFlags.Read);
                    b.UseTexture(upsampleInput,AccessFlags.Read); b.UseTexture(final,directUpsample?AccessFlags.ReadWrite:AccessFlags.Write);
                    if(d.publish) { b.SetGlobalTextureAfterPass(final,AOGlobal); b.AllowGlobalStateModification(true); b.AllowPassCulling(false); }
                    b.SetRenderFunc(static (Data a,ComputeGraphContext c)=>{
                        Common(a,c); SetFloat(a,c,IDDepthScale,a.depthScale); SetFloat(a,c,IDDepthOffset,a.depthOffset);
                        SetFloat(a,c,IDDepthThreshold,a.upsampleThreshold); SetFloat(a,c,IDNormalPower,a.upsamplePower);
                        c.cmd.SetComputeMatrixParam(a.shader,View,a.view);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,Depth,a.depth);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,Normals,a.normals);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,RawAO,a.raw);
                        c.cmd.SetComputeTextureParam(a.shader,a.kernel,FinalAO,a.output);
                        c.cmd.DispatchCompute(a.shader,a.kernel,(a.width+7)/8,(a.height+7)/8,1);
                        if(a.publish) { c.cmd.SetGlobalVector(AOParam,new Vector4(1,0,0,a.direct)); c.cmd.EnableKeyword(AOKeyword); }
                    });
                }
                var filteredBent=bent;
                if(bentDebug && bentFilter!=null && (options.outputBentNormals || options.debugView==DebugView.BentNormal))
                {
                    bool directBent=sharedBent.IsValid()&&!resolveBent;
                    filteredBent=directBent?sharedBent:Make(graph,fullW,fullH,bentFormat,"CORTAO Filtered Bent Normal");
                    using(var b=graph.AddComputePass<Data>("CORTAO - Bent normal reconstruction"+EyeLabel(stereoInput,eye),out var d))
                    {
                        d.shader=bentFilter; d.kernel=Kernel(bentFilter,"FilterBent"+suffix+inputSuffix+(directBent?"Array":"")); d.eye=eye; d.stereoInput=stereoInput;
                        d.width=fullW; d.height=fullH; d.fullWidth=fullW; d.fullHeight=fullH; d.packed=packed;
                        d.depth=depth; d.normals=normals; d.bent=bent; d.output=filteredBent;
                        d.view=cam.GetViewMatrix(eye); d.depthScale=depthScale; d.depthOffset=depthOffset;
                        d.upsampleThreshold=options.upsampleDepthThreshold; d.upsamplePower=options.upsampleNormalPower;
                        b.UseTexture(d.depth,AccessFlags.Read); b.UseTexture(d.normals,AccessFlags.Read); b.UseTexture(bent,AccessFlags.Read); b.UseTexture(filteredBent,directBent?AccessFlags.ReadWrite:AccessFlags.Write);
                        d.publishBent=(publish||(directBent&&eye==1))&&options.outputBentNormals&&!resolveBent;
                        if(d.publishBent) { b.SetGlobalTextureAfterPass(filteredBent,IDCORTAOBentNormalTexture); b.AllowPassCulling(false); b.AllowGlobalStateModification(true); }
                        b.SetRenderFunc(static (Data a,ComputeGraphContext c)=>{
                            Common(a,c); SetFloat(a,c,IDDepthScale,a.depthScale); SetFloat(a,c,IDDepthOffset,a.depthOffset);
                            SetFloat(a,c,IDDepthThreshold,a.upsampleThreshold); SetFloat(a,c,IDNormalPower,a.upsamplePower);
                            c.cmd.SetComputeMatrixParam(a.shader,View,a.view);
                            c.cmd.SetComputeTextureParam(a.shader,a.kernel,Depth,a.depth); c.cmd.SetComputeTextureParam(a.shader,a.kernel,Normals,a.normals);
                            c.cmd.SetComputeTextureParam(a.shader,a.kernel,IDBent,a.bent);
                            c.cmd.SetComputeTextureParam(a.shader,a.kernel,IDFilteredBent,a.output);
                            c.cmd.DispatchCompute(a.shader,a.kernel,(a.width+7)/8,(a.height+7)/8,1);
                            if(a.publishBent) c.cmd.EnableKeyword(BentKeyword);
                        });
                    }
                }
                var resolved=final;
                if(useTemporal&&!lowTemporal)
                {
                    resolved=RecordTemporal(graph,cam,depth,normals,final,fullW,fullH,eye,historyView,publish,stereoInput,resolveBent ? filteredBent : default,out rejection,out var temporalBent,sharedAO,sharedBent);
                    if(resolveBent) filteredBent=temporalBent;
                }
                var debug=options.debugView==DebugView.RawAO?raw:
                        options.debugView==DebugView.RawBentNormal?bent:
                        options.debugView==DebugView.BentNormal?filteredBent:
                        options.debugView==DebugView.FilteredAO?(lowTemporal?output:final):
                        TemporalDebug?rejection:resolved;
                return new EyeResult { ao=resolved,bent=bentDebug && bentFilter!=null?filteredBent:default,debug=debug };
            }
            private sealed class StereoData
            {
                public ComputeShader shader;
                public int kernel,w,h,eye;
                public TextureHandle first,second,output,extra;
                public bool publish, publishBent;
                public float direct;
            }
            private TextureHandle RecordCombine(RenderGraph graph,ComputeShader shader,TextureHandle left,TextureHandle right,int w,int h,bool color,int global,bool publish)
            {
                var output=graph.CreateTexture(new TextureDesc(w,h) { name="CORTAO Stereo Output",dimension=TextureDimension.Tex2DArray,slices=2,colorFormat=color?bentFormat:aoFormat,enableRandomWrite=true,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp });
                using(var b=graph.AddComputePass<StereoData>("CORTAO - Combine eyes",out var d))
                {
                    d.shader=shader; d.kernel=Kernel(shader,color?"CombineColor":"CombineAO"); d.w=w; d.h=h; d.first=left; d.second=right; d.output=output; d.eye=color?1:0; d.publish=publish; d.direct=visual.direct;
                    b.UseTexture(left,AccessFlags.Read); b.UseTexture(right,AccessFlags.Read); b.UseTexture(output,AccessFlags.Write);
                    if(global!=-1) { b.SetGlobalTextureAfterPass(output,global); b.AllowPassCulling(false); }
                    d.publishBent=global==IDCORTAOBentNormalTexture;
                    if(publish || d.publishBent) b.AllowGlobalStateModification(true);
                    b.SetRenderFunc(static (StereoData a,ComputeGraphContext c)=>{
                        var cmd=c.cmd; var s=a.shader; int k=a.kernel; bool color=a.eye==1;
                        cmd.SetComputeIntParam(s,IDWidth,a.w); cmd.SetComputeIntParam(s,IDHeight,a.h);
                        cmd.SetComputeTextureParam(s,k,(color?IDLeftColor:IDLeftAO),a.first); cmd.SetComputeTextureParam(s,k,(color?IDRightColor:IDRightAO),a.second); cmd.SetComputeTextureParam(s,k,(color?IDStereoColor:IDStereoAO),a.output);
                        cmd.DispatchCompute(s,k,(a.w+7)/8,(a.h+7)/8,2);
                        if(a.publishBent) cmd.EnableKeyword(BentKeyword);
                        if(a.publish) { cmd.SetGlobalVector(AOParam,new Vector4(1,0,0,a.direct)); cmd.EnableKeyword(AOKeyword); }
                    });
                }
                return output;
            }
            private sealed class History
            {
                public RTHandle[] textures = new RTHandle[2];
                public RTHandle[] bentTextures = new RTHandle[2];
                public Camera camera;
                public int width,height,index,lastFrame=-100,signature;
                public VisualSettings visual;
                public Matrix4x4 vp,view;
                public Vector3 position;
                public Quaternion rotation;
                public void Release() { foreach(var t in textures) t?.Release(); textures=new RTHandle[2]; foreach(var t in bentTextures) t?.Release(); bentTextures=new RTHandle[2]; }
            }
            private readonly System.Collections.Generic.Dictionary<(Camera camera,int view),History> histories = new();
            public void ReleaseHistory() { foreach(var h in histories.Values) h.Release(); histories.Clear(); automaticMotion.Clear(); automaticSkinnedMotion.Clear(); }
            private int lastPruneFrame = -1;
            private readonly System.Collections.Generic.List<(Camera camera,int view)> expired = new();
            public void PruneHistory(int frame)
            {
                if (lastPruneFrame == frame) return;
                lastPruneFrame = frame;
                expired.Clear();
                if (histories.Count == 0) return;
                foreach (var pair in histories)
                    if (pair.Value.camera == null || frame - pair.Value.lastFrame > 120 || frame < pair.Value.lastFrame)
                        expired.Add(pair.Key);
                foreach (var key in expired) { histories[key].Release(); histories.Remove(key); }
                expired.Clear();
            }
            private void InvalidateHistory(Camera camera,int view) { if(histories.TryGetValue((camera,view),out var h)) { h.Release(); histories.Remove((camera,view)); } }
            private readonly CORTAOAutomaticMotion automaticMotion = new();
            private readonly CORTAOAutomaticSkinnedMotion automaticSkinnedMotion = new();
            public void ClearAutomaticSkinnedMotion() { automaticSkinnedMotion.Clear(); }
            private sealed class MotionDraw
            {
                public Mesh mesh;
                public Matrix4x4 current, previous;
                public bool valid, skinned;
            }
            private sealed class MotionData
            {
                public Material material;
                public TextureHandle depth;
                public Matrix4x4 vp, previousVP, previousView;
                public float tolerance;
                public int eye, shaderPass;
                public bool stereoInput;
                public MotionDraw[] draws;
            }
            private TextureHandle RecordEarlyMotion(RenderGraph graph, Camera camera, TextureHandle depth, int w, int h,
                Matrix4x4 vp, Matrix4x4 previousVP, Matrix4x4 previousView, Matrix4x4 cullingVP, int historyView, bool stereoInput, int eye, int shaderPass)
            {
                var draws=new System.Collections.Generic.List<MotionDraw>();
                if(options.customDeformedMotion && Application.isPlaying)
                {
                    foreach(var tracked in CORTAODeformedMotionObject.Active)
                    {
                        if(tracked==null || !tracked.Snapshot(camera,out var mesh,out var current,out var previous,out var valid)) continue;
                        draws.Add(new MotionDraw { mesh=mesh,current=current,previous=previous,valid=valid,skinned=true });
                    }
                }
                foreach(var tracked in CORTAOMotionObject.Active)
                {
                    if(tracked==null || (options.customDeformedMotion && tracked.GetComponent<CORTAODeformedMotionObject>() != null) || !tracked.Snapshot(camera,out var mesh,out var current,out var previous,out var valid)) continue;
                    draws.Add(new MotionDraw { mesh=mesh,current=current,previous=previous,valid=valid });
                }
                if(options.automaticRigidMotion && Application.isPlaying)
                {
                    var planes = GeometryUtility.CalculateFrustumPlanes(cullingVP);
                    foreach(var entry in automaticMotion.Capture(options.automaticMotionLayers.value))
                    {
                        if(!entry.renderer || (camera.cullingMask & (1 << entry.renderer.gameObject.layer)) == 0 ||
                            !GeometryUtility.TestPlanesAABB(planes, entry.renderer.bounds)) continue;
                        // Stationary surfaces use the existing camera reprojection.
                        if(entry.valid && entry.current.Equals(entry.previous)) continue;
                        draws.Add(new MotionDraw { mesh=entry.mesh,current=entry.current,previous=entry.previous,valid=entry.valid });
                    }
                }
                else automaticMotion.Clear();
                if(options.skinnedMotion && Application.isPlaying)
                {
                    foreach(var tracked in CORTAOSkinnedMotionObject.Active)
                    {
                        if(tracked==null || (options.customDeformedMotion && tracked.GetComponent<CORTAODeformedMotionObject>() != null) || !tracked.Snapshot(camera,out var mesh,out var current,out var previous,out var valid)) continue;
                        draws.Add(new MotionDraw { mesh=mesh,current=current,previous=previous,valid=valid,skinned=true });
                    }
                }
                if(options.skinnedMotion && options.automaticSkinnedMotion && Application.isPlaying)
                {
                    var planes = GeometryUtility.CalculateFrustumPlanes(cullingVP);
                    foreach(var entry in automaticSkinnedMotion.Discover(options.automaticMotionLayers.value))
                    {
                        if(!entry.renderer || !GeometryUtility.TestPlanesAABB(planes, entry.renderer.bounds)) continue;
                        if(!entry.snapshot.Capture(entry.renderer,camera,out var mesh,out var current,out var previous,out var valid)) continue;
                        draws.Add(new MotionDraw { mesh=mesh,current=current,previous=previous,valid=valid,skinned=true });
                    }
                }
                else automaticSkinnedMotion.Clear();
                if(draws.Count==0) return default;
                var output=graph.CreateTexture(new TextureDesc(w,h) { name="CORTAO Early Motion View "+historyView,colorFormat=GraphicsFormat.R32G32B32A32_SFloat,clearBuffer=true,clearColor=Color.clear,filterMode=FilterMode.Point });
                using(var b=graph.AddRasterRenderPass<MotionData>("CORTAO - Early motion View "+historyView,out var d))
                {
                    d.material=motionMaterial; d.depth=depth; d.vp=vp; d.previousVP=previousVP; d.previousView=previousView;
                    d.tolerance=options.motionDeviceDepthTolerance; d.draws=draws.ToArray();
                    d.stereoInput=stereoInput; d.eye=eye; d.shaderPass=shaderPass;
                    b.UseTexture(depth,AccessFlags.Read);
                    b.SetRenderAttachment(output,0,AccessFlags.Write);
                    b.AllowGlobalStateModification(true);
                    b.SetRenderFunc(static (MotionData a,RasterGraphContext c)=>{
                        c.cmd.SetGlobalTexture((a.stereoInput?IDCORTAOMotionDepthArray:IDCORTAOMotionDepth),a.depth);
                        var props=new MaterialPropertyBlock();
                        props.SetFloat(IDCORTAOMotionDepthTolerance,a.tolerance);
                        props.SetInt(IDCORTAOMotionEye,a.eye);
                        foreach(var draw in a.draws)
                        {
                            props.SetMatrix(IDCORTAOMotionCurrentMVP,a.vp*draw.current);
                            props.SetMatrix(IDCORTAOMotionPreviousMVP,a.previousVP*draw.previous);
                            props.SetMatrix(IDCORTAOMotionPreviousMV,a.previousView*draw.previous);
                            props.SetFloat(IDCORTAOMotionObjectValid,draw.valid?1:0);
                            props.SetFloat(IDCORTAOMotionSkinned,draw.skinned?1:0);
                            for(int sub=0;sub<draw.mesh.subMeshCount;sub++)
                                c.cmd.DrawMesh(draw.mesh,draw.current,a.material,sub,a.shaderPass,props);
                        }
                    });
                }
                return output;
            }
            private sealed class TemporalData
            {
                public ComputeShader shader;
                public int kernel,w,h,previousW,previousH,valid,eye;
                public bool stereoInput;
                public bool publish, bentEnabled, publishBent;
                public TextureHandle current,depth,normals,previous,next,result,rejection,currentBent,previousBent,nextBent,resultBent;
                public Matrix4x4 inverseVP,previousVP,previousView,view,inverseView;
                public float weight,depthThreshold,normalThreshold,expansion,direct,response,varianceExpansion;
                public int bilinear, useMotion, lowResolution, historyDebugMode;
                public TextureHandle motion;
            }
            private TextureHandle RecordTemporal(RenderGraph graph,UniversalCameraData cam,TextureHandle depth,TextureHandle normals,TextureHandle input,int w,int h,int eye,int historyView,bool publish,bool stereoInput,TextureHandle bentInput,out TextureHandle rejection,out TextureHandle bentResult,TextureHandle sharedAO=default,TextureHandle sharedBent=default,bool lowResolution=false)
            {
                bool bentEnabled=bentInput.IsValid();
                bentResult=default;
                var camera=cam.camera;
                int frame=Time.frameCount;
                var historyKey=(camera,historyView);
                if(!histories.TryGetValue(historyKey,out var history)) { history=new History { camera=camera }; histories.Add(historyKey,history); }
                bool resize=history.width!=w || history.height!=h;
                if(history.textures[history.index]==null || (history.bentTextures[history.index]!=null)!=bentEnabled || (bentEnabled && history.bentTextures[history.index]!=null && history.bentTextures[history.index].rt.graphicsFormat!=bentFormat) || (resize && !options.preserveHistoryOnResize))
                {
                    history.Release(); history.width=w; history.height=h; history.index=0; history.lastFrame=-100;
                    history.textures[0]=RTHandles.Alloc(w,h,colorFormat:GraphicsFormat.R32G32B32A32_SFloat,enableRandomWrite:true,filterMode:FilterMode.Point,name:"CORTAO History View "+historyView+" Buffer 0");
                    if(bentEnabled) history.bentTextures[0]=RTHandles.Alloc(w,h,colorFormat:bentFormat,enableRandomWrite:true,filterMode:FilterMode.Point,name:"CORTAO Bent History "+historyView+" Buffer 0");
                }
                int previousW=history.width, previousH=history.height;
                int nextIndex=1-history.index;
                var target=history.textures[nextIndex];
                if(target==null || target.rt.width!=w || target.rt.height!=h)
                {
                    target?.Release();
                    history.textures[nextIndex]=RTHandles.Alloc(w,h,colorFormat:GraphicsFormat.R32G32B32A32_SFloat,enableRandomWrite:true,filterMode:FilterMode.Point,name:"CORTAO History View "+historyView+" Buffer "+nextIndex);
                }
                if(bentEnabled)
                {
                    var bentTarget=history.bentTextures[nextIndex];
                    if(bentTarget==null || bentTarget.rt.width!=w || bentTarget.rt.height!=h || bentTarget.rt.graphicsFormat!=bentFormat)
                    {
                        bentTarget?.Release();
                        history.bentTextures[nextIndex]=RTHandles.Alloc(w,h,colorFormat:bentFormat,enableRandomWrite:true,filterMode:FilterMode.Point,name:"CORTAO Bent History "+historyView+" Buffer "+nextIndex);
                    }
                }
                int signature=System.HashCode.Combine(System.HashCode.Combine(options.radius,options.intensity,options.quality,options.lookupQuality,options.resolution,options.normalBiasMultiplier,options.randomSeed,System.HashCode.Combine(lut.count,options.rayCount)),System.HashCode.Combine(options.maxScreenRadius,options.mipBias,options.maxSlopeCompensation,options.backfaceThreshold,options.backfaceBoost,options.filterDepthSensitivity,options.filterNormalSensitivity),options.upsampleDepthThreshold,options.upsampleNormalPower,camera.fieldOfView,camera.nearClipPlane,camera.farClipPlane);
                signature=System.HashCode.Combine(System.HashCode.Combine(signature,camera.orthographic,camera.orthographicSize,options.earlyRigidMotion,options.motionDeviceDepthTolerance,options.automaticRigidMotion,options.automaticMotionLayers.value,options.skinnedMotion),options.automaticSkinnedMotion);
                signature=System.HashCode.Combine(signature,options.enableXRMultipassMotion,options.enableXRSinglePassMotion,cam.xr.enabled ? (int)UnityEngine.XR.XRSettings.stereoRenderingMode : -1);
                signature=System.HashCode.Combine(signature,activeFoveation,options.foveationPolicy,options.customDeformedMotion,lowResolution);
                if(!lowResolution && options.resolution==Resolution.Half)
                    signature=System.HashCode.Combine(signature,options.spatialDenoiseIterations,options.spatialDenoiseStrength);
                var view=cam.GetViewMatrix(eye); var vp=GL.GetGPUProjectionMatrix(cam.GetProjectionMatrix(eye), true)*view;
                bool valid=history.lastFrame==frame-1 && history.signature==signature && visual.Compatible(history.visual) && Vector3.Distance(history.position,camera.transform.position)<Mathf.Max(1,visual.radius*4) && Quaternion.Angle(history.rotation,camera.transform.rotation)<45;
                var previous=graph.ImportTexture(history.textures[history.index]);
                var next=graph.ImportTexture(history.textures[1-history.index]);
                var motion=default(TextureHandle);
                bool fullXRViewport=Mathf.Abs(UnityEngine.XR.XRSettings.appliedRenderViewportScale-1f)<0.0001f;
                bool direct3D=SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D11 || SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D12;
                bool xrMotionAllowed=cam.xr.enabled && !activeFoveation && fullXRViewport &&
                    (stereoInput ? cam.xr.singlePassEnabled && cam.xr.viewCount==2 && options.enableXRSinglePassMotion && direct3D && UnityEngine.XR.XRSettings.stereoRenderingMode != UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassMultiview :
                    !cam.xr.singlePassEnabled && cam.xr.viewCount==1 && options.enableXRMultipassMotion);
                bool fixedViewport=!camera.allowDynamicResolution && camera.rect == new Rect(0,0,1,1);
                if(options.earlyRigidMotion && (!cam.xr.enabled || xrMotionAllowed) && fixedViewport)
                {
                    int motionPass=motionMaterial==null?-1:stereoInput?motionMaterial.FindPass("StereoDepth"):0;
                    if(motionPass>=0) motion=RecordEarlyMotion(graph,camera,depth,w,h,vp,history.vp,history.view,cam.GetProjectionMatrix(eye)*view,historyView,stereoInput,eye,motionPass);
                    else Report("Early motion shader or required StereoDepth pass unavailable. Using camera reprojection.");
                }
                if(options.earlyRigidMotion && cam.xr.enabled && (stereoInput?options.enableXRSinglePassMotion:options.enableXRMultipassMotion) && (!xrMotionAllowed || !fixedViewport))
                    Report("Early XR motion requires a supported fixed-resolution full viewport without foveation (Direct3D for single pass). Using camera reprojection.");
                var result=sharedAO.IsValid()?sharedAO:Make(graph,w,h,aoFormat,"CORTAO Temporal AO");
                rejection=TemporalDebug?Make(graph,w,h,aoFormat,"CORTAO Temporal Debug"):default;
                using(var b=graph.AddComputePass<TemporalData>("CORTAO - Temporal resolve"+EyeLabel(stereoInput,eye),out var d))
                {
                    d.shader=temporal; d.kernel=Kernel(temporal,(bentEnabled ? "ResolveBent" : "Resolve")+(stereoInput?"Stereo":"")+(sharedAO.IsValid()?"Array":"")+(bentEnabled&&sharedBent.IsValid()?"BentArray":"")+(rejection.IsValid()?"Debug":"")); d.eye=eye; d.stereoInput=stereoInput; d.w=w; d.h=h; d.previousW=previousW; d.previousH=previousH; d.valid=valid?1:0;
                    d.motion=motion.IsValid()?motion:previous; d.useMotion=motion.IsValid()?1:0; d.lowResolution=lowResolution?1:0;
                    if(motion.IsValid()) b.UseTexture(motion,AccessFlags.Read);
                    d.current=input; d.depth=depth; d.normals=normals; d.previous=previous; d.next=next; d.result=result; d.rejection=rejection; d.historyDebugMode=options.debugView==DebugView.HistoryWeight?1:0;
                    d.inverseVP=vp.inverse; d.previousVP=history.vp; d.previousView=history.view; d.view=view; d.inverseView=view.inverse;
                    d.bilinear=options.validatedBilinearHistory?1:0; d.response=options.historyResponse; d.weight=options.historyWeight; d.depthThreshold=options.historyDepthThreshold; d.normalThreshold=options.historyNormalThreshold; d.expansion=options.historyClampExpansion; d.varianceExpansion=options.historyVarianceExpansion; d.direct=visual.direct; d.publish=publish||(sharedAO.IsValid()&&eye==1);
                    b.UseTexture(input,AccessFlags.Read); b.UseTexture(d.depth,AccessFlags.Read); b.UseTexture(d.normals,AccessFlags.Read); b.UseTexture(previous,AccessFlags.Read);
                    b.UseTexture(next,AccessFlags.Write); b.UseTexture(result,sharedAO.IsValid()?AccessFlags.ReadWrite:AccessFlags.Write); if(rejection.IsValid()) b.UseTexture(rejection,AccessFlags.Write);
                    d.bentEnabled=bentEnabled;
                    if(bentEnabled)
                    {
                        bentResult=sharedBent.IsValid()?sharedBent:Make(graph,w,h,bentFormat,"CORTAO Temporal Bent Normal");
                        d.currentBent=bentInput; d.previousBent=graph.ImportTexture(history.bentTextures[history.index]); d.nextBent=graph.ImportTexture(history.bentTextures[1-history.index]); d.resultBent=bentResult;
                        b.UseTexture(d.currentBent,AccessFlags.Read); b.UseTexture(d.previousBent,AccessFlags.Read); b.UseTexture(d.nextBent,AccessFlags.Write); b.UseTexture(d.resultBent,sharedBent.IsValid()?AccessFlags.ReadWrite:AccessFlags.Write);
                        d.publishBent=(publish||(sharedBent.IsValid()&&eye==1)) && options.outputBentNormals;
                        if(d.publishBent) { b.SetGlobalTextureAfterPass(d.resultBent,IDCORTAOBentNormalTexture); b.AllowGlobalStateModification(true); }
                    }
                    if(d.publish) { b.SetGlobalTextureAfterPass(result,AOGlobal); b.AllowGlobalStateModification(true); } b.AllowPassCulling(false);
                    b.SetRenderFunc(static (TemporalData a,ComputeGraphContext c)=>{
                        var cmd=c.cmd; var s=a.shader; int k=a.kernel;
                        if(a.stereoInput) cmd.SetComputeIntParam(s,IDCORTAOEye,a.eye);
                        cmd.SetComputeIntParam(s,IDFullWidth,a.w); cmd.SetComputeIntParam(s,IDFullHeight,a.h); cmd.SetComputeIntParam(s,IDHistoryValid,a.valid); cmd.SetComputeIntParam(s,IDPreviousWidth,a.previousW); cmd.SetComputeIntParam(s,IDPreviousHeight,a.previousH);
                        cmd.SetComputeMatrixParam(s,IDInverseVP,a.inverseVP); cmd.SetComputeMatrixParam(s,IDPreviousVP,a.previousVP); cmd.SetComputeMatrixParam(s,IDPreviousView,a.previousView); cmd.SetComputeMatrixParam(s,View,a.view); cmd.SetComputeMatrixParam(s,IDInverseView,a.inverseView);
                        cmd.SetComputeIntParam(s,IDUseEarlyMotion,a.useMotion);
                        cmd.SetComputeIntParam(s,IDLowResolution,a.lowResolution);
                        cmd.SetComputeTextureParam(s,k,IDEarlyMotion,a.motion);
                        cmd.SetComputeIntParam(s,IDValidatedBilinearHistory,a.bilinear); cmd.SetComputeFloatParam(s,IDHistoryResponse,a.response);
                        cmd.SetComputeFloatParam(s,IDHistoryWeight,a.weight); cmd.SetComputeFloatParam(s,IDHistoryDepthThreshold,a.depthThreshold); cmd.SetComputeFloatParam(s,IDHistoryNormalThreshold,a.normalThreshold); cmd.SetComputeFloatParam(s,IDClampExpansion,a.expansion); cmd.SetComputeFloatParam(s,IDVarianceExpansion,a.varianceExpansion);
                        cmd.SetComputeTextureParam(s,k,IDCurrentAO,a.current); cmd.SetComputeTextureParam(s,k,Depth,a.depth); cmd.SetComputeTextureParam(s,k,Normals,a.normals); cmd.SetComputeTextureParam(s,k,IDHistory,a.previous); cmd.SetComputeTextureParam(s,k,IDNextHistory,a.next); cmd.SetComputeTextureParam(s,k,FinalAO,a.result); if(a.rejection.IsValid()) { cmd.SetComputeTextureParam(s,k,IDRejection,a.rejection); cmd.SetComputeIntParam(s,IDHistoryDebugMode,a.historyDebugMode); }
                        if(a.bentEnabled)
                        {
                            cmd.SetComputeTextureParam(s,k,IDCurrentBent,a.currentBent);
                            cmd.SetComputeTextureParam(s,k,IDHistoryBent,a.previousBent);
                            cmd.SetComputeTextureParam(s,k,IDNextBent,a.nextBent);
                            cmd.SetComputeTextureParam(s,k,IDFinalBent,a.resultBent);
                        }
                        cmd.DispatchCompute(s,k,(a.w+7)/8,(a.h+7)/8,1); if(a.publishBent) cmd.EnableKeyword(BentKeyword); if(a.publish) { cmd.SetGlobalVector(AOParam,new Vector4(1,0,0,a.direct)); cmd.EnableKeyword(AOKeyword); }
                    });
                }
                history.index=nextIndex; history.width=w; history.height=h; history.lastFrame=frame; history.signature=signature; history.visual=visual; history.vp=vp; history.view=view; history.position=camera.transform.position; history.rotation=camera.transform.rotation;
                return result;
            }
            public override void OnCameraCleanup(CommandBuffer cmd)
            { if(cmd!=null) { cmd.DisableKeyword(AOKeyword); cmd.DisableKeyword(BentKeyword); } }
        }
    }
}

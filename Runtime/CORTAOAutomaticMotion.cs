using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityCoder.CORTAO
{
    // Per-feature, per-frame snapshots. No components are added to scene objects.
    internal sealed class CORTAOAutomaticMotion
    {
        internal sealed class Entry
        {
            internal MeshRenderer renderer;
            internal Mesh mesh;
            internal Matrix4x4 current, previous;
            internal int frame = -1;
            internal bool valid;
        }
        private Dictionary<MeshRenderer, Entry> entries = new();
        private int frame = -1;
        internal void Clear() { entries.Clear(); frame = -1; }
        internal IEnumerable<Entry> Capture(int layerMask)
        {
            int now = Time.frameCount;
            if (frame == now) return entries.Values;
            var next = new Dictionary<MeshRenderer, Entry>();
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (!renderer || !renderer.enabled || renderer.forceRenderingOff || renderer.isPartOfStaticBatch ||
                    renderer.gameObject.isStatic || renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly ||
                    (layerMask & (1 << renderer.gameObject.layer)) == 0 ||
                    renderer.GetComponentInParent<LODGroup>() != null || renderer.GetComponent<CORTAOMotionObject>() != null || renderer.GetComponent<CORTAODeformedMotionObject>() != null)
                    continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh) continue;
                var mesh = filter.sharedMesh;
                var materials = renderer.sharedMaterials;
                if (materials.Length != mesh.subMeshCount || materials.Length == 0) continue;
                bool supported = true;
                foreach (var mat in materials)
                {
                    if (!mat || !mat.shader || (mat.shader.name != "Universal Render Pipeline/Lit" &&
                        mat.shader.name != "Universal Render Pipeline/Simple Lit") ||
                        mat.renderQueue > (int)RenderQueue.GeometryLast || mat.IsKeywordEnabled("_ALPHATEST_ON") ||
                        (mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") != 0) ||
                        (mat.HasProperty("_Surface") && mat.GetFloat("_Surface") != 0) ||
                        (mat.HasProperty("_Cull") && mat.GetFloat("_Cull") != 2) ||
                        (mat.HasProperty("_ZWrite") && mat.GetFloat("_ZWrite") != 1))
                    { supported = false; break; }
                }
                if (!supported) continue;
                entries.TryGetValue(renderer, out var old);
                var current = renderer.localToWorldMatrix;
                bool valid = old != null && old.frame == now - 1 && old.mesh == mesh;
                next.Add(renderer, new Entry { renderer = renderer, mesh = mesh, current = current,
                    previous = valid ? old.current : current, valid = valid, frame = now });
            }
            entries = next;
            frame = now;
            return entries.Values;
        }
    }
}

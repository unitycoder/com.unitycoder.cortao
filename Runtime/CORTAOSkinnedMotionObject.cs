using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityCoder.CORTAO
{
    [DisallowMultipleComponent, RequireComponent(typeof(SkinnedMeshRenderer))]
    public sealed class CORTAOSkinnedMotionObject : MonoBehaviour
    {
        internal static readonly HashSet<CORTAOSkinnedMotionObject> Active = new();
        private readonly CORTAOSkinnedSnapshot snapshot = new();
        private void OnEnable() { Active.Add(this); }
        private void OnDisable() { Active.Remove(this); snapshot.Release(); }
        internal bool Snapshot(Camera camera, out Mesh mesh, out Matrix4x4 now, out Matrix4x4 old, out bool historyValid)
        {
            return snapshot.Capture(GetComponent<SkinnedMeshRenderer>(), camera, out mesh, out now, out old, out historyValid);
        }
    }

    internal sealed class CORTAOSkinnedSnapshot
    {
        private Mesh baked, source;
        private List<Vector3> currentVertices = new(), previousVertices = new();
        private Matrix4x4 current, previous;
        private int frame = -1;
        private bool valid;

        internal void Release()
        {
            frame = -1; valid = false; source = null;
            currentVertices.Clear(); previousVertices.Clear();
            if (baked != null) { CoreUtils.Destroy(baked); baked = null; }
        }
        internal static bool Eligible(SkinnedMeshRenderer renderer)
        {
            if (!renderer || !renderer.enabled || renderer.forceRenderingOff || !renderer.sharedMesh ||
                renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly ||
                renderer.GetComponentInParent<LODGroup>() != null ||
                !renderer.gameObject.activeInHierarchy) return false;
            var materials = renderer.sharedMaterials;
            if (materials.Length == 0 || materials.Length != renderer.sharedMesh.subMeshCount) return false;
            foreach (var mat in materials)
            {
                if (!mat || !mat.shader || (mat.shader.name != "Universal Render Pipeline/Lit" &&
                    mat.shader.name != "Universal Render Pipeline/Simple Lit") ||
                    mat.renderQueue > (int)RenderQueue.GeometryLast || mat.IsKeywordEnabled("_ALPHATEST_ON") ||
                    (mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") != 0) ||
                    (mat.HasProperty("_Surface") && mat.GetFloat("_Surface") != 0) ||
                    (mat.HasProperty("_Cull") && mat.GetFloat("_Cull") != 2) ||
                    (mat.HasProperty("_ZWrite") && mat.GetFloat("_ZWrite") != 1)) return false;
            }
            return true;
        }
        internal bool Capture(SkinnedMeshRenderer renderer, Camera camera, out Mesh mesh, out Matrix4x4 now, out Matrix4x4 old, out bool historyValid)
        {
            mesh = null; now = old = Matrix4x4.identity; historyValid = false;
            if (!Eligible(renderer)) { Release(); return false; }
            if ((camera.cullingMask & (1 << renderer.gameObject.layer)) == 0) return false;
            int f = Time.frameCount;
            if (frame != f)
            {
                if (baked == null) { baked = new Mesh { name = "CORTAO Skinned Motion", hideFlags = HideFlags.HideAndDontSave }; baked.MarkDynamic(); }
                var swap = previousVertices; previousVertices = currentVertices; currentVertices = swap;
                previous = current;
                current = renderer.transform.localToWorldMatrix;
                renderer.BakeMesh(baked, false);
                baked.GetVertices(currentVertices);
                valid = frame == f - 1 && source == renderer.sharedMesh && previousVertices.Count == currentVertices.Count;
                // TEXCOORD7 is private to the temporary mesh and carries previous local positions.
                baked.SetUVs(7, valid ? previousVertices : currentVertices);
                source = renderer.sharedMesh;
                frame = f;
            }
            mesh = baked; now = current; old = valid ? previous : current; historyValid = valid;
            return baked != null && baked.vertexCount > 0;
        }
    }
}

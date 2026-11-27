using System.Collections.Generic;
using UnityEngine;

namespace UnityCoder.CORTAO
{
    // Opt-in rigid opaque geometry. Do not use on alpha-clipped, deformed or LOD meshes.
    [DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CORTAOMotionObject : MonoBehaviour
    {
        internal static readonly HashSet<CORTAOMotionObject> Active = new();
        private Matrix4x4 current, previous;
        private int frame = -1;
        private bool valid;
        private Mesh lastMesh;
        private void OnEnable() { frame = -1; valid = false; Active.Add(this); }
        private void OnDisable() { Active.Remove(this); frame = -1; }
        internal bool Snapshot(Camera camera, out Mesh mesh, out Matrix4x4 now, out Matrix4x4 old, out bool historyValid)
        {
            var renderer = GetComponent<MeshRenderer>();
            mesh = GetComponent<MeshFilter>().sharedMesh;
            int f = Time.frameCount;
            if (frame != f)
            {
                valid = frame == f - 1 && mesh == lastMesh;
                previous = current;
                current = transform.localToWorldMatrix;
                lastMesh = mesh;
                frame = f;
            }
            now = current; old = previous; historyValid = valid;
            return mesh != null && renderer.enabled && !renderer.forceRenderingOff &&
                !renderer.isPartOfStaticBatch && (camera.cullingMask & (1 << gameObject.layer)) != 0;
        }
    }
}

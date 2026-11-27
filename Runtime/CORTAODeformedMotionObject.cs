using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityCoder.CORTAO
{
    // Submit a CPU representation of the actual rendered opaque geometry each frame.
    [DisallowMultipleComponent]
    public sealed class CORTAODeformedMotionObject : MonoBehaviour
    {
        internal static readonly HashSet<CORTAODeformedMotionObject> Active = new();
        private Mesh proxy;
        private Renderer target;
        private Matrix4x4 current, previous;
        private int submittedFrame = -1;
        private bool valid;

        private void OnEnable() { target = GetComponent<Renderer>(); Active.Add(this); }
        private void OnDisable()
        {
            Active.Remove(this);
            submittedFrame = -1;
            valid = false;
            if (proxy != null) CoreUtils.Destroy(proxy);
            proxy = null;
        }

        // Both position arrays must match the topology of currentGeometry. Previous
        // positions and matrix must describe the preceding rendered frame.
        // Call once per frame before any camera renders, on the main thread.
        public void Submit(Mesh currentGeometry, List<Vector3> previousPositions,
            Matrix4x4 currentLocalToWorld, Matrix4x4 previousLocalToWorld, bool historyValid)
        {
            if (!isActiveAndEnabled) throw new InvalidOperationException("Motion provider is disabled.");
            if (submittedFrame == Time.frameCount) throw new InvalidOperationException("Submit only once per frame.");
            if (!currentGeometry || !currentGeometry.isReadable || currentGeometry.vertexCount == 0)
                throw new ArgumentException("A readable nonempty mesh is required.", nameof(currentGeometry));
            for (int i = 0; i < currentGeometry.subMeshCount; i++)
                if (currentGeometry.GetTopology(i) != MeshTopology.Triangles)
                    throw new ArgumentException("Only triangle meshes are supported.", nameof(currentGeometry));
            if (historyValid && (previousPositions == null || previousPositions.Count != currentGeometry.vertexCount))
                throw new ArgumentException("Previous positions must match vertex count.", nameof(previousPositions));
            var next = Instantiate(currentGeometry);
            next.name = "CORTAO Custom Motion Proxy";
            next.hideFlags = HideFlags.HideAndDontSave;
            next.SetUVs(7, historyValid ? previousPositions : new List<Vector3>(currentGeometry.vertices));
            if (proxy != null) CoreUtils.Destroy(proxy);
            proxy = next;
            valid = historyValid && submittedFrame == Time.frameCount - 1;
            current = currentLocalToWorld;
            previous = valid ? previousLocalToWorld : currentLocalToWorld;
            submittedFrame = Time.frameCount;
        }

        internal bool Snapshot(Camera camera, out Mesh mesh, out Matrix4x4 now, out Matrix4x4 old, out bool historyValid)
        {
            mesh = proxy; now = current; old = previous; historyValid = valid;
            return submittedFrame == Time.frameCount && mesh != null && target != null &&
                target.enabled && !target.forceRenderingOff && !target.isPartOfStaticBatch &&
                target.shadowCastingMode != ShadowCastingMode.ShadowsOnly &&
                (camera.cullingMask & (1 << gameObject.layer)) != 0;
        }
    }
}

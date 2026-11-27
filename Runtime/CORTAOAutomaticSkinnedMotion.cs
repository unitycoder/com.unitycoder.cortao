using System.Collections.Generic;
using UnityEngine;

namespace UnityCoder.CORTAO
{
    // Owns temporary meshes; never adds components to scene objects.
    internal sealed class CORTAOAutomaticSkinnedMotion
    {
        internal sealed class Entry
        {
            internal SkinnedMeshRenderer renderer;
            internal CORTAOSkinnedSnapshot snapshot = new();
        }
        private Dictionary<SkinnedMeshRenderer, Entry> entries = new();
        private int frame = -1;
        private int layers;
        internal void Clear()
        {
            foreach (var entry in entries.Values) entry.snapshot.Release();
            entries.Clear(); frame = -1;
        }
        internal IEnumerable<Entry> Discover(int layerMask)
        {
            int now = Time.frameCount;
            if (frame == now && layers == layerMask) return entries.Values;
            var next = new Dictionary<SkinnedMeshRenderer, Entry>();
            foreach (var renderer in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (!renderer || (layerMask & (1 << renderer.gameObject.layer)) == 0 ||
                    renderer.GetComponent<CORTAOSkinnedMotionObject>() != null || renderer.GetComponent<CORTAODeformedMotionObject>() != null ||
                    !CORTAOSkinnedSnapshot.Eligible(renderer)) continue;
                if (!entries.TryGetValue(renderer, out var entry)) entry = new Entry { renderer = renderer };
                next.Add(renderer, entry);
            }
            foreach (var pair in entries)
                if (!next.ContainsKey(pair.Key)) pair.Value.snapshot.Release();
            entries = next; frame = now; layers = layerMask;
            return entries.Values;
        }
    }
}

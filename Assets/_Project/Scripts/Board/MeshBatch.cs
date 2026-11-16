using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EcoTilt.Board
{
    /// <summary>
    /// Junta muchas cajas en una sola malla por material (por ejemplo, cientos de barrotes de baranda).
    /// Así el tablero se dibuja con pocas llamadas, importante en Android.
    /// </summary>
    public sealed class MeshBatch
    {
        private readonly Dictionary<Material, List<CombineInstance>> _parts = new Dictionary<Material, List<CombineInstance>>();
        private readonly Mesh _cube;

        public MeshBatch(Mesh cube)
        {
            _cube = cube;
        }

        public void AddBox(Material material, Vector3 center, Vector3 size, Quaternion rotation)
        {
            if (material == null) return;
            if (!_parts.TryGetValue(material, out List<CombineInstance> list))
            {
                list = new List<CombineInstance>();
                _parts.Add(material, list);
            }
            list.Add(new CombineInstance { mesh = _cube, transform = Matrix4x4.TRS(center, rotation, size) });
        }

        public void AddBox(Material material, Vector3 center, Vector3 size)
        {
            AddBox(material, center, size, Quaternion.identity);
        }

        /// <summary>Crea un hijo por material bajo <paramref name="parent"/> con la malla combinada.</summary>
        public void Bake(Transform parent, string prefix, IBoardBuildSink sink)
        {
            foreach (KeyValuePair<Material, List<CombineInstance>> pair in _parts)
            {
                var mesh = new Mesh { name = $"{prefix}_{pair.Key.name}", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                sink.OnMeshCreated(mesh);

                var go = new GameObject(mesh.name);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = pair.Key;
            }
            _parts.Clear();
        }
    }
}

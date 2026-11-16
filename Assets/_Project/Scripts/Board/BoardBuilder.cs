using System.Collections.Generic;
using EcoTilt.Level;
using UnityEngine;

namespace EcoTilt.Board
{
    /// <summary>
    /// Arma (o rearma) el tablero como hijo de este objeto a partir de un laberinto.
    /// Lo usa el editor para crear la escena y el juego para regenerar el mapa al perder las vidas.
    /// </summary>
    public sealed class BoardBuilder : MonoBehaviour
    {
        private const string GeneratedName = "Generado";

        [SerializeField] private BoardLayout layout;
        [SerializeField] private BoardTheme theme;
        [SerializeField] private BoardDimensions dimensions = new BoardDimensions();
        [Tooltip("Contenedores en orden: plástico, metal, cartón.")]
        [SerializeField] private GameObject[] binPrefabs = new GameObject[0];
        [SerializeField] private Transform spawnPoint;

        private readonly List<Mesh> _runtimeMeshes = new List<Mesh>();

        public BoardLayout Layout => layout;
        public Transform SpawnPoint => spawnPoint;

        /// <summary>Configura el builder desde código (lo usa el generador de escena del editor).</summary>
        public void Configure(BoardLayout boardLayout, BoardTheme boardTheme, GameObject[] bins)
        {
            layout = boardLayout;
            theme = boardTheme;
            binPrefabs = bins;
        }

        /// <summary>Borra el tablero actual y construye uno nuevo a partir de <paramref name="grid"/>.</summary>
        public BoardBuildResult Rebuild(MazeGrid grid, IBoardBuildSink sink = null)
        {
            sink = sink ?? new RuntimeSink(_runtimeMeshes);
            Clear();

            var root = new GameObject(GeneratedName).transform;
            root.SetParent(transform, false);

            BoardBuildResult result = new BoardGeometry(grid, layout, dimensions, theme, sink).Build(root);
            for (int i = 0; i < binPrefabs.Length && i < result.BinSlots.Count; i++)
                if (binPrefabs[i] != null)
                    sink.Instantiate(binPrefabs[i], result.BinSlots[i]);

            spawnPoint = result.SpawnPoint;
            return result;
        }

        private void Clear()
        {
            Transform old = transform.Find(GeneratedName);
            if (old != null)
            {
                if (Application.isPlaying) Destroy(old.gameObject);
                else DestroyImmediate(old.gameObject);
            }

            foreach (Mesh mesh in _runtimeMeshes)
                if (mesh != null) Destroy(mesh);
            _runtimeMeshes.Clear();
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in _runtimeMeshes)
                if (mesh != null) Destroy(mesh);
        }

        /// <summary>Durante el juego las mallas quedan en memoria y se liberan al regenerar.</summary>
        private sealed class RuntimeSink : IBoardBuildSink
        {
            private readonly List<Mesh> _meshes;

            public RuntimeSink(List<Mesh> meshes)
            {
                _meshes = meshes;
            }

            public void OnMeshCreated(Mesh mesh) => _meshes.Add(mesh);

            public GameObject Instantiate(GameObject prefab, Transform parent) => Object.Instantiate(prefab, parent);
        }
    }
}

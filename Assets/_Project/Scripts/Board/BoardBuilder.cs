using System;
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
        [Tooltip("Contenedores (uno por categoría). En cada mapa se reparten al azar.")]
        [SerializeField] private GameObject[] binPrefabs = new GameObject[0];
        [Tooltip("Cuántos contenedores van hundidos en callejones del laberinto; el resto queda tras la rampa.")]
        [SerializeField, Range(0, 2)] private int binsInsideMaze = 1;
        [SerializeField] private Transform spawnPoint;

        private readonly List<Mesh> _runtimeMeshes = new List<Mesh>();

        public BoardLayout Layout => layout;

        /// <summary>Se dispara cada vez que el tablero se reconstruye (los contenedores son nuevos).</summary>
        public event Action Rebuilt;
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

            var random = new System.Random(Environment.TickCount);
            BoardBuildResult result = new BoardGeometry(grid, layout, dimensions, theme, sink,
                                                        binsInsideMaze, binPrefabs.Length, random).Build(root);

            // Orden aleatorio: en cada mapa cambia qué contenedor queda en el laberinto y cuál en cada lado de la rampa.
            var order = new List<GameObject>(binPrefabs);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            for (int i = 0; i < order.Count && i < result.BinSlots.Count; i++)
                if (order[i] != null)
                    sink.Instantiate(order[i], result.BinSlots[i]);

            spawnPoint = result.SpawnPoint;
            Rebuilt?.Invoke();
            return result;
        }

        private void Clear()
        {
            Transform old = transform.Find(GeneratedName);
            if (old != null)
            {
                old.SetParent(null); // fuera de la jerarquía ya mismo, aunque se destruya al final del frame
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

            public GameObject Instantiate(GameObject prefab, Transform parent) => UnityEngine.Object.Instantiate(prefab, parent);
        }
    }
}

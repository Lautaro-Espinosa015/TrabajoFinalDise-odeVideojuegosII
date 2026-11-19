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
        [Header("Límites invisibles")]
        [SerializeField, Min(0.1f)] private float containmentMargin = 0.5f;
        [SerializeField, Min(2f)] private float containmentHeight = 4.5f;
        [SerializeField, Min(0.2f)] private float containmentThickness = 0.6f;

        private void Awake() => EnsureContainment();

        // También protege escenas ya construidas, sin regenerar su mapa ni sus objetos.
        public void EnsureContainment()
        {
            Transform root = transform.Find(GeneratedName) ?? transform;
            if (root.Find("LimitesInvisibles") != null) return;
            Bounds bounds = new Bounds();
            bool found = false;
            foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>())
            {
                if (box.isTrigger || !box.enabled ||
                    (box.attachedRigidbody != null && box.attachedRigidbody.transform != transform)) continue;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = box.center + Vector3.Scale(box.size * 0.5f,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 point = root.InverseTransformPoint(box.transform.TransformPoint(corner));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!found) return;
            var limits = new GameObject("LimitesInvisibles").transform;
            limits.SetParent(root, false);
            float t = Mathf.Max(0.2f, containmentThickness);
            float bottom = bounds.min.y - 2f;
            float top = Mathf.Max(containmentHeight, bounds.max.y + 2f);
            float x = bounds.size.x + 2f * containmentMargin;
            float z = bounds.size.z + 2f * containmentMargin;
            Vector3 c = new Vector3(bounds.center.x, (bottom + top) * 0.5f, bounds.center.z);
            void Wall(string name, Vector3 center, Vector3 size)
            {
                var wall = new GameObject(name);
                wall.transform.SetParent(limits, false);
                wall.transform.localPosition = center;
                wall.AddComponent<BoxCollider>().size = size;
            }
            Wall("Izquierda", c + Vector3.left * ((x + t) * 0.5f), new Vector3(t, top - bottom, z + 2f * t));
            Wall("Derecha", c + Vector3.right * ((x + t) * 0.5f), new Vector3(t, top - bottom, z + 2f * t));
            Wall("Frente", c + Vector3.back * ((z + t) * 0.5f), new Vector3(x + 2f * t, top - bottom, t));
            Wall("Fondo", c + Vector3.forward * ((z + t) * 0.5f), new Vector3(x + 2f * t, top - bottom, t));
            Wall("Techo", new Vector3(c.x, top + t * 0.5f, c.z), new Vector3(x + 2f * t, t, z + 2f * t));
            // Sin piso adicional: los pozos y el espacio del salto siguen abiertos.
        }

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

            EnsureContainment();
            spawnPoint = result.SpawnPoint;
            Rebuilt?.Invoke();
            return result;
        }

        private void Clear()
        {
            Transform old = transform.Find(GeneratedName);
            if (old != null)
            {
                old.gameObject.SetActive(false);
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

using System.Collections;
using System.Collections.Generic;
using EcoTilt.Board;
using UnityEngine;

namespace EcoTilt.Waste
{
    /// <summary>
    /// Mantiene residuos en juego: coloca varios en el inicio y, cada vez que uno se separa, agrega otro de
    /// tipo aleatorio hasta completar la cantidad del nivel. Es la única clase que sabe qué residuos están activos.
    /// </summary>
    public sealed class WasteSpawner : MonoBehaviour
    {
        [SerializeField] private BoardBuilder board;
        [SerializeField] private RecyclingCenter center;
        [SerializeField] private WasteItem[] prefabs = new WasteItem[0];
        [Tooltip("Cuántos residuos hay a la vez sobre el tablero.")]
        [SerializeField, Range(1, 5)] private int activeCount = 3;
        [SerializeField, Min(0f)] private float spawnDelay = 1f;
        [Tooltip("Desplazamientos respecto del punto de inicio para que no aparezcan encimados.")]
        [SerializeField] private Vector3[] slotOffsets =
        {
            new Vector3(-0.45f, 0f, 0f), new Vector3(0f, 0.3f, 0f), new Vector3(0.45f, 0.6f, 0f)
        };

        private readonly List<WasteItem> _active = new List<WasteItem>();
        private int _spawned;
        private int _quota = int.MaxValue;
        private int _spawnedThisLevel;

        public IReadOnlyList<WasteItem> Active => _active;

        private void OnEnable()
        {
            if (center != null) center.Sorted += OnSorted;
        }

        private void OnDisable()
        {
            if (center != null) center.Sorted -= OnSorted;
        }

        /// <summary>
        /// Empieza un nivel con <paramref name="quota"/> residuos en total. Los que ya están en juego cuentan;
        /// se completa hasta tener <c>activeCount</c> a la vez.
        /// </summary>
        public void BeginLevel(int quota)
        {
            _quota = quota;
            _spawnedThisLevel = _active.Count;
            while (_active.Count < activeCount && _spawnedThisLevel < _quota)
                if (Spawn() == null) break;
        }

        private void OnSorted(WasteSortResult result)
        {
            _active.Remove(result.Item);
            StartCoroutine(SpawnLater());
        }

        private IEnumerator SpawnLater()
        {
            yield return new WaitForSeconds(spawnDelay);
            if (_active.Count < activeCount) Spawn();
        }

        public WasteItem Spawn()
        {
            if (prefabs.Length == 0 || board == null || board.SpawnPoint == null) return null;
            if (_spawnedThisLevel >= _quota) return null;
            _spawnedThisLevel++;

            Transform point = board.SpawnPoint;
            WasteItem prefab = prefabs[Random.Range(0, prefabs.Length)];
            Vector3 offset = slotOffsets.Length > 0 ? slotOffsets[_spawned % slotOffsets.Length] : Vector3.zero;
            _spawned++;

            WasteItem item = Instantiate(prefab, point.position + point.rotation * offset,
                                         point.rotation * prefab.RestRotation, transform);
            item.Respawnable.Configure(point, offset);
            _active.Add(item);
            return item;
        }

        /// <summary>Oculta y congela todos los residuos activos (por ejemplo, al regenerar el mapa).</summary>
        public void SuspendAll()
        {
            foreach (WasteItem item in _active) item.Respawnable.Suspend();
        }

        /// <summary>Lleva todos los residuos activos al punto de inicio actual del tablero.</summary>
        public void ResetAllToSpawn()
        {
            foreach (WasteItem item in _active)
            {
                item.Respawnable.SetSpawnPoint(board.SpawnPoint);
                item.Respawnable.ResetToSpawn();
            }
        }
    }
}

using System;
using System.Collections;
using EcoTilt.Board;
using EcoTilt.Level;
using EcoTilt.Waste;
using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>
    /// Al quedarse sin vidas, genera un laberinto nuevo (siempre distinto), rearma el tablero,
    /// devuelve los residuos activos al inicio y restablece las vidas.
    /// </summary>
    public sealed class LevelRegenerator : MonoBehaviour
    {
        [SerializeField] private BoardBuilder board;
        [SerializeField] private LivesSystem lives;
        [SerializeField] private WasteSpawner spawner;
        [SerializeField] private MazeSettings maze = new MazeSettings();
        [Tooltip("Espera antes de rearmar, para que el jugador vea que perdió.")]
        [SerializeField, Min(0f)] private float delay = 1.5f;
        [Tooltip("Si está activo, la primera partida también usa un mapa aleatorio en vez del diseñado.")]
        [SerializeField] private bool randomMapOnStart;

        private System.Random _random;

        public int MapNumber { get; private set; } = 1;
        public bool IsRegenerating { get; private set; }

        /// <summary>Se dispara con el número de mapa cuando el tablero nuevo ya está listo.</summary>
        public event Action<int> MapRegenerated;

        private void Awake()
        {
            _random = new System.Random(Environment.TickCount);
        }

        private void Start()
        {
            if (randomMapOnStart) StartCoroutine(Regenerate(0f));
        }

        private void OnEnable()
        {
            if (lives != null) lives.OutOfLives += OnOutOfLives;
        }

        private void OnDisable()
        {
            if (lives != null) lives.OutOfLives -= OnOutOfLives;
        }

        private void OnOutOfLives()
        {
            RequestNewMap(delay);
        }

        /// <summary>Arma un laberinto nuevo después de <paramref name="wait"/> segundos (si no se está armando ya).</summary>
        public void RequestNewMap(float wait)
        {
            if (!IsRegenerating) StartCoroutine(Regenerate(wait));
        }

        private IEnumerator Regenerate(float wait)
        {
            IsRegenerating = true;
            yield return new WaitForSeconds(wait);

            spawner.SuspendAll();

            string map = MazeGenerator.Generate(maze, _random);
            board.Rebuild(MazeGrid.Parse(map));

            // Esperar un paso de física para que los colliders nuevos estén activos antes de soltar los residuos.
            yield return new WaitForFixedUpdate();

            spawner.ResetAllToSpawn();

            lives.ResetLives();
            MapNumber++;
            IsRegenerating = false;
            MapRegenerated?.Invoke(MapNumber);
        }
    }
}

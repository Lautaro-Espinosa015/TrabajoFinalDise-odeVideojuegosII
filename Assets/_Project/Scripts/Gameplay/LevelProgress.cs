using System;
using EcoTilt.Waste;
using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>
    /// Progreso del nivel: cada nivel trae una cantidad fija de residuos. Cada acierto devuelve una vida;
    /// al separarlos todos se recargan las vidas, se felicita al jugador y se arma un laberinto nuevo
    /// (siguiente nivel). Si se pierden las vidas, el laberinto cambia pero el nivel se repite.
    /// </summary>
    public sealed class LevelProgress : MonoBehaviour
    {
        [SerializeField] private RecyclingCenter center;
        [SerializeField] private LivesSystem lives;
        [SerializeField] private LevelRegenerator regenerator;
        [SerializeField] private WasteSpawner spawner;
        [Tooltip("Residuos que trae cada nivel (en el tablero hay hasta 3 a la vez).")]
        [SerializeField, Min(1)] private int wastePerLevel = 6;
        [Tooltip("Segundos entre completar el nivel y que aparezca el laberinto nuevo.")]
        [SerializeField, Min(0f)] private float nextLevelDelay = 2.5f;

        public int Level { get; private set; } = 1;
        public int SortedThisLevel { get; private set; }
        public int CorrectThisLevel { get; private set; }
        public int WastePerLevel => wastePerLevel;
        public bool IsCompleting { get; private set; }

        /// <summary>Un residuo fue separado; el bool indica si devolvió una vida.</summary>
        public event Action<WasteSortResult, bool> ItemSorted;
        /// <summary>Nivel completado: número de nivel, aciertos y total de residuos.</summary>
        public event Action<int, int, int> LevelCompleted;

        private void OnEnable()
        {
            if (center != null) center.Sorted += OnSorted;
            if (regenerator != null) regenerator.MapRegenerated += OnMapRegenerated;
        }

        private void OnDisable()
        {
            if (center != null) center.Sorted -= OnSorted;
            if (regenerator != null) regenerator.MapRegenerated -= OnMapRegenerated;
        }

        private void Start()
        {
            spawner.BeginLevel(wastePerLevel);
        }

        private void OnSorted(WasteSortResult result)
        {
            SortedThisLevel++;
            if (result.Correct) CorrectThisLevel++;

            bool lifeGained = result.Correct && lives.AddLife();
            ItemSorted?.Invoke(result, lifeGained);

            if (SortedThisLevel < wastePerLevel || IsCompleting) return;
            IsCompleting = true;
            lives.ResetLives();
            LevelCompleted?.Invoke(Level, CorrectThisLevel, wastePerLevel);
            regenerator.RequestNewMap(nextLevelDelay);
        }

        private void OnMapRegenerated(int mapNumber)
        {
            if (IsCompleting)
            {
                Level++;
                IsCompleting = false;
            }
            SortedThisLevel = 0;
            CorrectThisLevel = 0;
            spawner.BeginLevel(wastePerLevel);
        }
    }
}

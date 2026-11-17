using System;
using UnityEngine;

namespace EcoTilt.Gameplay
{
    /// <summary>
    /// Cuenta vidas: cada objeto que cae fuera del tablero resta una.
    /// Solo cuenta y avisa; qué pasa al quedarse sin vidas lo decide quien escuche el evento (SRP).
    /// </summary>
    public sealed class LivesSystem : MonoBehaviour
    {
        [SerializeField] private KillZone killZone;
        [SerializeField, Min(1)] private int maxLives = 3;

        public int Lives { get; private set; }
        public int MaxLives => maxLives;

        public event Action<int> LivesChanged;
        public event Action OutOfLives;

        private void Awake()
        {
            Lives = maxLives;
        }

        private void OnEnable()
        {
            if (killZone != null) killZone.Fell += OnFell;
        }

        private void OnDisable()
        {
            if (killZone != null) killZone.Fell -= OnFell;
        }

        /// <summary>Suma una vida si no está al máximo. Devuelve true si la sumó.</summary>
        public bool AddLife()
        {
            if (Lives >= maxLives) return false;
            Lives++;
            LivesChanged?.Invoke(Lives);
            return true;
        }

        public void ResetLives()
        {
            Lives = maxLives;
            LivesChanged?.Invoke(Lives);
        }

        private void OnFell(IRespawnable _)
        {
            if (Lives <= 0) return;
            Lives--;
            LivesChanged?.Invoke(Lives);
            if (Lives == 0) OutOfLives?.Invoke();
        }
    }
}

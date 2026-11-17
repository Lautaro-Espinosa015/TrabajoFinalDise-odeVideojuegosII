using System;
using UnityEngine;

namespace EcoTilt.Waste
{
    /// <summary>Puntaje acumulado de la partida: aciertos, errores y puntos.</summary>
    public sealed class ScoreKeeper : MonoBehaviour
    {
        [SerializeField] private RecyclingCenter center;
        [SerializeField] private int correctPoints = 100;
        [SerializeField] private int wrongPoints = -50;

        public int Correct { get; private set; }
        public int Wrong { get; private set; }
        public int Score { get; private set; }

        public event Action Changed;

        private void OnEnable()
        {
            if (center != null) center.Sorted += OnSorted;
        }

        private void OnDisable()
        {
            if (center != null) center.Sorted -= OnSorted;
        }

        private void OnSorted(WasteSortResult result)
        {
            if (result.Correct)
            {
                Correct++;
                Score += correctPoints;
            }
            else
            {
                Wrong++;
                Score = Mathf.Max(0, Score + wrongPoints);
            }
            Changed?.Invoke();
        }
    }
}

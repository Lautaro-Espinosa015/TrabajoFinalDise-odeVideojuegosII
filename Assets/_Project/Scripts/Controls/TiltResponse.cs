using System;
using UnityEngine;

namespace EcoTilt.Controls
{
    /// <summary>Cómo responde el tablero a los movimientos del teléfono.</summary>
    public enum TiltResponseMode
    {
        /// <summary>El tablero copia al teléfono casi sin demora (1 a 1, hasta 90°).</summary>
        Fiel = 0,
        /// <summary>Movimientos más tranquilos: menos ángulo, filtro y giro limitado.</summary>
        Suave = 1
    }

    /// <summary>Valores que definen una respuesta del tablero.</summary>
    [Serializable]
    public struct TiltResponseSettings
    {
        [Tooltip("Ángulo máximo que puede alcanzar el tablero.")]
        [Range(10f, 90f)] public float MaxAngle;
        [Tooltip("1 = espejo exacto del teléfono. Menos de 1 suaviza, más de 1 exagera.")]
        [Range(0.3f, 2f)] public float Sensitivity;
        [Tooltip("Filtro adaptativo: frecuencia de corte con el teléfono casi quieto (Hz). 0 = sin filtro. " +
                 "Más bajo = menos temblor transmitido al tablero.")]
        [Range(0f, 10f)] public float Smoothing;
        [Tooltip("Cuánto se abre el filtro cuando el teléfono gira rápido: más alto = menos demora en los giros.")]
        [Range(0f, 5f)] public float SmoothingSpeedGain;
        [Tooltip("Inclinaciones menores a este ángulo se ignoran (temblor de la mano).")]
        [Range(0f, 10f)] public float DeadZone;
        [Tooltip("Velocidad de giro máxima en grados por segundo. 0 = sin límite.")]
        [Min(0f)] public float MaxDegreesPerSecond;

        public static TiltResponseSettings Faithful => new TiltResponseSettings
        {
            MaxAngle = 90f, Sensitivity = 1f, Smoothing = 2f, SmoothingSpeedGain = 1.5f, DeadZone = 1f, MaxDegreesPerSecond = 0f
        };

        public static TiltResponseSettings Smooth => new TiltResponseSettings
        {
            MaxAngle = 30f, Sensitivity = 0.6f, Smoothing = 1f, SmoothingSpeedGain = 0.5f, DeadZone = 2f, MaxDegreesPerSecond = 150f
        };
    }

    /// <summary>Algo que permite elegir la respuesta del tablero (lo usa el menú de pausa).</summary>
    public interface ITiltResponseSelector
    {
        TiltResponseMode ResponseMode { get; set; }
    }

    /// <summary>Guarda la respuesta elegida entre partidas.</summary>
    public static class TiltResponsePreference
    {
        private const string Key = "RespuestaTablero";

        public static TiltResponseMode Load() =>
            PlayerPrefs.GetInt(Key, (int)TiltResponseMode.Fiel) == (int)TiltResponseMode.Suave
                ? TiltResponseMode.Suave
                : TiltResponseMode.Fiel;

        public static void Save(TiltResponseMode mode)
        {
            PlayerPrefs.SetInt(Key, (int)mode);
            PlayerPrefs.Save();
        }
    }
}

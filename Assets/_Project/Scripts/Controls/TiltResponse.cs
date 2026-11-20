using System;
using UnityEngine;

namespace EcoTilt.Controls
{
    /// <summary>Cómo responde el tablero a los movimientos del teléfono.</summary>
    public enum TiltResponseMode
    {
        /// <summary>Respuesta moderada, ligeramente más potente que Suave y ajustable.</summary>
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
        [Tooltip("Velocidad del filtro de lecturas: 0 = sin filtro. Más alto = sigue más rápido " +
                 "(8 ≈ 0,13 s de demora, 30 ≈ 0,03 s).")]
        [Range(0f, 60f)] public float Smoothing;
        [Tooltip("Inclinaciones menores a este ángulo se ignoran (temblor de la mano).")]
        [Range(0f, 10f)] public float DeadZone;
        [Tooltip("Velocidad de giro máxima en grados por segundo. 0 = sin límite.")]
        [Min(0f)] public float MaxDegreesPerSecond;

        public static TiltResponseSettings Faithful => new TiltResponseSettings
        {
            MaxAngle = 35f, Sensitivity = 0.7f, Smoothing = 10f, DeadZone = 2f, MaxDegreesPerSecond = 180f
        };

        public static TiltResponseSettings Smooth => new TiltResponseSettings
        {
            MaxAngle = 30f, Sensitivity = 0.6f, Smoothing = 8f, DeadZone = 2f, MaxDegreesPerSecond = 150f
        };
    }

    /// <summary>Algo que permite elegir la respuesta del tablero (lo usa el menú de pausa).</summary>
    public interface ITiltResponseSelector
    {
        TiltResponseMode ResponseMode { get; set; }
        float FaithfulSensitivity { get; set; }
    }

    /// <summary>Guarda la respuesta elegida entre partidas.</summary>
    public static class TiltResponsePreference
    {
        private const string Key = "RespuestaTablero";
        private const string SensitivityKey = "SensibilidadFiel";
        public const float MinSensitivity = 0.6f;
        public const float MaxSensitivity = 0.8f;

        public static float LoadSensitivity(float fallback = 0.7f) =>
            Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, fallback), MinSensitivity, MaxSensitivity);

        public static void SaveSensitivity(float value) =>
            PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, MinSensitivity, MaxSensitivity));

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

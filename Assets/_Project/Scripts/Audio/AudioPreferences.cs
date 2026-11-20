using UnityEngine;

namespace EcoTilt.Audio
{
    /// <summary>
    /// Volúmenes elegidos en Opciones. Se leen de PlayerPrefs una sola vez y quedan en memoria:
    /// en Android cada lectura de PlayerPrefs pasa por el sistema y es lenta, y los sonidos la pedían en
    /// cada cuadro (un residuo, la música…).
    /// </summary>
    public static class AudioPreferences
    {
        private const string MusicKey = "VolumenMusica";
        private const string EffectsKey = "VolumenEfectos";
        public const float DefaultMusic = 0.35f;
        public const float DefaultEffects = 0.85f;

        private static float? _music;
        private static float? _effects;

        public static float Music
        {
            get => _music ??= Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, DefaultMusic));
            set
            {
                _music = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicKey, _music.Value);
            }
        }

        public static float Effects
        {
            get => _effects ??= Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, DefaultEffects));
            set
            {
                _effects = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(EffectsKey, _effects.Value);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            // Sin "Domain Reload" en el editor los valores estáticos sobreviven entre Plays.
            _music = null;
            _effects = null;
        }
    }
}

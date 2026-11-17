using System;
using EcoTilt.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EcoTilt.Gameplay
{
    /// <summary>
    /// Pausa: detiene el tiempo del juego. Teclas Esc o P (en Android, el botón Atrás llega como Esc).
    /// Al continuar recalibra, por si el jugador cambió de posición durante la pausa.
    /// Si la app pasa a segundo plano (llamada, notificación), se pausa sola.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        [Tooltip("Componente que implemente ICalibratable (por ejemplo, TiltInputProvider).")]
        [SerializeField] private MonoBehaviour calibrationSource;
        [SerializeField] private bool recalibrateOnResume = true;

        private ICalibratable _calibratable;

        public bool IsPaused { get; private set; }

        public event Action<bool> PauseChanged;

        private void Awake()
        {
            _calibratable = calibrationSource as ICalibratable;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
                Toggle();
        }

        private void OnApplicationPause(bool pausedBySystem)
        {
            if (pausedBySystem) Pause();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        public void Toggle()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            PauseChanged?.Invoke(true);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (recalibrateOnResume) _calibratable?.Calibrate();
            PauseChanged?.Invoke(false);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

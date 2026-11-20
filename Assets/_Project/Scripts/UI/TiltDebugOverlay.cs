using EcoTilt.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.UI
{
    /// <summary>
    /// Acceso a calibración junto a Pausa (tecla C en el editor), sin datos técnicos en pantalla.
    /// Conserva el nombre del componente para mantener las referencias de las escenas existentes.
    /// </summary>
    public sealed class TiltDebugOverlay : MonoBehaviour
    {
        [Tooltip("Componente que implemente ICalibratable (por ejemplo, TiltInputProvider).")]
        [SerializeField] private MonoBehaviour source;

        private ICalibratable _calibratable;
        private GUIStyle _button;

        private void Awake()
        {
            _calibratable = source as ICalibratable;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.cKey.wasPressedThisFrame) _calibratable?.Calibrate();
        }

        private void OnGUI()
        {
            if (_calibratable == null || Time.timeScale <= 0f) return;
            float scale = Screen.height / 720f;
            EnsureStyles(scale);

            if (GUI.Button(GameplayHudLayout.CalibrationButton(scale), "Calibrar", _button))
                _calibratable.Calibrate();
        }

        private void EnsureStyles(float scale)
        {
            if (_button != null && _button.fontSize == Mathf.RoundToInt(22f * scale)) return;
            _button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(22f * scale) };
        }
    }
}

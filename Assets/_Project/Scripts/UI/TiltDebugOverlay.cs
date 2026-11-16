using EcoTilt.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.UI
{
    /// <summary>
    /// Panel de diagnóstico de la inclinación: qué sensor se usa, qué valores llegan y qué recibe el tablero.
    /// Incluye un botón para recalibrar (tecla C en el editor). Se apaga desactivando el componente.
    /// </summary>
    public sealed class TiltDebugOverlay : MonoBehaviour
    {
        [Tooltip("Componente que implemente ITiltDiagnostics (por ejemplo, TiltInputProvider).")]
        [SerializeField] private MonoBehaviour source;

        private ITiltDiagnostics _diagnostics;
        private ICalibratable _calibratable;
        private GUIStyle _style;
        private GUIStyle _button;

        private void Awake()
        {
            _diagnostics = source as ITiltDiagnostics;
            _calibratable = source as ICalibratable;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.cKey.wasPressedThisFrame) _calibratable?.Calibrate();
        }

        private void OnGUI()
        {
            if (_diagnostics == null) return;
            float scale = Screen.height / 720f;
            EnsureStyles(scale);

            var box = new Rect(Screen.width - 430f * scale, 12f * scale, 418f * scale, 130f * scale);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 10f * scale, box.y + 6f * scale, box.width - 20f * scale, box.height),
                      _diagnostics.Diagnostics, _style);

            if (_calibratable != null &&
                GUI.Button(new Rect(box.x, box.yMax + 8f * scale, 180f * scale, 48f * scale), "Calibrar", _button))
                _calibratable.Calibrate();
        }

        private void EnsureStyles(float scale)
        {
            int size = Mathf.RoundToInt(20f * scale);
            if (_style != null && _style.fontSize == size) return;
            _style = new GUIStyle(GUI.skin.label) { fontSize = size };
            _style.normal.textColor = Color.white;
            _button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(22f * scale) };
        }
    }
}

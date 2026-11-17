using EcoTilt.Controls;
using UnityEngine;

namespace EcoTilt.UI
{
    /// <summary>
    /// Mensajes de calibración para el jugador (prototipo, se reemplaza por la interfaz final en el Hito 3):
    /// - Al empezar: "poné el teléfono como vas a jugar y dejalo quieto", con una barra de progreso.
    /// - Si el tablero queda muy inclinado y quieto varios segundos (el jugador cambió de postura),
    ///   ofrece recalibrar con un botón.
    /// </summary>
    public sealed class CalibrationPrompt : MonoBehaviour
    {
        [Tooltip("Componente que implemente ITiltStatus e ICalibratable (por ejemplo, TiltInputProvider).")]
        [SerializeField] private MonoBehaviour source;
        [Tooltip("Inclinación (grados) a partir de la cual se sospecha un cambio de postura.")]
        [SerializeField, Range(20f, 90f)] private float postureAngle = 50f;
        [Tooltip("Segundos que tiene que mantenerse esa inclinación casi sin cambios para ofrecer recalibrar.")]
        [SerializeField, Range(1f, 10f)] private float postureHoldTime = 3f;
        [Tooltip("Cuánto puede variar la inclinación (grados) y seguir considerándose mantenida.")]
        [SerializeField, Range(1f, 20f)] private float postureTolerance = 8f;

        private ITiltStatus _status;
        private ICalibratable _calibratable;
        private Quaternion _postureReference = Quaternion.identity;
        private float _postureTime;
        private bool _offerRecalibration;
        private GUIStyle _title;
        private GUIStyle _button;

        private void Awake()
        {
            _status = source as ITiltStatus;
            _calibratable = source as ICalibratable;
        }

        private void Update()
        {
            if (_status == null || !_status.HasData || _status.IsCalibrating)
            {
                ResetPosture();
                return;
            }

            Quaternion tilt = _status.LastTilt;
            if (Tilt.Magnitude(tilt) < postureAngle || Quaternion.Angle(tilt, _postureReference) > postureTolerance)
            {
                _postureReference = tilt;
                _postureTime = 0f;
                return;
            }

            _postureTime += Time.unscaledDeltaTime;
            if (_postureTime >= postureHoldTime) _offerRecalibration = true;
        }

        private void ResetPosture()
        {
            _postureTime = 0f;
            _offerRecalibration = false;
        }

        private void OnGUI()
        {
            if (_status == null || !_status.HasData) return;
            float scale = Screen.height / 720f;
            EnsureStyles(scale);

            if (_status.IsCalibrating)
            {
                var panel = new Rect(Screen.width * 0.5f - 360f * scale, Screen.height * 0.3f, 720f * scale, 150f * scale);
                GUI.Box(panel, GUIContent.none);
                GUI.Label(new Rect(panel.x, panel.y + 12f * scale, panel.width, 70f * scale),
                          "Sostené el teléfono como vas a jugar\ny dejalo quieto un momento", _title);
                var bar = new Rect(panel.x + 60f * scale, panel.yMax - 45f * scale, panel.width - 120f * scale, 18f * scale);
                GUI.Box(bar, GUIContent.none);
                Color previous = GUI.color;
                GUI.color = new Color(1f, 0.55f, 0.15f);
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * _status.CalibrationProgress, bar.height), Texture2D.whiteTexture);
                GUI.color = previous;
                return;
            }

            if (_offerRecalibration && _calibratable != null)
            {
                var button = new Rect(Screen.width * 0.5f - 300f * scale, Screen.height - 110f * scale, 600f * scale, 80f * scale);
                if (GUI.Button(button, "¿Cambiaste de posición? Tocá acá para recalibrar", _button))
                {
                    _calibratable.Calibrate();
                    ResetPosture();
                }
            }
        }

        private void EnsureStyles(float scale)
        {
            int size = Mathf.RoundToInt(30f * scale);
            if (_title != null && _title.fontSize == size) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _title.normal.textColor = Color.white;
            _button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(26f * scale), fontStyle = FontStyle.Bold };
        }
    }
}

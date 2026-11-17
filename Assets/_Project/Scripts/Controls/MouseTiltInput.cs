using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Inclinación con el mouse: mientras se mantiene el click DERECHO, la posición del cursor
    /// respecto al centro de la pantalla define hacia dónde y cuánto se inclina el tablero.
    /// Es el derecho para que el click izquierdo con el que se le da foco a la ventana no mueva el tablero.
    /// </summary>
    public sealed class MouseTiltInput : ITiltInput
    {
        private readonly float _deadZone;
        private readonly float _maxAngle;

        public MouseTiltInput(float deadZone, float maxAngle)
        {
            _deadZone = Mathf.Clamp01(deadZone);
            _maxAngle = maxAngle;
        }

        public Quaternion ReadTilt()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed) return Quaternion.identity;

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float halfSize = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) * 0.5f);
            Vector2 offset = Vector2.ClampMagnitude((mouse.position.ReadValue() - center) / halfSize, 1f);
            if (offset.magnitude < _deadZone) return Quaternion.identity;

            return Tilt.FromAngles(offset.x * _maxAngle, offset.y * _maxAngle);
        }
    }
}

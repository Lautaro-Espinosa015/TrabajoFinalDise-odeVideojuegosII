using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Inclinación con el mouse: mientras se mantiene el click izquierdo, la posición del cursor
    /// respecto al centro de la pantalla define hacia dónde se inclina el tablero.
    /// </summary>
    public sealed class MouseTiltInput : ITiltInput
    {
        private readonly float _deadZone;

        public MouseTiltInput(float deadZone)
        {
            _deadZone = Mathf.Clamp01(deadZone);
        }

        public Vector2 ReadTilt()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed) return Vector2.zero;

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float halfSize = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) * 0.5f);
            Vector2 offset = Vector2.ClampMagnitude((mouse.position.ReadValue() - center) / halfSize, 1f);

            return offset.magnitude < _deadZone ? Vector2.zero : offset;
        }
    }
}

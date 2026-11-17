using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Inclinación con WASD o flechas (pruebas en PC / editor). El ángulo crece de forma gradual
    /// mientras se mantiene la tecla, para no sacudir el tablero de golpe.
    /// </summary>
    public sealed class KeyboardTiltInput : ITiltInput
    {
        private readonly float _maxAngle;
        private readonly float _speed;
        private Vector2 _angles;

        public KeyboardTiltInput(float maxAngle, float degreesPerSecond)
        {
            _maxAngle = maxAngle;
            _speed = degreesPerSecond;
        }

        public Quaternion ReadTilt()
        {
            Vector2 target = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) target.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) target.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) target.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) target.y += 1f;
            }

            target = Vector2.ClampMagnitude(target, 1f) * _maxAngle;
            _angles = Vector2.MoveTowards(_angles, target, _speed * Time.deltaTime);
            return Tilt.FromAngles(_angles.x, _angles.y);
        }
    }
}

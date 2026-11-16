using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Inclinación a partir de la dirección de la gravedad medida por el teléfono.
    /// Usa el GravitySensor (fusión de acelerómetro + giroscopio en Android) y, si el equipo
    /// no lo tiene, cae al acelerómetro con un filtro paso bajo.
    /// La posición en la que el jugador sostiene el teléfono al empezar se toma como neutra.
    /// </summary>
    public sealed class SensorTiltInput : ITiltInput, ICalibratable
    {
        public struct Settings
        {
            public float DeviceTiltForFullInput; // grados de inclinación física que equivalen a 1
            public float Responsiveness;         // cuanto mayor, menos suavizado
            public float DeadZone;
            public bool InvertX;
            public bool InvertY;
            public bool CalibrateOnFirstSample;
        }

        private readonly Settings _settings;
        private Vector3 _filteredGravity;
        private bool _hasSample;
        private bool _pendingCalibration;
        private float _neutralRoll;
        private float _neutralPitch;

        public SensorTiltInput(Settings settings)
        {
            _settings = settings;
            _pendingCalibration = settings.CalibrateOnFirstSample;
            EnableSensors();
        }

        /// <summary>True si el dispositivo tiene algún sensor utilizable.</summary>
        public static bool IsSupported => GravitySensor.current != null || Accelerometer.current != null;

        public void Calibrate()
        {
            if (!_hasSample)
            {
                _pendingCalibration = true;
                return;
            }

            GetAngles(_filteredGravity, out _neutralRoll, out _neutralPitch);
            _pendingCalibration = false;
        }

        public Vector2 ReadTilt()
        {
            if (!TryReadGravity(out Vector3 raw)) return Vector2.zero;

            float alpha = 1f - Mathf.Exp(-_settings.Responsiveness * Time.unscaledDeltaTime);
            _filteredGravity = _hasSample ? Vector3.Lerp(_filteredGravity, raw, alpha) : raw;
            _hasSample = true;

            if (_pendingCalibration) Calibrate();

            GetAngles(_filteredGravity, out float roll, out float pitch);
            float range = Mathf.Max(1f, _settings.DeviceTiltForFullInput);
            Vector2 tilt = new Vector2(
                Mathf.DeltaAngle(_neutralRoll, roll) / range,
                Mathf.DeltaAngle(_neutralPitch, pitch) / range);

            if (_settings.InvertX) tilt.x = -tilt.x;
            if (_settings.InvertY) tilt.y = -tilt.y;

            tilt = Vector2.ClampMagnitude(tilt, 1f);
            return tilt.magnitude < _settings.DeadZone ? Vector2.zero : tilt;
        }

        // Roll: inclinación lateral. Pitch: borde superior del teléfono hacia abajo = positivo.
        private static void GetAngles(Vector3 gravity, out float roll, out float pitch)
        {
            roll = Mathf.Atan2(gravity.x, -gravity.z) * Mathf.Rad2Deg;
            pitch = Mathf.Atan2(gravity.y, -gravity.z) * Mathf.Rad2Deg;
        }

        private static void EnableSensors()
        {
            if (GravitySensor.current != null && !GravitySensor.current.enabled)
                InputSystem.EnableDevice(GravitySensor.current);
            if (Accelerometer.current != null && !Accelerometer.current.enabled)
                InputSystem.EnableDevice(Accelerometer.current);
        }

        private static bool TryReadGravity(out Vector3 gravity)
        {
            GravitySensor gravitySensor = GravitySensor.current;
            if (gravitySensor != null && gravitySensor.enabled)
            {
                gravity = gravitySensor.gravity.ReadValue();
                if (gravity.sqrMagnitude > 0.0001f) return true;
            }

            Accelerometer accelerometer = Accelerometer.current;
            if (accelerometer != null && accelerometer.enabled)
            {
                gravity = accelerometer.acceleration.ReadValue();
                if (gravity.sqrMagnitude > 0.0001f) return true;
            }

            gravity = Vector3.zero;
            return false;
        }
    }
}

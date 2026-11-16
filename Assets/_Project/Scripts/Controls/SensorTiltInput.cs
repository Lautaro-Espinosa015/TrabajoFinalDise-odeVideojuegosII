using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoTilt.Controls
{
    /// <summary>
    /// Inclinación a partir de la dirección de la gravedad medida por el teléfono.
    /// Usa el GravitySensor (fusión de acelerómetro + giroscopio en Android) y, si el equipo
    /// no lo tiene, cae al acelerómetro con un filtro paso bajo.
    /// Los sensores se buscan y activan en cada lectura: con Unity Remote aparecen recién
    /// cuando el teléfono se conecta, un momento después de entrar en Play.
    /// La posición en la que el jugador sostiene el teléfono al empezar se toma como neutra.
    /// </summary>
    public sealed class SensorTiltInput : ITiltInput, ICalibratable, ITiltDiagnostics
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
        private Vector3 _lastRaw;
        private Vector2 _lastTilt;
        private string _source = "ninguno";
        private bool _hasSample;
        private bool _pendingCalibration;
        private float _neutralRoll;
        private float _neutralPitch;

        public SensorTiltInput(Settings settings)
        {
            _settings = settings;
            _pendingCalibration = settings.CalibrateOnFirstSample;
        }

        public string Diagnostics =>
            $"Sensor: {_source}\n" +
            $"Gravedad: ({_lastRaw.x:0.00}, {_lastRaw.y:0.00}, {_lastRaw.z:0.00})\n" +
            $"Inclinación: ({_lastTilt.x:0.00}, {_lastTilt.y:0.00})";

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
            if (!TryReadGravity(out Vector3 raw))
            {
                _lastTilt = Vector2.zero;
                return Vector2.zero;
            }

            _lastRaw = raw;
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
            _lastTilt = tilt.magnitude < _settings.DeadZone ? Vector2.zero : tilt;
            return _lastTilt;
        }

        // Roll: inclinación lateral. Pitch: borde superior del teléfono hacia abajo = positivo.
        private static void GetAngles(Vector3 gravity, out float roll, out float pitch)
        {
            roll = Mathf.Atan2(gravity.x, -gravity.z) * Mathf.Rad2Deg;
            pitch = Mathf.Atan2(gravity.y, -gravity.z) * Mathf.Rad2Deg;
        }

        private bool TryReadGravity(out Vector3 gravity)
        {
            GravitySensor gravitySensor = GravitySensor.current;
            if (Ready(gravitySensor))
            {
                gravity = gravitySensor.gravity.ReadValue();
                if (gravity.sqrMagnitude > 0.0001f)
                {
                    _source = $"GravitySensor ({gravitySensor.displayName})";
                    return true;
                }
            }

            Accelerometer accelerometer = Accelerometer.current;
            if (Ready(accelerometer))
            {
                gravity = accelerometer.acceleration.ReadValue();
                if (gravity.sqrMagnitude > 0.0001f)
                {
                    _source = $"Accelerometer ({accelerometer.displayName})";
                    return true;
                }
            }

            _source = gravitySensor == null && accelerometer == null
                ? "ninguno conectado"
                : "conectado, esperando datos";
            gravity = Vector3.zero;
            return false;
        }

        /// <summary>Activa el sensor si existe y todavía no está activo.</summary>
        private static bool Ready(InputDevice device)
        {
            if (device == null) return false;
            if (!device.enabled) InputSystem.EnableDevice(device);
            return device.enabled;
        }
    }
}
